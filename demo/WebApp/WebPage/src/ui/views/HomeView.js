import { LitElement, html, css } from 'lit';
import { BaseView } from './BaseView.js';
import { LifecycleHub } from '../../helpers/LifecycleHub.js';
import { HomeViewModel } from '../../viewmodels/HomeViewModel.js';
import { Theme } from '../../constants/Theme.js';

export class HomeView extends BaseView {
    constructor() {
        super();
        // Create a FRESH instance for THIS view only
        this.viewModel = new HomeViewModel();
        this.themeHub = new LifecycleHub(this, this.viewModel.theme$);
        this.userHub = new LifecycleHub(this, this.viewModel.user$);
        this.timeHub = new LifecycleHub(this, this.viewModel.remainingTime$);
    }

    static styles = css`
        :host { display: block; animation: fadeIn 0.4s ease-out; }
        .container { max-width: 1400px; margin: 0 auto; padding: var(--sl-spacing-large); }

        .grid-layout {
            display: grid;
            gap: var(--sl-spacing-large);
            /* 1. Base Strategy (Mobile First): Exactly 1 column */
            grid-template-columns: 1fr;
        }

        /* 2. Small Tablets / Large Phones: 2 columns */
        @media (min-width: 600px) {
            .grid-layout {
                grid-template-columns: repeat(2, 1fr);
            }
        }

        /* 3. Standard Tablet/Laptop: 3 columns */
        @media (min-width: 900px) {
            .grid-layout {
                grid-template-columns: repeat(3, 1fr);
            }
        }

        /* 4. Desktop: Cap at 4 columns */
        @media (min-width: 1200px) {
            .grid-layout {
                grid-template-columns: repeat(4, 1fr);
            }
        }

        @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
    `;

    render() {
        const user = this.userHub.value;
        if (!user) return this._renderDecrypting();

        return html`
            <div class="container">
                <profile-header 
                    .user=${user} 
                    .timeLeft=${this.timeHub.value}
                    .isDark=${this.themeHub.value === Theme.DARK}
                    @toggle-theme=${() => this.viewModel.toggleTheme()}
                    @logout-requested=${() => this.viewModel.logout()}>
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