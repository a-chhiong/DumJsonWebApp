import { BehaviorSubject, interval, Subscription } from 'rxjs';
import { map, tap, takeWhile, timer } from 'rxjs';
import { BaseViewModel } from './BaseViewModel.js';
import { apiManager } from '../managers/ApiManager.js';
import { tokenManager } from '../managers/TokenManager.js';
import { themeManager } from '../managers/ThemeManager.js';
import { Theme } from '../constants/Theme.js';

export class HomeViewModel extends BaseViewModel {
    constructor() {
        super();
        // Observables for the UI to "Hook" into
        this.user$ = new BehaviorSubject(null);
        this.remainingTime$ = new BehaviorSubject(-1);
        this.loading$ = new BehaviorSubject(false);
        // Internal subscription management
        this._timerSub = null;
        // Wrap the manager's stream so the View only sees the VM
        this.theme$ = themeManager.theme$;
    }

    async onConnect() {
        super.onConnect();
        await this._initDashboard();
    }

    onDisconnect() {
        super.onDisconnect();
        this._stopHeartbeat();
    }

    toggleTheme() {
        const current = themeManager.current;
        themeManager.setTheme(current === Theme.DARK ? Theme.LIGHT : Theme.DARK);
    }

    /**
     * Entry point for HomeView (onStart equivalent)
     */
    async _initDashboard() {
        this._startHeartbeat();
        
        // Prevent double-loading if user is already there
        if (this.user$.value) return;

        this.loading$.next(true);
        try {
            const res = await apiManager.authApi.get('/user');
            this.user$.next(res.data);
            
            // Start the security heartbeat once we have the user
            this._startHeartbeat();
        } catch (err) {
            console.error("HomeService: Profile Sync Failed", err);
        } finally {
            this.loading$.next(false);
        }
    }

    /**
     * Security Heartbeat logic (The JWT Countdown)
     */
    _startHeartbeat() {
        // Cleanup existing timer if any
        this._stopHeartbeat();

        const token = tokenManager.getAccessToken();
        const expiry = this._getExpiry(token);

        if (expiry <= 0) return;

        this._timerSub = timer(0, 1000).pipe(
            map(() => {
                const now = Math.floor(Date.now() / 1000);
                return Math.max(0, expiry - now);
            }),
            tap(timeLeft => {
                this.remainingTime$.next(timeLeft);
                if (timeLeft === 0) {
                    // Optional: Trigger auto-logout or alert
                    console.warn("Session Expired");
                }
            }),
            // Automatically stop the stream when time hits zero
            takeWhile(timeLeft => timeLeft >= 0, true) 
        ).subscribe({
            complete: () => console.log("HomeService: Heartbeat stopped (Expired or Logged Out)")
        });
    }

    _getExpiry(token) {
        if (!token) return -1;
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            return payload.exp;
        } catch (e) { return -1; }
    }

    _stopHeartbeat() {
        if (this._timerSub) {
            this._timerSub.unsubscribe();
            this._timerSub = null;
        }
    }

    async logout() {
        this._stopHeartbeat();
        try {
            const rt = tokenManager.getRefreshToken();
            if (rt) {
                await apiManager.tokenApi.post("/logout", { refreshToken: rt });
            }
        } catch (err) {
            console.warn("HomeService: Logout request failed, clearing local state anyway.");
        } finally {
            // This triggers tokenMgr's isAuthenticated$ -> AppShell routes to Login
            await tokenManager.clearTokens();
            this.user$.next(null);
            this.remainingTime$.next(-1);
        }
    }
}