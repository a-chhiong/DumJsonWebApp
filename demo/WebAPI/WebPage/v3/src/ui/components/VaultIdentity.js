import { LitElement, html, css } from 'lit';

export class VaultIdentity extends LitElement {
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
            color: var(--sl-color-success-500);
            font-size: 1.2rem;
        }

        dl {
            margin: 0;
            display: grid;
            gap: 12px;
        }

        dt {
            color: var(--sl-color-neutral-500);
            font-size: 0.65rem;
            text-transform: uppercase;
            font-weight: bold;
        }

        dd {
            margin: 0;
            font-size: 0.9rem;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .mono {
            font-family: var(--sl-font-mono);
            font-size: 0.8rem;
            color: var(--sl-color-primary-600);
        }
    `;

    static properties = {
        user: { type: Object }
    };

    /**
     * Helper: Replaces your v2 DataRow function
     */
    _renderRow(label, value, isMono = false) {
        return html`
            <div>
                <dt>${label}</dt>
                <dd class="${isMono ? 'mono' : ''}" title="${value}">${value}</dd>
            </div>
        `;
    }

    render() {
        if (!this.user) return html``;

        return html`
            <sl-card>
                <div slot="header" class="header-title">
                    <sl-icon name="shield-lock" class="header-icon"></sl-icon> 
                    IDENTITY VAULT
                </div>

                <dl>
                    ${this._renderRow('Username', this.user.username)}
                    ${this._renderRow('Internal Email', this.user.email)}
                    ${this._renderRow('Network Node (IP)', this.user.ip, true)}
                    ${this._renderRow('Organization', this.user.company?.name)}
                    ${this._renderRow('Department', this.user.company?.department)}
                </dl>
            </sl-card>
        `;
    }
}

customElements.define('vault-identity', VaultIdentity);