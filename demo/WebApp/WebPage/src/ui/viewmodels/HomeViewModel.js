import { BehaviorSubject, timer, takeUntil } from 'rxjs'; // Fix: import takeUntil from main
import { map, tap, takeWhile } from 'rxjs';
import { BaseViewModel } from './BaseViewModel.js';
import { apiManager } from '../../managers/ApiManager.js';
import { tokenManager } from '../../managers/TokenManager.js';
import { themeManager } from '../../managers/ThemeManager.js';
import { Theme } from '../../constants/Theme.js';
import { dpopManager } from '../../managers/DPoPManager.js';

export class HomeViewModel extends BaseViewModel {
    constructor(host) {
        super(host);
        
        // 1. Private Subjects (Sources)
        this._user$ = new BehaviorSubject(null);
        this._accessTime$ = new BehaviorSubject(-1);
        this._sessionTime$ = new BehaviorSubject(-1);
        this._loading$ = new BehaviorSubject(false);

        // 2. Bound UI State (The "Hubs" are now internal)
        this.user = this.bind(this._user$);
        this.accessTime = this.bind(this._accessTime$, -1);
        this.sessionTime = this.bind(this._sessionTime$, -1);
        this.theme = this.bind(themeManager.theme$, themeManager.current);
        this.loading = this.bind(this._loading$, false);

        this._timerSub = null;
    }

    hostConnected() {
        super.hostConnected();
        this._initDashboard();
    }
    
    toggleTheme() {
        const current = themeManager.current;
        themeManager.setTheme(current === Theme.DARK ? Theme.LIGHT : Theme.DARK);
    }

    async _initDashboard() {
        this._startHeartbeat();
        
        if (this._user$.value) return;

        try {
            const res = await apiManager.authApi.get('/user');
            this._user$.next(res.data);
            this._startHeartbeat();
        } catch (err) {
            console.error("[HomeViewModel] 🚨 Profile Sync Failed", err);
        }
    }

    _startHeartbeat() {
        this._stopHeartbeat();
        const at = tokenManager.getAccessToken();
        const rt = tokenManager.getRefreshToken();
        const atExpiry = this._getExpiry(at);
        const rtExpiry = this._getExpiry(rt);

        if (rtExpiry <= 0) return;

        this._timerSub = timer(0, 1000).pipe(
            map(() => ({
                atLeft: Math.max(0, atExpiry - Math.floor(Date.now() / 1000)),
                rtLeft: Math.max(0, rtExpiry - Math.floor(Date.now() / 1000))
            })),
            tap(({ atLeft, rtLeft }) => {
                this._accessTime$.next(atLeft);
                this._sessionTime$.next(rtLeft);

                if (atLeft <= 0) 
                    console.warn("[HomeViewModel] 🚨 AccessToken Expired");
                if (rtLeft <= 0) 
                    console.warn("[HomeViewModel] 🚨 RefreshToken Expired");
            }),
            takeWhile(({ rtLeft }) => rtLeft >= 0, true),
            takeUntil(this.destroy$) 
        ).subscribe();
    }

    _getExpiry(token) {
        if (!token) return -1;
        try {
            return JSON.parse(atob(token.split('.')[1])).exp;
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
            if (rt) await apiManager.tokenApi.post("/logout", { refreshToken: rt });
        } finally {
            this._user$.next(null);
            await dpopManager.clearKeys();
            await tokenManager.clearTokens(true);
        }
    }

    async reloadPage() {
        // Prevent multiple simultaneous reloads
        if (this._loading$.value) return;

        console.debug("[HomeViewModel] 🔄 Manual Data Refresh...");
        
        this._loading$.next(true);

        try {
            // 1. Re-fetch user profile
            const res = await apiManager.authApi.get('/user');
            
            // 2. Push to BehaviorSubject -> HomeView sees this and renders
            this._user$.next(res.data);
            
            // 3. Optional: Re-sync tokens if they changed, otherwise just restart timers
            this._startHeartbeat();

            console.debug("[HomeViewModel] 🔄 Manual Data Successfully!");
        } catch (err) {
            console.error("[HomeViewModel] 🚨 Reload Failed:", err);
            // You could fire 'app:dialog' here if it's a critical failure
        } finally {
            this._loading$.next(false);
        }
    }
}