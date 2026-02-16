import { render, html } from 'html'; // Using your lit-html import
import { tokenMgr } from '../managers/TokenManager.js';
import { vaultMgr } from '../managers/VaultManager.js';
import { dpopMgr } from '../managers/DPoPManger.js';
import { apiMgr } from '../managers/ApiManager.js';
import { sessionMgr } from '../managers/SessionManager.js';
import { Router } from './Router.js';

export class App {
    constructor(containerId) {
        this.containerId = containerId;
        this.container = null;
        this.router = null;
        this.loaderElement = null;
        this._subs = [];
        this._setupListeners();
    }

    _setupListeners() {
        window.addEventListener('DOMContentLoaded', () => this._onLoaded());
        window.addEventListener('pageshow', (event) => this._onPageShow(event));
        window.addEventListener('pagehide', (event) => this._onPageHide(event));
        document.addEventListener('visibilitychange', () => {
            document.visibilityState === 'visible' ? this._onVisible() : this._onInvisible();
        });
    }

    _onLoaded() {
        console.log("App: onLoaded");
        this.container = document.getElementById(this.containerId);
        
        // 1. Programmatically create the Global Loader (Clean Body)
        this._initLoader();

        // 2. Initialize FragmentManager (Router)
        this.router = new Router(this.container);
    }

    _onPageShow(event) {
        console.log("App: onPageShow");

        // 1. If we are coming back from bfcache, we don't need to re-init managers
        if (event.persisted) {
            return; 
        }

        // 2. Initial Splash Fragment
        this.router.toLaunch();
        
        // 3. Start Managers
        this._initManagers()
            .then(() => {
                // Subscribe to Auth state to handle navigation (Intents)
                const authSub = tokenMgr.isAuthenticated$.subscribe(o => {
                    const {isAuth, token} = o;
                    console.log(`isAuth? ${isAuth}`);
                    isAuth ? this.router.toHome() : this.router.toLogin();
                });
                this._subs.push(authSub);
            })
            .catch(err => console.error("System Crash during boot", err));
    }

    _onPageHide(event) { 
        console.log("App: onPageHide");
        // Unsubscribe from RxJS stream (Like removing a Listener in onStop)
        this._clearSubscriptions();
        this.router.dispose();
    }

    _clearSubscriptions() {
        // Equivalent to CompositeDisposable.clear()
        this._subs.forEach(s => s.unsubscribe());
        this._subs = [];
    }

    _initManagers() {
        return vaultMgr.init()
            .then(() => sessionMgr.init())
            .then(idx => Promise.all([tokenMgr.init(idx), dpopMgr.init(idx)]))
            .then(() => apiMgr.init());
    }

    _initLoader() {
        // Creates the loader overlay without cluttering index.html
        this.loaderElement = document.createElement('div');
        this.loaderElement.id = 'app-loader';
        this.loaderElement.className = 'loader-overlay';
        this.loaderElement.style.display = 'none';
        
        // loader template
        const renderLoader = (message = "Processing...") => html`
            <div class="spinner"></div>
            <p>${message}</p>
        `;
        // Injecting a simple template via lit-html
        render(renderLoader(), this.loaderElement);

        document.body.appendChild(this.loaderElement);

        // Listen for EventBus signals to show/hide
        window.addEventListener('app:loader', (e) => {
            const { show, message } = e.detail;
            this.loaderElement.style.display = show ? 'flex' : 'none';
            if (show && message) {
                render(renderLoader(message), this.loaderElement);
            }
        });
    }

    _onVisible() { console.log("App: onVisible"); }
    _onInvisible() { console.log("App: onInvisible"); }
}