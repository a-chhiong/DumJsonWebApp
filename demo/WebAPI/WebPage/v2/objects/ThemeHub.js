import { BehaviorSubject } from 'rxjs';
import { Theme } from '../constants/Theme.js';
import { Identity } from '../constants/Identity.js';
import { Config } from '../constants/Config.js';

class ThemeHub {
    constructor() {
        const mode = this._initMode();
        this._theme$ = new BehaviorSubject(mode);
        
        // Initial setup
        this._apply(mode);
    }

    get theme$() { return this._theme$.asObservable(); }
    get current() { return this._theme$.value; }

    toggle() {
        const next = this.current === Theme.LIGHT ? Theme.DARK : Theme.LIGHT;
        this.setTheme(next);
    }

    setTheme(mode) {
        this._saveTheme(mode);
        this._apply(mode);
        this._theme$.next(mode);
    }

    _initMode() {
        const saved = localStorage.getItem(`${Identity.APP_SCHEM}${Config.THEME_MODE}`);
        if (saved) return saved;

        // "Android logic": Follow system if nothing is saved
        return window.matchMedia('(prefers-color-scheme: dark)').matches 
            ? Theme.DARK 
            : Theme.LIGHT;
    }

    _saveTheme(mode) {
        localStorage.setItem(`${Identity.APP_SCHEM}${Config.THEME_MODE}`, mode);
    }

    /**
     * The "Dirty" DOM logic: Includes a fallback for slow-loading HTML
     */
    _apply(mode) {
        const isDark = mode === Theme.DARK;
        
        // 1. Tag the Root immediately (Fastest way to update Shoelace CSS variables)
        document.documentElement.classList.toggle('sl-theme-dark', isDark);
        document.documentElement.classList.toggle('sl-theme-light', !isDark);
        document.documentElement.setAttribute('data-theme', mode);

        // 2. Safely Swap Stylesheets
        const lightLink = document.getElementById("shoelace-theme-light");
        const darkLink = document.getElementById("shoelace-theme-dark");

        if (lightLink && darkLink) {
            // Standard swap
            lightLink.disabled = isDark;
            darkLink.disabled = !isDark;
        } else {
            // RECOVERY LOGIC: If the links aren't in DOM yet, wait for them.
            // This happens often in SPAs where scripts run before head is fully parsed.
            window.addEventListener('DOMContentLoaded', () => this._apply(mode), { once: true });
        }
    }
}

export const themeHub = new ThemeHub();