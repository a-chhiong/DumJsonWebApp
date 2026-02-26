import { vaultManager } from './VaultManager.js';
import { Identity } from '../constants/Identity.js';
import { Session } from "../constants/Session.js";
import { stateHub } from '../helpers/EventHub.js';
import { BehaviorSubject, distinctUntilChanged } from 'rxjs';

class DPoPManager {
    constructor() {
        this._currentIdx = 0;
        this._sessionKeys = []; // RAM cache: { index: { privateKey, publicKey, publicJwk } }

        // Initialize Subject with a default state
        this._keySubject = new BehaviorSubject({ isReady: false, jwk: null });
        // Expose the observable immediately so UI can subscribe anytime
        this.isKeyReady$ = this._keySubject.asObservable().pipe(
            distinctUntilChanged((prev, curr) => prev.jwk === curr.jwk)
        );

        this._initPromise = null;
        this._isInitialised = false;
        this._hydratingSlots = new Map(); // Track active hydration per index
    }

    async init(startIndx) {
        // If already initializing or finished, return the existing promise
        if (this._initPromise) return this._initPromise;
        this._currentIdx = startIndx;
        this._initPromise = (async () => {
        try {
            // Hydrate all slots into RAM at boot
            const indices = Array.from({ length: Session.MAX_COUNT }, (_, i) => i);
            await Promise.all(indices.map(i => this._hydrate(i)));
            await this._shouldCreateKeyPair();
            console.debug(`[DPoPManager] Initialized at index ${this._currentIdx}`);
        } catch (err) {
            this._initPromise = null;
            throw err;
        } finally {
            this._isInitialised = true;
        }
        })();

        stateHub.watch('KEY_SYNC').subscribe(async (data) => {
            await this._hydrate(data.idx);
            console.debug(`[DPoPManager] Key Sync complete for index ${data.idx}`);
        });

        stateHub.hear('SESSION_SYNC').subscribe(async (data) => {
            this._currentIdx = data.idx;
            await this._hydrate(data.idx);
            console.debug(`[DPoPManager] Session Sync complete for index ${data.idx}`);
        });

        return this._initPromise;
    }

    async _loadKeys(idx) {
        return await Promise.all([
            vaultManager.loadCryptoKey(`${Identity.APP_SCHEM}PUBLIC[${idx}]`),
            vaultManager.loadCryptoKey(`${Identity.APP_SCHEM}PRIVATE[${idx}]`)
        ]);
    }

    async _loadJwk(pub) {
        return await crypto.subtle.exportKey("jwk", pub);
    }

    async _hydrate(idx) {
        if (idx === undefined || idx === null) return;

        const hydrationTask = (async () => {
            try {
                const [priv, pub] = await this._loadKeys(idx);
                let publicJwk = null;
                if (pub) {
                    try {
                        publicJwk = await this._loadJwk(pub);
                    } catch (e) {
                        console.warn(`[DPoPManager] Failed to export public JWK`, e);
                    }
                }
                this._sessionKeys[idx] = (priv && pub) ? { privateKey: priv, publicKey: pub, publicJwk } : null;
                if (idx === this._currentIdx) this._updateKeyState();
            } catch (e) {
                console.error(`[DPoPManager] Hydration error for slot ${idx}`, e);
            } finally {
                this._hydratingSlots.delete(idx);
            }
        })();
        this._hydratingSlots.set(idx, hydrationTask);

        return hydrationTask;
    }

    async _shouldCreateKeyPair() {
        const idx = this._currentIdx;
        const existing = this._sessionKeys[idx];
        if (existing?.privateKey && existing?.publicKey) {
            console.debug(`[DPoPManager] Slot ${idx} already has a key pair, skipping create.`);
            return;
        }
        console.debug(`[DPoPManager] No keys found for slot ${idx}, generating new pair...`);
        const kp = await this._generateKeyPair();
        const publicJwk = await this._loadJwk(kp.publicKey);

        this._sessionKeys[idx] = { privateKey: kp.privateKey, publicKey: kp.publicKey, publicJwk };

        await this._saveKeys(idx, kp.privateKey, kp.publicKey);
        
        stateHub.cast('KEY_SYNC', { type: 'ROTATE', idx });
        console.debug(`[DPoPManager] Key init complete for index ${idx}`);
    }

    _assertReady() {
        if (!this._isInitialised) throw new Error("[DPoPManager] Not initialized.");
        if (this._hydratingSlots.has(this._currentIdx)) throw new Error(`[DPoPManager] Slot ${this._currentIdx} is re-hydrating.`);
    }

