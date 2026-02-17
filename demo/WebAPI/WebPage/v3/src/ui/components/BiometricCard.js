import { LitElement, html, css } from 'lit';

export class BiometricCard extends LitElement {
    static styles = css`
        :host {
            display: block;
            height: 100%;
        }

        sl-card {
            width: 100%;
            height: 100%;
            --width: 100%;
            border: none;
        }

        sl-card::part(base) {
            height: 100%;
            display: flex;
            flex-direction: column;
            border: 1px solid var(--sl-color-neutral-300);
            box-shadow: var(--sl-shadow-medium);
            background-color: var(--sl-color-neutral-0);
        }

        sl-card::part(body) {
            flex: 1 1 auto;
            padding: var(--sl-spacing-large);
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
            color: var(--sl-color-indigo-500);
            font-size: 1.2rem;
        }

        /* Sub-grid for Biometric data */
        .bio-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 16px;
        }

        .stat-box {
            display: flex;
            flex-direction: column;
            gap: 4px;
        }

        .label {
            color: var(--sl-color-neutral-500);
            font-size: 0.6rem;
            text-transform: uppercase;
            font-weight: bold;
            letter-spacing: 0.5px;
        }

        .value {
            font-size: 1.1rem;
            font-weight: 600;
            color: var(--sl-color-neutral-900);
        }

        .unit {
            font-size: 0.7rem;
            color: var(--sl-color-neutral-400);
            margin-left: 2px;
        }

        .blood-badge {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            background: var(--sl-color-danger-100);
            color: var(--sl-color-danger-700);
            border-radius: 4px;
            padding: 2px 8px;
            font-weight: bold;
            font-size: 0.9rem;
        }

        .divider {
            grid-column: span 2;
            height: 1px;
            background: var(--sl-color-neutral-200);
            margin: 8px 0;
        }
    `;

    static properties = {
        user: { type: Object }
    };

    render() {
        if (!this.user) return html``;

        return html`
            <sl-card>
                <div slot="header" class="header-title">
                    <sl-icon name="activity" class="header-icon"></sl-icon> 
                    SYSTEM VITALS
                </div>

                <div class="bio-grid">
                    <div class="stat-box">
                        <span class="label">Blood Type</span>
                        <div><span class="blood-badge">${this.user.bloodGroup}</span></div>
                    </div>
                    
                    <div class="stat-box">
                        <span class="label">Height</span>
                        <div class="value">${this.user.height}<span class="unit">cm</span></div>
                    </div>

                    <div class="divider"></div>

                    <div class="stat-box">
                        <span class="label">Weight</span>
                        <div class="value">${this.user.weight}<span class="unit">kg</span></div>
                    </div>

                    <div class="stat-box">
                        <span class="label">Eye Color</span>
                        <div class="value" style="color: ${this.user.eyeColor.toLowerCase()}">
                            ${this.user.eyeColor}
                        </div>
                    </div>

                    <div class="divider"></div>

                    <div class="stat-box" style="grid-column: span 2;">
                        <span class="label">Internal SSN</span>
                        <div class="value" style="font-family: var(--sl-font-mono); font-size: 0.9rem;">
                            ${this.user.ssn.replace(/\d(?=\d{4})/g, "*")}
                        </div>
                    </div>
                </div>
            </sl-card>
        `;
    }
}

customElements.define('biometric-card', BiometricCard);