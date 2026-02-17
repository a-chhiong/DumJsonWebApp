import { LitElement, html, css } from 'lit';
import { homeService } from '../../services/HomeService.js';
import { themeManager } from '../../managers/ThemeManager.js';
import { Theme } from '../../constants/Theme.js';

export class ProfileHeader extends LitElement {
    static styles = css`
        :host { display: block; }
        
        .header {
            display: flex;
            justify-content: space-between;
            align-items: flex-end;
            margin-bottom: var(--sl-spacing-2x-large);
            padding-bottom: var(--sl-spacing-medium);
            border-bottom: 1px solid var(--sl-color-neutral-200);
        }

        .profile-brand { display: flex; align-items: center; gap: var(--sl-spacing-medium); }
        .security-status { text-align: right; }
        
        .heartbeat { 
            font-family: var(--sl-font-mono); 
            font-size: 1.8rem; 
            font-weight: 700; 
            line-height: 1;
            margin: 4px 0;
        }

        .danger { color: var(--sl-color-danger-600); }
        .success { color: var(--sl-color-success-600); }

        .action-group {
            display: flex;
            gap: 12px;
            margin-top: 8px;
            justify-content: flex-end;
            align-items: center;
        }

        /* Forces both buttons to be exactly the same size */
        .action-btn {
            font-size: 1.1rem;
        }
    `;

    static properties = {
        user: { type: Object },
        timeLeft: { type: Number },
        isDark: { type: Boolean }
    };

    _getTimerContent() {
        if (this.timeLeft < 0) return `SYNCING...`;
        if (this.timeLeft === 0) return `EXPIRED`;
        return `${this.timeLeft}s`;
    }

    _toggleTheme() {
        const newTheme = this.isDark ? Theme.LIGHT : Theme.DARK;
        themeManager.setTheme(newTheme);
    }

    render() {
        if (!this.user) return html``;
        const isLowTime = this.timeLeft > 0 && this.timeLeft < 15;
        const isExpired = this.timeLeft <= 0;

        return html`
            <header class="header">
                <div class="profile-brand">
                    <sl-avatar image="${this.user.image}" style="--size: 4.5rem;"></sl-avatar>
                    <div>
                        <h2 style="margin: 0;">${this.user.firstName} ${this.user.lastName}</h2>
                        <sl-badge variant="neutral" pill style="margin-top: 8px;">
                            ${this.user.role.toUpperCase()}
                        </sl-badge>
                    </div>
                </div>

                <div class="security-status">
                    <span style="font-size: 0.6rem; text-transform: uppercase; letter-spacing: 1.5px; opacity: 0.7;">
                        ${isExpired ? 'Access Revoked' : 'Session TTL'}
                    </span>
                    <div class="heartbeat ${isLowTime || isExpired ? 'danger' : 'success'}">
                        ${this._getTimerContent()}
                    </div>
                    
                    <div class="action-group">
                        <sl-button 
                            class="action-btn"
                            variant="default" 
                            size="small" 
                            circle 
                            outline 
                            title="Toggle Theme"
                            @click=${this._toggleTheme}>
                            <sl-icon name="${this.isDark ? 'moon' : 'sun'}"></sl-icon>
                        </sl-button>

                        <sl-button 
                            class="action-btn"
                            variant="danger" 
                            size="small" 
                            circle 
                            outline 
                            title="Logout"
                            @click=${() => homeService.logout()}>
                            <sl-icon name="box-arrow-right"></sl-icon>
                        </sl-button>
                    </div>
                </div>
            </header>
        `;
    }
}
customElements.define('profile-header', ProfileHeader);