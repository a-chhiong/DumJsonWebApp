import { Identity } from "../constants/Identity.js";
import { Config } from "../constants/Config.js";
import { stateHub } from '../helpers/EventHub.js';

/**
 * SessionManager.js
 * Responsibility: Manage the "Active Account Pointer" using localStorage.
 */

class SessionManager {
    constructor() {
        this._isInitialised = false;
    }

    init() {
        if (this._isInitialised) return;

        // Initialize state
        const idx = this._calculateInitialIndex();
        this._setActive(idx);

        stateHub.watch('SESSION_SYNC').subscribe(async (data) => {
            this._setActive(data.idx);
            console.debug(`[SessionManager] Session Sync complete for index ${data.idx}`);
        });

        this._isInitialised = true;

        return this._activeIdx;
    }

    _getCurrentTabIndex() {
        return sessionStorage.getItem(`${Identity.APP_SCHEM}${Config.CURRENT_TAB}`);
    }

    _setCurrentTabIndex(idx) {
        return sessionStorage.setItem(`${Identity.APP_SCHEM}${Config.CURRENT_TAB}`, idx);
    }

    _getLastActiveTabIndex() {
        return localStorage.getItem(`${Identity.APP_SCHEM}${Config.LAST_ACTIVE_TAB}`);
    }

    _setLastActiveTabIndex(idx) {
        return localStorage.setItem(`${Identity.APP_SCHEM}${Config.LAST_ACTIVE_TAB}`, idx);
    }

    _calculateInitialIndex() {
        let idx = this._getCurrentTabIndex();
        if (idx === null) {
            idx = this._getLastActiveTabIndex();
            // Claim it for this tab session
            if (idx !== null) this._setCurrentTabIndex(idx);
        }
        return idx !== null ? parseInt(idx, 10) : 0;
    }

    _setActive(idx) {
        if (idx === null || idx < 0 || idx >= Config.SESSION_MAX) {
            throw new Error("Invalid account index");
        }
        this._activeIdx = idx;
        this._setCurrentTabIndex(idx);
        this._setLastActiveTabIndex(idx)
    }

    setActive(idx) {
        if (!!this._isInitialised) return;
        this._setActive(idx);
        stateHub.cast('SESSION_SYNC', { idx: idx });
    }
}

export const sessionManager = new SessionManager();
