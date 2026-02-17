import { LitElement, html, css } from 'lit';

export class CryptoAssets extends LitElement {
    static styles = css`
        :host {
            display: block;
            height: 100%; /* Important for the shadow-height trick */
        }

        sl-card {
            width: 100%;
            height: 100%;
            --width: 100%;
            /* Remove default Shoelace borders/shadows from the host */
            border: none;
        }

        /* This is the secret sauce: Targeting the actual 'box' inside Shoelace */
        sl-card::part(base) {
            height: 100%;
            display: flex;
            flex-direction: column;
            
            /* Apply the border and shadow to the SAME element */
            border: 1px solid var(--sl-color-neutral-300);
            box-shadow: var(--sl-shadow-medium);
            
            /* Ensure the shadow doesn't get clipped */
            overflow: visible; 
            
            background-color: var(--sl-color-neutral-0);
        }

        /* Force the body of the card to grow to fill the height established by the shadow-box */
        sl-card::part(body) {
            flex: 1 1 auto;
        }

        .header-title {
            display: flex;
            align-items: center;
            gap: 8px;
            font-weight: bold;
            font-size: 0.8rem;
            color: var(--sl-color-neutral-700);
        }

        .header-icon {
            color: var(--sl-color-warning-500);
            font-size: 1.2rem;
        }

        .data-grid {
            display: grid;
            gap: 12px;
        }

        .label {
            color: var(--sl-color-neutral-500);
            font-size: 0.65rem;
            text-transform: uppercase;
            font-weight: bold;
        }

        .value {
            font-size: 0.9rem;
            margin-top: 2px;
        }

        /* Specialized Wallet Styling */
        .wallet-container {
            margin-top: 8px;
            padding: var(--sl-spacing-small);
            background: var(--sl-color-neutral-100);
            border-radius: var(--sl-border-radius-medium);
            border: 1px dashed var(--sl-color-neutral-300);
        }

        .wallet-address {
            font-family: var(--sl-font-mono);
            font-size: 0.75rem;
            word-break: break-all;
            color: var(--sl-color-neutral-700);
            line-height: 1.4;
        }
    `;

    static properties = {
        user: { type: Object }
    };

    render() {
        if (!this.user?.crypto) return html``;
        const { coin, wallet, network } = this.user.crypto;

        return html`
            <sl-card>
                <div slot="header" class="header-title">
                    <sl-icon name="currency-bitcoin" class="header-icon"></sl-icon> 
                    LEDGER ASSETS
                </div>

                <div class="data-grid">
                    <div>
                        <div class="label">Primary Asset</div>
                        <div class="value">${coin}</div>
                    </div>

                    <div>
                        <div class="label">Blockchain Network</div>
                        <div class="value">
                            <sl-badge variant="warning" pill pulse style="--pulse-color: var(--sl-color-warning-400)">
                                ${network}
                            </sl-badge>
                        </div>
                    </div>

                    <div>
                        <div class="label">Public Wallet Address</div>
                        <div class="wallet-container">
                            <div class="wallet-address">${wallet}</div>
                            <div style="margin-top: 8px; display: flex; justify-content: flex-end;">
                                <sl-copy-button value="${wallet}" size="small"></sl-copy-button>
                            </div>
                        </div>
                    </div>
                </div>
            </sl-card>
        `;
    }
}

customElements.define('crypto-assets', CryptoAssets);