import { LitElement, html, css } from 'lit';

export class FinancialSlots extends LitElement {
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
            color: var(--sl-color-primary-500);
            font-size: 1.2rem;
        }

        /* Container Query Setup */

        .card-wrapper { 
            container-type: inline-size; 
            width: 100%; 
            margin-bottom: var(--sl-spacing-large);
            /* Ensure the inner card's shadow doesn't fight the outer card's border */
            padding: 4px; 
            box-sizing: border-box;
        }

        .credit-card {
            aspect-ratio: 1.586 / 1;
            width: 100%;
            border-radius: 12px;
            padding: 1.5rem;
            position: relative;
            overflow: hidden;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            color: white;
            box-shadow: var(--sl-shadow-large);
            border: 1px solid rgba(255, 255, 255, 0.1);
            box-sizing: border-box;
            transition: transform 0.3s ease;
        }

        .credit-card:hover { transform: translateY(-5px); }

        /* Glassmorphism Overlay */
        .glass {
            position: absolute; top: 0; left: 0; right: 0; height: 50%;
            background: linear-gradient(to bottom, rgba(255,255,255,0.15) 0%, rgba(255,255,255,0) 100%);
            pointer-events: none;
        }

        .card-number {
            font-family: var(--sl-font-mono);
            font-size: 1.4rem;
            letter-spacing: 2px;
            text-shadow: 0 2px 4px rgba(0,0,0,0.5);
            z-index: 1;
        }

        /* Responsive Text using Container Queries */
        @container (max-width: 300px) {
            .card-number { font-size: 1rem; }
            .card-holder { font-size: 0.7rem; }
        }

        .card-footer { display: flex; justify-content: space-between; align-items: flex-end; z-index: 1; }
        .label { font-size: 0.5rem; text-transform: uppercase; opacity: 0.8; }
        .value { font-size: 0.85rem; font-weight: bold; }

        .details-list { margin: 0; display: grid; gap: 8px; }
        dt { color: var(--sl-color-neutral-500); font-size: 0.6rem; text-transform: uppercase; font-weight: bold; }
        dd { margin: 0; font-family: var(--sl-font-mono); font-size: 0.8rem; overflow: hidden; text-overflow: ellipsis; }
    `;

    static properties = {
        user: { type: Object }
    };

    /**
     * Maps card types to their specific high-contrast gradients
     */
    _getCardTheme(type) {
        const themes = {
            'Mastercard': 'linear-gradient(135deg, #ff5f6d 0%, #ffc371 100%)',
            'Visa': 'linear-gradient(135deg, #00d2ff 0%, #3a7bd5 100%)',
            'American Express': 'linear-gradient(135deg, #5f2c82 0%, #49a09d 100%)'
        };
        return themes[type] || 'linear-gradient(135deg, #434343 0%, #000000 100%)';
    }

    render() {
        if (!this.user?.bank) return html``;
        const { bank, firstName, lastName } = this.user;
        const cardBg = this._getCardTheme(bank.cardType);

        return html`
            <sl-card>
                <div slot="header" class="header-title">
                    <sl-icon name="credit-card" class="header-icon"></sl-icon> 
                    FINANCIAL ASSETS
                </div>

                <div class="card-wrapper">
                    <div class="credit-card" style="background: ${cardBg}">
                        <div class="glass"></div>
                        
                        <div style="display: flex; justify-content: space-between; align-items: flex-start; z-index:1;">
                            <div style="width: 35px; height: 25px; background: rgba(255,255,255,0.2); border-radius: 4px;"></div>
                            <sl-icon name="wifi" style="font-size: 1.2rem;"></sl-icon>
                        </div>

                        <div class="card-number">
                            ${bank.cardNumber.replace(/\d(?=\d{4})/g, "• ")}
                        </div>

                        <div class="card-footer">
                            <div>
                                <div class="label">Card Holder</div>
                                <div class="value">${firstName} ${lastName}</div>
                            </div>
                            <div style="text-align: right;">
                                <div class="label">Expires</div>
                                <div class="value">${bank.cardExpire}</div>
                            </div>
                        </div>
                    </div>
                </div>

                <dl class="details-list">
                    <dt>IBAN</dt>
                    <dd>${bank.iban}</dd>
                    <dt>Currency</dt>
                    <dd>${bank.currency} (Active)</dd>
                </dl>
            </sl-card>
        `;
    }
}

customElements.define('financial-slots', FinancialSlots);