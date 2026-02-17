import { Identity } from '../constants/Identity.js';

/**
 * VaultManager.js
 * Handles IndexedDB storage and Non-Extractable WebCrypto keys.
 */

class VaultManager {
    constructor() {
        this._dbName = `${Identity.APP_SCHEM}db`;
        this._keyStore = `${Identity.APP_SCHEM}keys`;    // structured-clone CryptoKeys
        this._storeName = `${Identity.APP_SCHEM}vault`;  // encrypted strings
        this._masterName = `${Identity.APP_SCHEM}MASTER`;
        this._db = null;
        this._masterKey = null;
        this._rotationPromise = null;
    }

    async init() {
        if (this._db) return;

        this._db = await new Promise((resolve, reject) => {
            const request = indexedDB.open(this._dbName, 1);
            request.onupgradeneeded = (e) => {
                const db = e.target.result;
                if (!db.objectStoreNames.contains(this._storeName)) db.createObjectStore(this._storeName);
                if (!db.objectStoreNames.contains(this._keyStore)) db.createObjectStore(this._keyStore);
            };
            request.onsuccess = (e) => resolve(e.target.result);
            request.onerror = (e) => reject(e.target.error);
        });

        await this._prepareKey();
    }

    async _prepareKey() {
        // Try to load existing master key
        const tx = this._db.transaction(this._keyStore, "readonly");
        const existingKey = await new Promise((res) => {
            const req = tx.objectStore(this._keyStore).get(this._masterName);
            req.onsuccess = () => res(req.result);
        });
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
            const saveTx = this._db.transaction(this._keyStore, "readwrite");
            saveTx.objectStore(this._keyStore).put(this._masterKey, this._masterName);
        }
    }

    /**
     * A helper to ensure we aren't currently middle-rotation.
     */
    async _guard() {
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
                await this.init();
                const newKey = await crypto.subtle.generateKey(
                    { name: "AES-GCM", length: 256 },
                    false,
                    ["encrypt", "decrypt"]
                );

                const tx = this._db.transaction(this._storeName, "readonly");
                const ids = await new Promise(res => {
                    const req = tx.objectStore(this._storeName).getAllKeys();
                    req.onsuccess = () => res(req.result);
                });

                for (const id of ids) {
                    // We load with the current/old masterKey
                    const data = await this.load(id);
                    if (data) {
                        // Re-encrypt with the NEW key using the fixed blob format
                        await this._saveWithKey(id, data, newKey);
                    }
                }

                const keyTx = this._db.transaction(this._keyStore, "readwrite");
                keyTx.objectStore(this._keyStore).put(newKey, this._masterName);

                this._masterKey = newKey;
                console.log("Rotation complete.");
            } finally {
                // Clear the promise so the gate opens
                this._rotationPromise = null;
            }
        })();

        return this._rotationPromise;
    }

    /**
     * Internal helper to save data with a specific key 
     * (Prevents race conditions during rotation)
     */
    async _saveWithKey(id, data, key) {
        const iv = crypto.getRandomValues(new Uint8Array(12));
        const encoded = new TextEncoder().encode(data);
        const ciphertext = await crypto.subtle.encrypt({ name: "AES-GCM", iv }, key, encoded);
        const blob = { iv, data: ciphertext };
        const tx = this._db.transaction(this._storeName, "readwrite");
        return new Promise((res, rej) => {
            const req = tx.objectStore(this._storeName).put(blob, id);
            req.onsuccess = () => res();
            req.onerror = () => rej(req.error);
        });
    }

    // === String data (tokens) ===

    async save(id, data) {
        await this._guard();
        await this.init();
        return this._saveWithKey(id, data, this._masterKey);
    }

    async load(id) {
        if (!this._rotationPromise) await this._guard();
        await this.init();
        const tx = this._db.transaction(this._storeName, "readonly");
        const blob = await new Promise((res) => {
            const req = tx.objectStore(this._storeName).get(id);
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
        await this._guard();
        await this.init();
        const tx = this._db.transaction(this._storeName, "readonly");
        const count = await new Promise((res) => {
            const req = tx.objectStore(this._storeName).count(id);
            req.onsuccess = () => res(req.result);
        });
        return count > 0;
    }

    async clear(id) {
        await this._guard();
        await this.init();
        const tx = this._db.transaction(this._storeName, "readwrite");
        tx.objectStore(this._storeName).delete(id);
    }

    // === CryptoKey objects (DPoP keys) ===

    async saveCryptoKey(keyId, cryptoKey) {
        await this._guard();
        await this.init();
        const tx = this._db.transaction(this._keyStore, "readwrite");
        return new Promise((res, rej) => {
            const req = tx.objectStore(this._keyStore).put(cryptoKey, keyId);
            req.onsuccess = () => res();
            req.onerror = () => rej(req.error);
        });
    }

    async loadCryptoKey(keyId) {
        await this._guard();
        await this.init();
        const tx = this._db.transaction(this._keyStore, "readonly");
        return new Promise((res, rej) => {
            const req = tx.objectStore(this._keyStore).get(keyId);
            req.onsuccess = () => res(req.result || null);
            req.onerror = () => rej(req.error);
        });
    }

    async deleteCryptoKey(keyId) {
        await this._guard();
        await this.init();
        const tx = this._db.transaction(this._keyStore, "readwrite");
        return new Promise((res, rej) => {
            const req = tx.objectStore(this._keyStore).delete(keyId);
            req.onsuccess = () => res();
            req.onerror = () => rej(req.error);
        });
    }
}

export const vaultManager = new VaultManager(); // as singleton