    _getKeyPair() {
        this._assertReady();
        return this._sessionKeys[this._currentIdx] || null;
    }

    async _saveKeys(idx, priv, pub) {
        await Promise.all([
            vaultManager.saveCryptoKey(`${Identity.APP_SCHEM}PUBLIC[${idx}]`, priv),
            vaultManager.saveCryptoKey(`${Identity.APP_SCHEM}PRIVATE[${idx}]`, pub)
        ]);
    }

    async _generateKeyPair() {
        return await crypto.subtle.generateKey(
            { name: "ECDSA", namedCurve: "P-256" },
            false, // private key non-extractable
            ["sign", "verify"]
        );
    }

    async rotateKey() {
        this._assertReady();
        const idx = this._currentIdx;

        // Generate a new EC key pair
        const kp = await this._generateKeyPair();

        // Export public JWK for DPoP header
        const publicJwk = await this._loadJwk(kp.publicKey);

        // Update RAM cache
        this._sessionKeys[idx] = { privateKey: kp.privateKey, publicKey: kp.publicKey, publicJwk };

        // Persist keys in Vault
        this._saveKeys(idx, kp.privateKey, kp.publicKey);

        // Broadcast rotation event
        stateHub.cast('KEY_SYNC', { type: 'ROTATE', idx });
        console.debug(`[DPoPManager] Key rotation complete for index ${idx}`);

        this._updateKeyState();
    }

    async _clearKeys(idx) {
        await Promise.all([
            vaultManager.saveCryptoKey(`${Identity.APP_SCHEM}PUBLIC[${idx}]`),
            vaultManager.saveCryptoKey(`${Identity.APP_SCHEM}PRIVATE[${idx}]`)
        ]);
    }

    async clearKeys() {
        this._assertReady();
        const idx = this._currentIdx;
        const was = this._sessionKeys[idx];
        this._sessionKeys[idx] = null;
        try {
            await this._clearKeys(idx);
            stateHub.cast('KEY_SYNC', { type: 'CLEAR', idx });
            console.debug(`[DPoPManager] Clearance confirmed for index ${idx}`);
            this._updateKeyState();
        } catch (err) {
            this._sessionKeys[idx] = was;
            throw new Error("Failed to clear keys safely from Vault.");
        }
    }

    _updateKeyState() {
        const current = this._sessionKeys[this._currentIdx];
        this._keySubject.next({
            isReady: !!current?.privateKey,
            jwk: current?.publicJwk || null
        });
    }

    // For initial token request (no ATH)
    // For requests with an access token (with ATH binding)
    async createDPoP(htm, htu, accessToken = null) {
        let ath = null;
        if (accessToken) {
            // Compute ath = base64url(sha256(accessToken))
            const hash = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(accessToken));
            ath = this._base64url(hash);
        }
        return this._createProof(htm, htu, ath);
    }

    async _createProof(htm, htu, ath) {
        const kp = this._getKeyPair();
        if (!kp) throw new Error("No DPoP key pair available.");

        const header = {
            alg: "ES256",
            typ: "dpop+jwt",
            jwk: { kty: kp.publicJwk.kty, crv: kp.publicJwk.crv, x: kp.publicJwk.x, y: kp.publicJwk.y }
        };

        const payload = {
            jti: crypto.randomUUID(),
            htm: htm.toUpperCase(),
            htu,
            iat: Math.floor(Date.now()/1000),
            ...(ath ? { ath } : {})
        };

        const signingInput = `${this._base64url(JSON.stringify(header))}.${this._base64url(JSON.stringify(payload))}`;
        const sig = await crypto.subtle.sign({ name:"ECDSA", hash:{name:"SHA-256"} }, kp.privateKey, new TextEncoder().encode(signingInput));
        return `${signingInput}.${this._base64url(sig)}`;
    }

    _base64url(data) {
        let bytes;
        if (typeof data === "string") {
            bytes = new TextEncoder().encode(data);
        } else if (data instanceof ArrayBuffer) {
            bytes = new Uint8Array(data);
        } else {
            bytes = new Uint8Array(data);
        }
        let binary = "";
        bytes.forEach(b => binary += String.fromCharCode(b));
        return btoa(binary).replace(/\+/g,"-").replace(/\//g,"_").replace(/=+$/,"");
    }

}

export const dpopManager = new DPoPManager();
