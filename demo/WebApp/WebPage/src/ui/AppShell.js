import { LitElement, html, css } from 'lit';
import { Subject, takeUntil, filter } from 'rxjs';
import { apiManager } from '../managers/ApiManager.js';
import { tokenManager } from '../managers/TokenManager.js';
import { Router } from './Router.js';

export class AppShell extends LitElement {
    constructor() {
        super();
        this.router = null;
        this.loading = { show: false, message: 'Processing...' };
        this.alert = { show: false, title: '', message: '' };

        this._onLoader = (e) => {
            this.loading = e.detail;
            this.requestUpdate();
        };

        this._onDialog = (e) => {
            this.alert = { show: true, ...e.detail };
            this.requestUpdate();
        };

        this._onVisibility = () => {
            console.log(document.visibilityState === 'visible' ? "[AppShell] Visible" : "[AppShell] Invisible");
        };

        this._destroy$ = new Subject(); // The "Kill Switch"
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

        this._addListeners();
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        console.debug(`[AppShell] disconnectedCallback!`);

        this._removeListener();

        this._destroy$.next();
        this._destroy$.complete();
    }

    /**
     * The first time the UI is actually ready (Shadow DOM is accessible).
     */
    firstUpdated() {
        console.debug(`[AppShell] firstUpdated!`);

        const outlet = this.shadowRoot.getElementById('outlet');
        this.router = new Router(outlet);

        tokenManager.isAuthenticated$
            .pipe(takeUntil(this._destroy$))
            .subscribe(state => {
                this._handleAuthRouting(state)
            });

        apiManager.isAuthenticated$
            .pipe(takeUntil(this._destroy$), filter(state => state.isAuth !== null))
            .subscribe(state => { 
                this._handleAuthRouting(state)
            });
    }

    updated(changedProperties) {
        console.debug('[AppShell] updated:', changedProperties);
    }

    _addListeners() {
        document.addEventListener('visibilitychange', this._onVisibility);
        window.addEventListener('app:loader', this._onLoader);
        window.addEventListener('app:dialog', this._onDialog);
    }

    _removeListener() {
        document.removeEventListener('visibilitychange', this._onVisibility);
        window.removeEventListener('app:loader', this._onLoader);
        window.removeEventListener('app:dialog', this._onDialog);
    }

    _handleAuthRouting(authState) {
        if (!this.router || !authState) return;

        const { isAuth, _, isLogout } = authState;
        console.log(`[AppShell] Routing logic -> isAuth: ${isAuth}`);
        console.log(`[AppShell] Routing logic -> isLogout: ${isLogout}`);

        if (isAuth === false) {
            if (this.router.isAtSecuredView === true) {
                if (isLogout) {
                    this.router.toLogin();
                } else {
                    this.alert = {
                        show: true,
                        title: 'Session Expired',
                        message: 'Your session has timed out. Please log in again.'
                    };
                    this.requestUpdate();
                }
                return;
            }
        }
        
        isAuth === true ? this.router.toHome() : this.router.toLogin();
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