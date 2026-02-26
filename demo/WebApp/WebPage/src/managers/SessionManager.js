import { Identity } from "../constants/Identity.js";
import { Session } from "../constants/Session.js";

class SessionManager {
    constructor() {
        this.activeIdx = null;
        this.activeId = null;
        this._isInitialised = false;
    }

    /**
     * Bootstraps the session context.
     * Logic: URL ID > SessionStorage Index > LocalStorage Last Index
     */
    init() {
        if (this._isInitialised)
            return;

        const registry = this._getRegistry();
        const urlId = this._parseIdFromUrl();
        const tabIdx = sessionStorage.getItem(`${Identity.APP_SCHEM}TAB_INDEX`);
        const lastIdx = localStorage.getItem(`${Identity.APP_SCHEM}LAST_ACTIVE`);

        let targetIdx = 0;

        if (urlId && registry.indexOf(urlId) !== -1) {
            targetIdx = registry.indexOf(urlId);
        } else if (tabIdx !== null) {
            targetIdx = parseInt(tabIdx, 10);
        } else if (lastIdx !== null) {
            targetIdx = parseInt(lastIdx, 10);
        }

        this._resolve(targetIdx);

        console.debug(`[SessionManager] activeIdx:`, this.activeIdx);
        console.debug(`[SessionManager] activeId:`, this.activeId);

        this._isInitialised = true;
    }

    /**
     * Forces the session to a specific index (Used for account switching)
     */
    switchToIndex(idx) {
        if (idx < 0 || idx >= Session.MAX_COUNT) return;
        this._resolve(idx);
        // Force a reload or route change to sync the URL
        window.location.hash = `#/${this.activeId}/home`;
        window.location.reload(); 
    }

    _resolve(idx) {
        console.debug(`[SessionManager] resolve, idx:`, idx);
        const registry = this._getRegistry();
        
        // If slot is empty, generate a new identity for this index
        if (!registry[idx]) {
            registry[idx] = `$${crypto.randomUUID().replaceAll("-", "").substring(0, 8)}`;
            this._saveRegistry(registry);
        }

        this.activeIdx = idx;
        this.activeId = registry[idx];

        // Persistence
        sessionStorage.setItem(`${Identity.APP_SCHEM}TAB_INDEX`, this.activeIdx);
        localStorage.setItem(`${Identity.APP_SCHEM}LAST_ACTIVE`, this.activeIdx);
    }

    _getRegistry() {
        const raw = localStorage.getItem(`${Identity.APP_SCHEM}REGISTRY`);
        return raw ? JSON.parse(raw) : new Array(Session.MAX_COUNT).fill(null);
    }

    _saveRegistry(arr) {
        localStorage.setItem(`${Identity.APP_SCHEM}REGISTRY`, JSON.stringify(arr));
    }

    _parseIdFromUrl() {
        const parts = window.location.hash.split('/');
        return (parts[1]?.startsWith('$')) ? parts[1] : null;
    }
}

export const sessionManager = new SessionManager();