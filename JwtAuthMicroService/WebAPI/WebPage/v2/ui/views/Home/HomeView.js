import { BaseView } from '../BaseView.js';
import { HomeTemplate } from './HomeTemplate.js';
import { apiMgr } from '../../../managers/ApiManager.js';
import { tokenMgr } from '../../../managers/TokenManager.js';
import { themeMgr } from '../../../managers/ThemeManager.js';

export class HomeView extends BaseView {
    constructor(container) {
        super(container);
        this.state = {
            theme: themeMgr.current,
            user: null,
            remainingTime: -1,
            results: []
        };
        this.timerInterval = null;
    }

    onMounted() {
        this.showLoader();
        apiMgr.authApi.get('/user')
            .then(res => {
                this.state.user = res.data;
                this.state.remainingTime = this._calcaulate(tokenMgr.getAccessToken());
                this._startTimer();
                this.updateView();
            })
            .catch(err => {
                console.error("Home Init Failed", err);
            })
            .finally(() => {
                this.hideLoader();
            });
    }

    _startTimer() {
        this.timerInterval = setInterval(() => {
            if (this.state.remainingTime > 0) {
                this.state.remainingTime--;
                this.updateView(); // Reactive update
            } else {
                this._stopTimer();
            }
        }, 1000);
    }

    _calcaulate(token) {
        if (!token) return -1;
        try {
            // JWT format: header.payload.signature
            const base64Url = token.split('.')[1]; 
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const payload = JSON.parse(window.atob(base64));
            const now = Math.floor(Date.now() / 1000); // Current time in seconds
            return payload.exp - now; // Time remaining
        } catch (e) {
            return -1;
        }
    }

    _stopTimer() {
        if (this.timerInterval) clearInterval(this.timerInterval);
    }

    onUnmount() {
        this._stopTimer(); // Prevent memory leaks
    }

    template() {
        return HomeTemplate(this.state, {
            onLogout: () => this.handleLogout(),
            onToggleTheme: (checked) => { this.toggleTheme(checked) },
        });
    }

    async handleLogout() {
        this.showLoader();
        try {
            const rt = tokenMgr.getRefreshToken();
            const res = await apiMgr.tokenApi.post("/logout", {
                refreshToken: rt,
            });
            console.log(`[Logout] Logged out, tokens to be cleared!`);
        } catch (err) {
            console.error("[Logout] Failed", err);
        } finally {
            this.hideLoader();
            await tokenMgr.clearTokens();
        }
    }
}