import { LitElement, html, css } from 'lit';
import { themeManager } from '../managers/ThemeManager.js';
import { tokenManager } from '../managers/TokenManager.js';
import { LifecycleHub } from '../helpers/LifecycleHub.js';
import { Router } from './Router.js';

export class AppShell extends LitElement {
    static styles = css`
        :host { display: block; height: 100vh; width: 100vw; }
        .app-container { display: flex; flex-direction: column; height: 100%; }
        #outlet { flex: 1; overflow-y: auto; }
        
        .loader-overlay {
            position: fixed; top: 0; left: 0; width: 100%; height: 100%;
            background: rgba(0, 0, 0, 0.7); display: flex; 
            flex-direction: column; align-items: center; justify-content: center;
            z-index: 9999;
        }
    `;

    constructor() {
        super();
        this.router = null;
        this._lastAuthState = null; // Track changes to prevent route-looping

        // LifecycleHubs: attach() on connect, detach() on disconnect
        this.theme = new LifecycleHub(this, themeManager.theme$);
        this.auth = new LifecycleHub(this, tokenManager.isAuthenticated$);
        
        this.loading = { show: false, message: 'Processing...' };
    }

    /**
     * Replaces Android's onStart.
     */
    connectedCallback() {
        super.connectedCallback();
        this._setupSystemListeners();
    }

    /**
     * The first time the UI is actually ready (Shadow DOM is accessible).
     */
    firstUpdated() {
        console.debug(`AppShell: firstUpdated`);
        const outlet = this.shadowRoot.getElementById('outlet');
        this.router = new Router(outlet);
        
        // Initial route check
        if (this.auth.value) {
            this._handleAuthRouting(this.auth.value);
        }
    }

    _setupSystemListeners() {
        // Handle BFcache (Back-Forward Cache) from your v2
        window.addEventListener('pageshow', (e) => {
            console.log("AppShell: pageshow", e.persisted ? "(from cache)" : "(new)");
            if (e.persisted) {
                // If coming back from cache, we might need to re-sync Managers
                // or force a route check.
                this._handleAuthRouting(this.auth.value);
            }
        });

        document.addEventListener('visibilitychange', () => {
            document.visibilityState === 'visible' ? this._onVisible() : this._onInvisible();
        });

        window.addEventListener('app:loader', (e) => {
            this.loading = e.detail;
            this.requestUpdate();
        });
    }

    /**
     * Efficiently watch for Auth changes.
     */
    updated(changedProperties) {
        console.debug('AppShell: updated:', changedProperties);
        // If the auth hub value changed, check if we need to navigate
        const currentAuth = this.auth.value?.isAuth;
        if (currentAuth !== this._lastAuthState) {
            this._lastAuthState = currentAuth;
            this._handleAuthRouting(this.auth.value);
        }
    }

    _handleAuthRouting(authState) {
        if (!this.router || !authState) return;

        const { isAuth } = authState;
        console.log(`AppShell: Routing logic -> isAuth: ${isAuth}`);
        
        isAuth ? this.router.toHome() : this.router.toLogin();
    }

    _onVisible() { console.log("AppShell: Visible"); }
    _onInvisible() { console.log("AppShell: Invisible"); }

    render() {
        return html`
            <div class="app-container">
                <div id="outlet"></div>

                ${this.loading.show ? html`
                    <div class="loader-overlay">
                        <sl-spinner style="font-size: 3rem;"></sl-spinner>
                        <p style="color: white; margin-top: 1rem;">${this.loading.message}</p>
                    </div>
                ` : ''}
            </div>
        `;
    }
}