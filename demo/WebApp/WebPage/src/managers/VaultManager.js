import { Identity } from '../constants/Identity.js';

/**
 * VaultManager.js
 * Handles IndexedDB storage and Non-Extractable WebCrypto keys.
 */

class VaultManager {
    constructor() {
        this._isInitialised = false;
        this._db = null;
        this._dbName = `${Identity.APP_SCHEM}db`;
        this._keyStore = `${Identity.APP_SCHEM}keys`;    // structured-clone CryptoKeys
        this._valueStore = `${Identity.APP_SCHEM}values`;  // encrypted strings
        this._masterKey = null;
        this._masterId = `${Identity.APP_SCHEM}MASTER`;
        this._rotationPromise = null;
    }

    async init() {
        if (this._db) return;

        this._db = await new Promise((resolve, reject) => {
            const request = indexedDB.open(this._dbName, 1);
            request.onupgradeneeded = (e) => {
                const db = e.target.result;
                if (!db.objectStoreNames.contains(this._valueStore)) db.createObjectStore(this._valueStore);
                if (!db.objectStoreNames.contains(this._keyStore)) db.createObjectStore(this._keyStore);
            };
            request.onsuccess = (e) => resolve(e.target.result);
            request.onerror = (e) => reject(e.target.error);
        });

        await this._prepareKey();

        this._isInitialised = true;
    }

    async _prepareKey() {
        // Try to load existing master key
        const existingKey = await this._loadKey(this._masterId);
        if (existingKey) {
            this._masterKey = existingKey;
        } else {
            // Create a NON-EXTRACTABLE key
            this._masterKey = await crypto.subtle.generateKey(
                { name: "AES-GCM", length: 256 },
                false, // extractable: false (CRITICAL)
                ["encrypt", "decrypt"]
            );
            // Store the key object (not raw bytes) in IndexedDB
            await this._saveKey(this._masterId, this._masterKey);
        }
    }

    /**
     * A helper to ensure we aren't currently middle-rotation.
     */
    async _assertReady() {
        if (!this._isInitialised) throw new Error("[VaultManager] Not initialized.");
        if (this._rotationPromise) await this._rotationPromise;
    }

    /**
     * Rotates the Master Key. 
     * Decrypts all existing vault data with the old key and re-encrypts it with a fresh one.
     */
    async rotate() {
        // If a rotation is already happening, just return that promise
        if (this._rotationPromise) return this._rotationPromise;

        // Create the promise and store it
        this._rotationPromise = (async () => {
            try {
                const newKey = await crypto.subtle.generateKey(
                    { name: "AES-GCM", length: 256 },
                    false,
                    ["encrypt", "decrypt"]
                );

                const tx = this._db.transaction(this._valueStore, "readonly");
                const ids = await new Promise(res => {
                    const req = tx.objectStore(this._valueStore).getAllKeys();
                    req.onsuccess = () => res(req.result);
                });

                for (const id of ids) {
                    // We load with the current/old masterKey
                    const data = await this._load(id);
                    if (data) {
                        // Re-encrypt with the NEW key using the fixed blob format
                        await this._save(id, data, newKey);
                    }
                }

                await this._saveKey(this._masterId, newKey);
                
                this._masterKey = newKey;
                console.log("Rotation complete.");
            } finally {
                // Clear the promise so the gate opens
                this._rotationPromise = null;
            }
        })();

        return this._rotationPromise;
    }

    // === String data (tokens, or anything sensitive) ===

    async save(id, data) {
        await this._assertReady();
        await this._save(id, data, this._masterKey);
    }

    async _save(id, data, key) {
        const iv = crypto.getRandomValues(new Uint8Array(12));
        const encoded = new TextEncoder().encode(data);
        const ciphertext = await crypto.subtle.encrypt({ name: "AES-GCM", iv }, key, encoded);
        const blob = { iv, data: ciphertext };
        const tx = this._db.transaction(this._valueStore, "readwrite");
        return new Promise((res, rej) => {
            const req = tx.objectStore(this._valueStore).put(blob, id);
            req.onsuccess = () => res();
            req.onerror = () => rej(req.error);
        });
    }

    async load(id) {
        await this._assertReady();
        return await this._load(id);
    }

    async _load(id) {
        const tx = this._db.transaction(this._valueStore, "readonly");
        const blob = await new Promise((res) => {
            const req = tx.objectStore(this._valueStore).get(id);
            req.onsuccess = () => res(req.result);
        });
        if (!blob) return null;
        const decrypted = await crypto.subtle.decrypt(
            { name: "AES-GCM", iv: blob.iv },
            this._masterKey,
            blob.data
        );
        return new TextDecoder().decode(decrypted);
    }

    async exists(id) {
        await this._assertReady();
        const tx = this._db.transaction(this._valueStore, "readonly");
        const count = await new Promise((res) => {
            const req = tx.objectStore(this._valueStore).count(id);
            req.onsuccess = () => res(req.result);
        });
        return count > 0;
    }

    async delete(id) {
        await this._assertReady();
        const tx = this._db.transaction(this._valueStore, "readwrite");
        tx.objectStore(this._valueStore).delete(id);
    }

    // === CryptoKey objects (DPoP keys) ===

    async saveKey(keyId, keyData) {
        await this._assertReady();
        await this._saveKey(keyId, keyData);
    }

    async _saveKey(keyId, keyData) {
        const tx = this._db.transaction(this._keyStore, "readwrite");
        return new Promise((res, rej) => {
            const req = tx.objectStore(this._keyStore).put(keyData, keyId);
            req.onsuccess = () => res();
            req.onerror = () => rej(req.error);
        });
    }

    async loadKey(keyId) {
        await this._assertReady();
        return await this._loadKey(keyId);
    }

    async _loadKey(keyId) {
        const tx = this._db.transaction(this._keyStore, "readonly");
        return new Promise((res, rej) => {
            const req = tx.objectStore(this._keyStore).get(keyId);
            req.onsuccess = () => res(req.result || null);
        });
    }

    async deleteKey(keyId) {
        await this._assertReady();
        const tx = this._db.transaction(this._keyStore, "readwrite");
        return new Promise((res, rej) => {
            const req = tx.objectStore(this._keyStore).delete(keyId);
            req.onsuccess = () => res();
            req.onerror = () => rej(req.error);
        });
    }
}

export const vaultManager = new VaultManager(); // as singleton