import { tokenMgr } from '../managers/TokenManager.js';
import { vaultMgr } from '../managers/VaultManager.js';
import { dpopMgr } from '../managers/DPoPManger.js';
import { apiMgr } from '../managers/ApiManager.js';
import { sessionMgr } from '../managers/SessionManager.js';
import { LaunchView } from './views/Launch/LaunchView.js';
import { LoginView } from './views/Login/LoginView.js';
import { HomeView } from './views/Home/HomeView.js';
import { Router } from './Router.js';


export class App {
    constructor(containerId) {
        this.containerId = containerId;
        this.container = null;
        this.router = null;
        this.loader = null;

        this._handleEvents();
    }

    _handleEvents() {
        window.addEventListener('DOMContentLoaded', () => this._onLoaded());
        window.addEventListener('pagehide', () => this._onPageHide());
        window.addEventListener('beforeunload', () => this._onUnloading());
        window.addEventListener('unload', () => this._onUnloaded());
        document.addEventListener('visibilitychange', () => {
        document.visibilityState === 'visible' 
            ? this._onVisible() 
            : this._onInvisible();
        });
    }

    _onLoaded() {
        console.log("App Shell: onLoaded");

        this.container = document.getElementById(this.containerId);
        this.router = new Router(this.container, [LaunchView, LoginView, HomeView]);
        this.loader = document.getElementById('app-loader');
        
        // Listen for the events triggered by BaseView's helper methods
        window.addEventListener('app:loader', (e) => {
            if (this.loader) {
                this.loader.style.display = e.detail.show ? 'flex' : 'none';
            }
        });

        this._handleInit();
    }

    _onVisible() { 
        console.log("App Shell: onVisible");
    }

    _onInvisible() { 
        console.log("App Shell: onInvisible");
    }

    _onPageHide() { 
        console.log("App Shell: onPageHide");
    }

    _onUnloading() { 
        console.log("App Shell: onUnloading");
    }

    _onUnloaded() {
        console.log("App Shell: onUnloaded");
        this.router.dispose();
    }

    /**
     * Manager Initialization
     */
    _handleInit() {
        // 1. Initial Launch View
        this.router.navigate(LaunchView);
        vaultMgr.init()
        .then(() => {
            // Step 2: Session Context (Synchronous)
            return sessionMgr.init();
        })
        .then((startIdx) => {
            // Step 3: Parallel Initialization for speed
            // Since Token and DPoP are often independent, we can run them together
            return Promise.all([
                tokenMgr.init(startIdx),
                dpopMgr.init(startIdx)
            ]);
        })
        .then(() => {
            // Step 4: Finalize API
            apiMgr.init();
            console.log("[App] System Ready.");
        })
        .catch((error) => {
            // "Crash" handler
            console.error("[App] Boot Sequence Failed:", error);
            throw error; 
        })
        .finally(() => {
            // Step 5: Where to go from here
            tokenMgr.isAuthenticated$.subscribe(authState => {
                this._handleRouting(authState);
            });
        });
    }

    /**
     * Navigation Logic
     */
    _handleRouting({ isAuth }) {
        // The Router handles the lifecycle of swapping
        const targetView = isAuth ? HomeView : LoginView;
        this.router.navigate(targetView);
    }
}