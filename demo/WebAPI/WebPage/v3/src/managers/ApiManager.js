import { tokenManager } from './TokenManager.js';
import { dpopManager } from './DPoPManger.js';
import { stateHub } from '../helpers/EventHub.js';
import axios from 'axios';

/**
 * ApiManager.js
 * Responsible for Axios Interceptors
 */

class ApiManager {
    constructor() {
        // Example: derive base URL dynamically
        const origin = window.location.origin;
        this._BASE_URL = `${origin}/api`;
        this._isInitialised = false;
        this._refreshPromise = null;
    }

    // 2. Expose via Getters
    get authApi() {
        if (!this._isInitialised) 
            console.warn("🚨 Accessing authApi before init()!");
        return this._authApi;
    }

    get anonApi() { 
        if (!this._isInitialised) 
            console.warn("🚨 Accessing anonApi before init()!");
        return this._anonApi; 
    }
    get tokenApi() { 
        if (!this._isInitialised) 
            console.warn("🚨 Accessing tokenApi before init()!");
        return this._tokenApi; 
    }

    init() {
        if (this._isInitialised) return;

        this._initApi();
        this._initRequest();
        this._initResponse();

        // Listen for session updates (Local or Global)
        stateHub.hear('SESSION_SYNC').subscribe(async (data) => {
            console.log(`[ApiManager] Context Switch: Targeting Slot ${data.idx}`);
            // Clear the lock so a new account doesn't wait on an old account's refresh
            this._refreshPromise = null;
        });

        this._isInitialised = true;
    }

    _initApi() {
        const config = {
            baseURL: this._BASE_URL,
            headers: { "Content-Type": "application/json" },
        };
        this._anonApi = axios.create(config);
        this._authApi = axios.create(config);
        this._tokenApi = axios.create(config);
    }

    _initRequest() {
        this._tokenApi.interceptors.request.use(
            async (config) => {
                config.url = `/token${config.url}`
                try {
                    const htm = config.method || "GET";
                    const htu = `${this._BASE_URL}${config.url}`;
                    let dpopProof = await dpopManager.createDPoP(htm, htu);
                    config.headers.DPoP = dpopProof;
                    return config;
                } catch (e) {
                    console.warn(`[ApiManager] Request Error: ${e}`);
                    return Promise.reject(e); 
                }
            },
            (error) => Promise.reject(error)
        );
        this._authApi.interceptors.request.use(
            async (config) => {
                try {
                    const token = tokenManager.getAccessToken();
                    config.headers.Authorization = `DPoP ${token}`;
                    config._sentWithToken = token;
                    const htm = config.method || "GET";
                    const htu = `${this._BASE_URL}${config.url}`;
                    // Bound proof with ATH
                    const dpopProof = await dpopManager.createDPoP(htm, htu, token);
                    config.headers.DPoP = dpopProof;
                    return config;
                } catch (e) {
                    console.warn(`[ApiManager] Request Error: ${e}`);
                    return Promise.reject(e); 
                }
            },
            (error) => Promise.reject(error)
        );
    }

    _initResponse() {
        this._authApi.interceptors.response.use(
            (response) => response,
            async (error) => {
                const originalRequest = error.config;
                if (!originalRequest) return Promise.reject(error);

                // Handle 401 Unauthorized
                if (error.response?.status === 401 && !originalRequest._retry) {
                    originalRequest._retry = true;

                    const currentToken = tokenManager.getAccessToken();
                    // 1. Check for Context Switch
                    if (currentToken !== null) {
                        if (originalRequest._sentWithToken !== currentToken) {
                           return Promise.reject(new Error("Context changed in flight"));
                        }
                    }
                    
                    // 2. The Atomic Lock:
                    // If a promise already exists, everyone waits for THAT specific promise.
                    if (this._refreshPromise) {
                        return this._refreshPromise.then(async newToken => {
                            originalRequest.headers.Authorization = `DPoP ${newToken}`;
                            const htm = originalRequest.method || "GET";
                            const htu = `${this._BASE_URL}${originalRequest.url}`;
                            const dpopProof = await dpopManager.createDPoP(htm, htu, newToken);
                            originalRequest.headers.DPoP = dpopProof;
                            return this._authApi(originalRequest);
                        });
                    }

                    // 3. Start the Refresh and store the Promise
                    this._refreshPromise = (async () => {
                        try {
                            const rt = tokenManager.getRefreshToken();
                            if (!rt) throw new Error("No refresh token for this slot");

                            console.log(`[ApiManager] Refreshing token chain...`);
                            const res = await this._tokenApi.post('/refresh', { 
                                refreshToken: rt,
                            });
                            
                            const { accessToken, refreshToken } = res.data;
                            await tokenManager.saveTokens(accessToken, refreshToken);
                            console.log(`[ApiManager] Refreshing token done!`);

                            // Resolve the lock with the new token
                            return accessToken; 
                        } catch (err) {
                            await tokenManager.clearTokens();
                            throw err;
                        } finally {
                            // Release the lock so future 401s (much later) can refresh again
                            this._refreshPromise = null;
                        }
                    })();

                    // The "First" request also waits for the lock it just created
                    return this._refreshPromise.then(async newToken => {
                        originalRequest.headers.Authorization = `DPoP ${newToken}`;
                        const htm = originalRequest.method || "GET";
                        const htu = `${this._BASE_URL}${originalRequest.url}`;
                        const dpopProof = await dpopManager.createDPoP(htm, htu, newToken);
                        originalRequest.headers.DPoP = dpopProof;
                        return this._authApi(originalRequest);
                    });
                }
                return Promise.reject(error);
            }
        );
    }
}

// Export a single instance (Singleton)
export const apiManager = new ApiManager();