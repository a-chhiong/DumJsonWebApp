import { apiManager } from '../managers/ApiManager.js';
import { tokenManager } from '../managers/TokenManager.js';
import { BehaviorSubject } from 'rxjs';

export class LoginService {
    constructor() {
        // State for the view to observe
        this.loading$ = new BehaviorSubject(false);
        this.error$ = new BehaviorSubject(null);
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
}

// Export a singleton instance
export const loginService = new LoginService();