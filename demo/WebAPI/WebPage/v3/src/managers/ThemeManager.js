// src/managers/ThemeManager.js
import { BehaviorSubject } from 'rxjs';
import { Theme } from '../constants/Theme.js';
import { Identity } from '../constants/Identity.js';
import { Config } from '../constants/Config.js';

class ThemeManager {
    constructor() {
        const mode = this._initMode();
        this._theme$ = new BehaviorSubject(mode);
        
        // This is safe to call immediately. 
        // document.documentElement is always available to scripts.
        this._apply(mode);
    }

    get theme$() { return this._theme$.asObservable(); }
    get current() { return this._theme$.value; }

    setTheme(mode) {
        localStorage.setItem(`${Identity.APP_SCHEM}${Config.THEME_MODE}`, mode);
        this._apply(mode);
        this._theme$.next(mode);
    }

    toggle() {
        const next = this.current === Theme.LIGHT ? Theme.DARK : Theme.LIGHT;
        this.setTheme(next);
    }

    _initMode() {
        const saved = localStorage.getItem(`${Identity.APP_SCHEM}${Config.THEME_MODE}`);
        if (saved) return saved;

        return window.matchMedia('(prefers-color-scheme: dark)').matches 
            ? Theme.DARK 
            : Theme.LIGHT;
    }

    _apply(mode) {
        const isDark = mode === Theme.DARK;
        const root = document.documentElement;

        // 1. Toggle the classes. 
        // This matches the .sl-theme-dark/light selectors in your "Bullet-proof" CSS.
        root.classList.toggle('sl-theme-dark', isDark);
        root.classList.toggle('sl-theme-light', !isDark);

        // 2. Browser hinting for scrollbars and system UI
        root.style.colorScheme = isDark ? 'dark' : 'light';
        
        // 3. Keep your data attribute for any legacy CSS selectors
        root.setAttribute('data-theme', mode);
    }
}

export const themeManager = new ThemeManager();