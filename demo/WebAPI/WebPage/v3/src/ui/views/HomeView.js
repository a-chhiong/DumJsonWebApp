import { LitElement, html, css } from 'lit';
import { homeService } from '../../services/HomeService.js';
import { themeManager } from '../../managers/ThemeManager.js';
import { LifecycleHub } from '../../helpers/LifecycleHub.js';
import { Theme } from '../../constants/Theme.js';

// Import our specialized components
import '../components/ProfileHeader.js';
import '../components/VaultIdentity.js';
import '../components/BiometricCard.js';
import '../components/FinancialSlots.js';
import '../components/CryptoAssets.js';

export class HomeView extends LitElement {
    static styles = css`
        :host { display: block; animation: fadeIn 0.4s ease-out; }
        .container { max-width: 1400px; margin: 0 auto; padding: var(--sl-spacing-large); }

        .grid-layout {
            display: grid;
            gap: var(--sl-spacing-large);
            /* 1. Default for Mobile: Force exactly 2 columns */
            grid-template-columns: repeat(2, 1fr);
        }

        /* 2. Tablet-ish: Move to 3 columns if space allows */
        @media (min-width: 900px) {
            .grid-layout {
                grid-template-columns: repeat(3, 1fr);
            }
        }

        /* 3. Desktop: Cap at 4 columns */
        @media (min-width: 1200px) {
            .grid-layout {
                grid-template-columns: repeat(4, 1fr);
            }
        }

        @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
    `;

    constructor() {
        super();
        this.userHub = new LifecycleHub(this, homeService.user$);
        this.timeHub = new LifecycleHub(this, homeService.remainingTime$);
        this.themeHub = new LifecycleHub(this, themeManager.theme$);
    }

    connectedCallback() {
        super.connectedCallback();
        homeService.initDashboard();
    }

    render() {
        const user = this.userHub.value;
        if (!user) return this._renderDecrypting();

        return html`
            <div class="container">
                <profile-header 
                    .user=${user} 
                    .timeLeft=${this.timeHub.value}
                    .isDark=${this.themeHub.value === Theme.DARK}>
                </profile-header>

                <main class="grid-layout">
                    <vault-identity .user=${user}></vault-identity>
                    <biometric-card .user=${user}></biometric-card>
                    <crypto-assets .user=${user}></crypto-assets>
                    <financial-slots .user=${user}></financial-slots>
                </main>
            </div>
        `;
    }

    _renderDecrypting() {
        return html`
            <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; height: 70vh; gap: 1.5rem;">
                <sl-spinner style="font-size: 3.5rem;"></sl-spinner>
                <code>ACCESSING ENCRYPTED VAULT...</code>
            </div>
        `;
    }
}
customElements.define('home-view', HomeView);