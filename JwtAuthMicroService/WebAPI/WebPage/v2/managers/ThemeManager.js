import { BehaviorSubject } from 'rxjs';
import { ThemeMode } from '../constants/ThemeMode.js';
import { Identity } from '../constants/Identity.js';
import { Config } from '../constants/Config.js';

class ThemeManager {
    constructor() {
        const mode = this._loadTheme();
        this._theme$ = new BehaviorSubject(mode);
        
        // Initial setup for the Shoelace specific logic
        this.apply(mode);
    }

    get theme$() { return this._theme$.asObservable(); }
    get current() { return this._theme$.value; }

    toggle() {
        const next = this.current === ThemeMode.LIGHT ? ThemeMode.DARK : ThemeMode.LIGHT;
        this.setTheme(next);
    }

    setTheme(mode) {
        this._saveTheme(mode);
        this.apply(mode);
        this._theme$.next(mode);
    }

    _loadTheme() {
        return localStorage.getItem(`${Identity.APP_SCHEM}${Config.THEME_MODE}`);
    }

    _saveTheme(mode) {
        localStorage.setItem(`${Identity.APP_SCHEM}${Config.THEME_MODE}`, mode);
    }

    /**
     * Internal: Handles the "Dirty" DOM manipulation
     */
    apply(mode) {
        const isDark = mode === ThemeMode.DARK;
        
        // 1. Handle Shoelace Stylesheet Links
        const lightLink = document.getElementById("shoelace-theme-light");
        const darkLink = document.getElementById("shoelace-theme-dark");

        if (lightLink && darkLink) {
            lightLink.disabled = isDark;
            darkLink.disabled = !isDark;
        }

        // 2. Toggle the class on <html> (Essential for Shoelace variables)
        document.documentElement.classList.toggle('sl-theme-dark', isDark);
        document.documentElement.classList.toggle('sl-theme-light', !isDark);
    }
}

export const themeMgr = new ThemeManager();