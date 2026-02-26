import { apiManager } from '../managers/ApiManager.js';
import { tokenManager } from '../managers/TokenManager.js';
import { themeManager } from '../managers/ThemeManager.js';
import { Theme } from '../constants/Theme.js';
import { BehaviorSubject } from 'rxjs';
import { BaseViewModel } from './BaseViewModel.js';

export class LoginViewModel extends BaseViewModel {
    constructor() {
        super();
        // State for the view to observe
        this.loading$ = new BehaviorSubject(false);
        this.error$ = new BehaviorSubject(null);
        // Wrap the manager's stream so the View only sees the VM
        this.theme$ = themeManager.theme$;
    }

    toggleTheme() {
        const current = themeManager.current;
        themeManager.setTheme(current === Theme.DARK ? Theme.LIGHT : Theme.DARK);
    }

    async login(username, password) {
        this.loading$.next(true);
        this.error$.next(null);

        try {
            const res = await apiManager.tokenApi.post("/login", { username, password });
            const { accessToken, refreshToken } = res.data;

            // This is the "Transaction": Save tokens and let the system react
            await tokenManager.saveTokens(accessToken, refreshToken);
            return { success: true };

        } catch (err) {
            const msg = err.response?.data?.message || err.message || "Login Failed";
            this.error$.next(msg);
            return { success: false, error: msg };
        } finally {
            this.loading$.next(false);
        }
    }

    onDisconnect() {
        this._dispose();
    }

    _dispose() {
        // Complete the streams so no one stays subscribed
        this.loading$.complete();
        this.error$.complete();
    }
}