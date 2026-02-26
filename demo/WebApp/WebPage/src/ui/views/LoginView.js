import { LitElement, html, css } from 'lit';
import { BaseView } from './BaseView.js';
import { LifecycleHub } from '../../helpers/LifecycleHub.js';
import { LoginViewModel } from '../../viewmodels/LoginViewModel.js';
import { Theme } from '../../constants/Theme.js';

export class LoginView extends BaseView {
    constructor() {
        super();
        this.username = '';
        this.password = '';

        this.viewModel = new LoginViewModel();
        this.themeHub = new LifecycleHub(this, this.viewModel.theme$);
        this.loadingHub = new LifecycleHub(this, this.viewModel.loading$);
        this.errorHub = new LifecycleHub(this, this.viewModel.error$);

        this.testUsers = [
            { name: 'Emily Johnson', user: 'emilys', pass: 'emilyspass', role: 'Admin' },
            { name: 'Michael Williams', user: 'michaelw', pass: 'michaelwpass', role: 'Admin' },
            { name: 'Sophia Brown', user: 'sophiab', pass: 'sophiabpass', role: 'Admin' },
            { name: 'James Davis', user: 'jamesd', pass: 'jamesdpass', role: 'Admin' },
            { name: 'Emma Miller', user: 'emmaj', pass: 'emmajpass', role: 'Admin' },
            { name: 'Olivia Wilson', user: 'oliviaw', pass: 'oliviawpass', role: 'Mod' },
            { name: 'Alexander Jones', user: 'alexanderj', pass: 'alexanderjpass', role: 'Mod' },
            { name: 'Ava Taylor', user: 'avat', pass: 'avatpass', role: 'Mod' },
            { name: 'Ethan Martinez', user: 'ethanm', pass: 'ethanmpass', role: 'Mod' },
            { name: 'Isabella Anderson', user: 'isabellad', pass: 'isabelladpass', role: 'Mod' }
        ];
    }

    static properties = {
        username: { type: String },
        password: { type: String }
    };

    static styles = css`
        :host {
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            width: 100%;
            background: radial-gradient(circle at center, var(--sl-color-neutral-100) 0%, var(--sl-color-neutral-50) 100%);
            transition: background 0.5s ease;
        }

        :host([theme="dark"]) {
            background: radial-gradient(circle at center, var(--sl-color-neutral-100) 0%, var(--sl-color-neutral-0) 100%);
        }

        sl-card {
            width: 100%;
            max-width: 420px;
            box-shadow: var(--sl-shadow-x-large);
            margin: var(--sl-spacing-large);
        }

        /* The Horizontal Header Logic */
        div[slot="header"] {
            display: flex;
            flex-direction: row;
            justify-content: space-between;
            align-items: center;
            width: 100%;
        }

        div[slot="header"] strong {
            font-size: 1.1rem;
            white-space: nowrap;
            line-height: 1;
        }

        .theme-toggle-btn {
            font-size: 1.1rem;
            /* Ensures no extra margin bumps it off line */
            margin: 0; 
        }

        .dev-tools {
            background: var(--sl-color-neutral-50);
            padding: var(--sl-spacing-small);
            border-radius: var(--sl-border-radius-medium);
            margin-bottom: var(--sl-spacing-large);
            border: 1px dashed var(--sl-color-neutral-400);
        }

        .dev-label {
            display: block;
            font-size: 0.6rem;
            font-weight: bold;
            color: var(--sl-color-neutral-500);
            margin-bottom: 4px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }

        .form {
            display: flex;
            flex-direction: column;
            gap: var(--sl-spacing-medium);
        }

        .role-header {
            padding: var(--sl-spacing-x-small) var(--sl-spacing-small);
            font-size: var(--sl-font-size-x-small);
            font-weight: var(--sl-font-weight-bold);
            color: var(--sl-color-neutral-500);
            background: var(--sl-color-neutral-100);
            text-transform: uppercase;
        }

        sl-button[variant="primary"] {
            width: 100%;
        }

        sl-alert {
            margin-top: var(--sl-spacing-small);
        }
    `;

    _handleQuickSelect(e) {
        const selectedValue = e.target.value;
        const found = this.testUsers.find(u => u.user === selectedValue);
        if (found) {
            this.username = found.user;
            this.password = found.pass;
        }
    }

    _handleLogin() {
        this.viewModel.login(this.username, this.password);
    }

    render() {
        const isDark = this.themeHub.value === Theme.DARK;
        const isLoading = this.loadingHub.value;
        const error = this.errorHub.value;

        this.setAttribute('theme', isDark ? 'dark' : 'light');

        return html`
            <sl-card>
                <div slot="header">
                    <strong style="font-size: 1.1rem;">DummyJSON Demo Web APP</strong>
                    
                    <sl-button 
                        class="theme-toggle-btn"
                        variant="default" 
                        size="small" 
                        circle 
                        outline 
                        @click=${() => this.viewModel.toggleTheme()}>
                        <sl-icon name="${isDark ? 'moon' : 'sun'}"></sl-icon>
                    </sl-button>
                </div>

                <div class="form">
                    <div class="dev-tools">
                        <span class="dev-label">Dev Quick Login</span>
                        <sl-select placeholder="Select test identity" @sl-change=${this._handleQuickSelect} pill size="small">
                            <sl-icon name="bug" slot="prefix"></sl-icon>
                            <div class="role-header">Administrators</div>
                            ${this.testUsers.filter(u => u.role === 'Admin').map(u => html`
                                <sl-option value="${u.user}">
                                    <sl-icon slot="prefix" name="shield-check" style="color: var(--sl-color-primary-500);"></sl-icon>
                                    ${u.name}
                                </sl-option>
                            `)}
                            <sl-divider></sl-divider>
                            <div class="role-header">Moderators</div>
                            ${this.testUsers.filter(u => u.role === 'Mod').map(u => html`
                                <sl-option value="${u.user}">
                                    <sl-icon slot="prefix" name="person-gear" style="color: var(--sl-color-warning-600);"></sl-icon>
                                    ${u.name}
                                </sl-option>
                            `)}
                        </sl-select>
                    </div>

                    <sl-input 
                        label="Username" 
                        placeholder="Enter username"
                        .value=${this.username} 
                        @sl-input=${e => this.username = e.target.value}>
                    </sl-input>

                    <sl-input 
                        label="Password" 
                        type="password" 
                        placeholder="Enter password"
                        password-toggle
                        .value=${this.password} 
                        @sl-input=${e => this.password = e.target.value}>
                    </sl-input>
                    
                    ${error ? html`<sl-alert variant="danger" open>${error}</sl-alert>` : ''}
                </div>

                <sl-button 
                    slot="footer" 
                    variant="primary" 
                    ?loading=${isLoading}
                    @click=${this._handleLogin}>
                    Authenticate
                </sl-button>
            </sl-card>
        `;
    }
}