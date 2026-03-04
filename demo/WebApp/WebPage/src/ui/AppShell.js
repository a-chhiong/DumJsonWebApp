import { LitElement, html, css } from 'lit';
import { apiManager } from '../managers/ApiManager.js';
import { themeManager } from '../managers/ThemeManager.js';
import { tokenManager } from '../managers/TokenManager.js';
import { LifecycleHub } from '../helpers/LifecycleHub.js';
import { Router } from './Router.js';

export class AppShell extends LitElement {
    constructor() {
        super();
        this.router = null;
        
        // LifecycleHubs: attach() on connect, detach() on disconnect
        this.theme = new LifecycleHub(this, themeManager.theme$);
        this.tokenAuth = new LifecycleHub(this, tokenManager.isAuthenticated$);
        this.apiAuth = new LifecycleHub(this, apiManager.isAuthenticated$);

        // 2. Reactive Side Effects (For Logic)
        // We listen to the source$ directly. No need for updated() or _lastAuthState.
        this.tokenAuth.source$.subscribe(state =>{ 
            console.debug(`[AppShell] token auth:`, state);
            this._handleAuthRouting(state)
        });
        this.apiAuth.source$.subscribe(state =>{ 
            console.debug(`[AppShell] api auth:`, state);
            this._handleAuthRouting(state)
        });
        
        this.loading = { show: false, message: 'Processing...' };
        this.alert = { show: false, title: '', message: '' };
    }

    static styles = css`
        :host { 
            display: block; 
            /* Fill the parent (html/body) which are already dvh-aware */
            height: 100%; 
            width: 100%; 
        }

        .app-container { 
            display: flex; 
            flex-direction: column; 
            height: 100%; 
            width: 100%;
            /* Prevent internal layout shifting */
            position: relative;
        }

        #outlet { 
            flex: 1; 
            /* Allow vertical scrolling only here */
            overflow-y: auto; 
            /* iOS momentum scrolling */
            -webkit-overflow-scrolling: touch;
        }
        
        .loader-overlay {
            position: fixed; 
            inset: 0; /* Modern shorthand for top/left/right/bottom: 0 */
            background: rgba(0, 0, 0, 0.2); 
            backdrop-filter: blur(4px);
            display: flex; 
            flex-direction: column; 
            align-items: center; 
            justify-content: center;
            z-index: 9999;
        }
    `;

    connectedCallback() {
        super.connectedCallback();
        console.debug(`[AppShell] connectedCallback!`);

        this._setupSystemListeners();
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        console.debug(`[AppShell] disconnectedCallback!`);
    }

    /**
     * The first time the UI is actually ready (Shadow DOM is accessible).
     */
    firstUpdated() {
        console.debug(`[AppShell] firstUpdated!`);

        const outlet = this.shadowRoot.getElementById('outlet');
        this.router = new Router(outlet);
        
        // Initial route check
        if (this.tokenAuth.value) {
            this._handleAuthRouting(this.tokenAuth.value);
        }
    }

    _setupSystemListeners() {
        // Handle BFcache (Back-Forward Cache)
        window.addEventListener('pageshow', (e) => {
            console.log("[AppShell] pageshow", e.persisted ? "(from cache)" : "(new)");
            if (e.persisted) {
                // If coming back from cache, we might need to re-sync Managers
                // or force a route check.
                this._handleAuthRouting(this.tokenAuth.value);
            }
        });

        document.addEventListener('visibilitychange', () => {
            document.visibilityState === 'visible' 
            ? console.log("[AppShell] Visible") 
            : console.log("[AppShell] Invisible");
        });

        window.addEventListener('app:loader', (e) => {
            this.loading = e.detail;
            this.requestUpdate();
        });

        window.addEventListener('app:dialog', (e) => {
            this.alert = { show: true, ...e.detail };
            this.requestUpdate();
        });
    }

    _handleAuthRouting(authState) {
        if (!this.router || !authState) return;

        const { isAuth, token, isLogout } = authState;
        console.log(`[AppShell] Routing logic -> isAuth: ${isAuth}`);

        if (!isLogout && !isAuth && this.router.isAtHome) {
            this.alert = {
                show: true,
                title: 'Session Expired',
                message: 'Your session has timed out. Please log in again.'
            };
            this.requestUpdate();
            return;
        }
        
        isAuth ? this.router.toHome() : this.router.toLogin();
    }

    _onClickDialog() {
        this.alert.show = false;
        this.router.toLogin();
        this.requestUpdate();
    }

    render() {
        return html`
            <div class="app-container">
                <div id="outlet"></div>

                <loader-overlay 
                    .show=${this.loading.show} 
                    .message=${this.loading.message}>
                </loader-overlay>

                <sl-dialog 
                    label="${this.alert.title}" 
                    ?open="${this.alert.show}" 
                    @sl-after-hide="${this._onClickDialog}">
                    <p>${this.alert.message}</p>
                    <sl-button slot="footer" variant="primary" @click="${this._onClickDialog}">
                        OK
                    </sl-button>
                </sl-dialog>
            </div>
        `;
    }
}