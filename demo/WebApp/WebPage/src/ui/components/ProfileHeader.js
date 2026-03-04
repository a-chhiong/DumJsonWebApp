import { LitElement, html, css } from 'lit';

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

        .access-timer {
            font-family: var(--sl-font-mono);
            font-size: 0.75rem;
            font-weight: 500; 
            display: flex;
            align-items: center;
            gap: 4px;
        }
        
        .session-timer { 
            font-family: var(--sl-font-mono); 
            font-size: 0.75rem; 
            font-weight: 700; 
            display: flex;
            align-items: center;
            margin: 4px 0;
        }

        .danger { color: var(--sl-color-danger-600); }
        .success { color: var(--sl-color-success-600); }
        .warning { color: var(--sl-color-warning-600); }

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

        /* Pulse animation for when session is critical */
        .pulse { animation: pulse-red 1s infinite; }
        @keyframes pulse-red {
            0% { opacity: 1; }
            50% { opacity: 0.5; }
            100% { opacity: 1; }
        }
    `;

    static properties = {
        user: { type: Object },
        accessLeft: { type: Number },
        sessionLeft: { type: Number },
        isDark: { type: Boolean }
    };

    _formatTime(seconds) {
        if (seconds <= 0) return 'EXPIRED';
        
        const m = Math.floor(seconds / 60);
        const s = seconds % 60;
        return m > 0 ? `${m}m ${s}s` : `${s}s`;
    }

    _getTimerClass(seconds, criticalThreshold) {
        if (seconds <= 0) return 'danger'; // Solid red when expired
        if (seconds < criticalThreshold) return 'warning pulse'; // Pulsing red when critical
        return 'success'; // Green otherwise
    }

    _handleReloadClick() {
        this.dispatchEvent(new CustomEvent('reload-requested', {
            bubbles: true,
            composed: true
        }));
    }

    _toggleTheme() {
        this.dispatchEvent(new CustomEvent('toggle-theme', {
            bubbles: true,
            composed: true
        }));
    }

    _handleLogoutClick() {
        this.dispatchEvent(new CustomEvent('logout-requested', {
            bubbles: true,
            composed: true
        }));
    }

    render() {
        if (!this.user) return html``;
        
        const isSessionExpired = this.sessionLeft <= 0;
        const isReloadDisabled = this.accessLeft > 0;

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

                <div>
                    <div class="security-status">
                        <span style="font-size: 0.6rem; text-transform: uppercase; letter-spacing: 1.5px; opacity: 0.7;">
                            ${isSessionExpired ? 'Access Revoked' : 'Session TTL'}
                        </span>

                        <div class="access-timer ${this._getTimerClass(this.accessLeft, 15)}">
                            Access: ${this._formatTime(this.accessLeft)}
                        </div>
                        <div class="session-timer ${this._getTimerClass(this.sessionLeft, 60)}">
                            Session: ${this._formatTime(this.sessionLeft)}
                        </div>
                    </div>

                    <div class="action-group">
                        <sl-button 
                            class="action-btn"
                            variant="default" 
                            size="small" 
                            circle 
                            outline 
                            title="Reload Page"
                            ?disabled=${isReloadDisabled}
                            @click=${this._handleReloadClick}>
                            <sl-icon name="arrow-repeat"></sl-icon>
                        </sl-button>

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
                            @click=${this._handleLogoutClick}>
                            <sl-icon name="box-arrow-right"></sl-icon>
                        </sl-button>
                    </div>
                </div>
            </header>
        `;
    }
}