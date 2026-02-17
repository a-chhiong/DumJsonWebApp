import { html } from 'html';
import { Theme } from '../../../constants/Theme.js';

export const LoginTemplate = (state, actions) => {
  const { theme, username, password, isLoading, error } = state;
  const isDark = theme === Theme.DARK;
  const styles = {
    overlay: `
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      padding: var(--sl-spacing-large);
      background-color: var(--sl-color-neutral-50);
    `,
    card: `
      width: 100%;
      max-width: 400px;
      box-shadow: var(--sl-shadow-x-large);
    `,
    form: `
      display: flex;
      flex-direction: column;
      gap: var(--sl-spacing-medium);
    `
  };

  return html`
    <div class style="${styles.overlay}">
      <sl-card style="${styles.card}">
        <div slot="header" class="flex justify-between items-center">
          <h2>DummyJSON Demo</h2>
          <br><br>
          <sl-switch 
            ?checked="${isDark}"
            @sl-change=${e => actions.onToggleTheme(e.target.checked)}>
            Dark Mode
          </sl-switch>
        </div>

        <div style="${styles.form}">
          <sl-input 
            label="Username" 
            .value=${username} 
            ?disabled=${isLoading}
            @sl-change=${e => actions.onUsernameChange(e.target.value)}>
          </sl-input>

          <sl-input 
            label="Password" 
            type="password" 
            .value=${password} 
            ?disabled=${isLoading}
            @sl-change=${e => actions.onPasswordChange(e.target.value)}>
          </sl-input>
          
          ${error ? html`<sl-alert variant="danger" open>${error}</sl-alert>` : ''}
          
        </div>

        <div slot="footer">
          <sl-button 
            variant="primary" 
            ?disabled=${isLoading} 
            ?loading=${isLoading}
            @click=${actions.onLogin}>
            Secure Login
          </sl-button>
        </div>
        
      </sl-card>
    </div>
  `;
};
