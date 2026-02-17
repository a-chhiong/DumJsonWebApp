import { vaultMgr } from './VaultManager.js';
import { Identity } from '../constants/Identity.js';
import { Config } from "../constants/Config.js";
import { stateHub } from '../objects/EventHub.js';
import { BehaviorSubject, distinctUntilChanged, map } from 'rxjs';

/**
 * TokenManager.js
 * Manages only the sensitive token strings.
 */

class TokenManager {
    constructor() {
        this._isInitialised = false;

        this._currentIdx = 0;
        this._sessionTokens = []; // RAM Cache: { index: { at, rt } }

        // Initialize Subject with a default state
        this._authSubject = new BehaviorSubject({ isAuth: false, token: null });
        // Expose the observable immediately so UI can subscribe anytime
        this.isAuthenticated$ = this._authSubject.asObservable().pipe(distinctUntilChanged((prev, curr) => prev.token === curr.token));
        
        this._initPromise = null;
        this._hydratingSlots = new Map(); // Track active hydration per index
    }

    async init(startIdx) {
        // If already initializing or finished, return the existing promise
        if (this._initPromise) return this._initPromise;
        this._currentIdx = startIdx;
        this._initPromise = (async () => {
            try {
                // Hydrate all slots into RAM at boot
                const indices = Array.from({ length: Config.SESSION_MAX }, (_, i) => i);
                await Promise.all(indices.map(i => this._hydrate(i)));

                console.debug(`[TokenManager] Initialized at index ${this._currentIdx}`);
            } catch (err) {
                this._initPromise = null; // Allow retry on failure
                throw err;
            } finally {
                this._isInitialised = true
            }
        })();

        // Listen for token updates (Local or Global)
        stateHub.watch('TOKEN_SYNC').subscribe(async (data) => {
            await this._hydrate(data.idx);
            console.debug(`[TokenManager] Token Sync complete for index ${data.idx}`);
        });
        
        // Listen for session updates (Local or Global)
        stateHub.hear('SESSION_SYNC').subscribe(async (data) => {
            this._currentIdx = data.idx;
            await this._hydrate(data.idx);
            console.debug(`[TokenManager] Session Sync complete for index ${data.idx}`);
        });

        return this._initPromise;
    }

    async _loadTokens(idx) {
        return await Promise.all([
            vaultMgr.load(`${Identity.APP_SCHEM}${Config.ACCESS_TOKEN}[${idx}]`),
            vaultMgr.load(`${Identity.APP_SCHEM}${Config.REFRESH_TOKEN}[${idx}]`)
        ]);
    }

    async _hydrate(idx) {
        if (idx === undefined || idx === null) return;
        
        const hydrationTask = (async () => {
            try {
                const [at, rt] = await this._loadTokens(idx);
                this._sessionTokens[idx] = (at || rt) ? { at, rt } : null;
                if (idx === this._currentIdx) this._updateAuthState(); // Update stream
            } catch (e) {
                console.error(`[TokenManager] Hydration error for slot ${idx}`, e);
            } finally {
                this._hydratingSlots.delete(idx);
            }
        })();
        this._hydratingSlots.set(idx, hydrationTask);

        return hydrationTask;
    }

    /**
     * Internal check to prevent out-of-order access.
     */
    _assertReady() {
        if (!this._isInitialised) {
            throw new Error("[TokenManager] Access denied: Manager is still initializing.");
        }

        if (this._hydratingSlots.has(this._currentIdx)) {
            // This prevents race conditions during SESSION_SYNC or TOKEN_SYNC
            throw new Error(`[TokenManager] Access denied: Slot ${this._currentIdx} is currently re-hydrating.`);
        }
    }

    // Explicit index-based accessors
    getAccessToken() { 
        this._assertReady();
        const idx = this._currentIdx;
        return this._sessionTokens[idx]?.at || null; 
    }
    
    getRefreshToken() {
        this._assertReady();
        const idx = this._currentIdx;
        return this._sessionTokens[idx]?.rt || null; 
    }
    
    async _saveTokens(idx, at, rt) {
        await Promise.all([
            vaultMgr.save(`${Identity.APP_SCHEM}${Config.ACCESS_TOKEN}[${idx}]`, at),
            vaultMgr.save(`${Identity.APP_SCHEM}${Config.REFRESH_TOKEN}[${idx}]`, rt)
        ]);
    }

    async saveTokens(at, rt) {
        if (!at || !rt) {
            throw new Error("Missing AT or RT to be saved in Vault.");
        }
        this._assertReady();
        const idx = this._currentIdx;
        const was = this._sessionTokens[idx];
        this._sessionTokens[idx] = { at, rt };
        try {
            await this._saveTokens(idx, at, rt);
            stateHub.cast('TOKEN_SYNC', { type: 'SAVE', idx });
            console.debug(`[TokenManager] Persistence confirmed for index ${idx}`);
            this._updateAuthState(); // Update stream
        } catch (err) {
            this._sessionTokens[idx] = was;     //rollback 
            throw new Error("Failed to persist tokens safely to Vault.");
        }
    }

    async _clearTokens(idx) {
        await Promise.all([
            vaultMgr.clear(`${Identity.APP_SCHEM}${Config.ACCESS_TOKEN}[${idx}]`),
            vaultMgr.clear(`${Identity.APP_SCHEM}${Config.REFRESH_TOKEN}[${idx}]`)
        ]);
    }

    async clearTokens() {
        this._assertReady();
        const idx = this._currentIdx;
        const was = this._sessionTokens[idx];
        this._sessionTokens[idx] = null;
        try {
            await this._clearTokens(idx);
            stateHub.cast('TOKEN_SYNC', { type: 'CLEAR', idx });
            console.debug(`[TokenManager] Clearance confirmed for index ${idx}`);
            this._updateAuthState(); // Update stream
        } catch (err) {
            this._sessionTokens[idx] = was;     //rollback
            throw new Error("Failed to persist tokens safely to Vault.");
        }
    }

    // Helper to update the stream
    _updateAuthState() {
        const current = this._sessionTokens[this._currentIdx];
        this._authSubject.next({
            isAuth: !!current?.at,
            token: current?.at || null
        });
    }
}

export const tokenMgr = new TokenManager();