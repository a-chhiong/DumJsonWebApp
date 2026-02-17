import { html } from 'html';
import { Theme } from '../../../constants/Theme.js';

export const HomeTemplate = (state, actions) => {
  const { theme, user, remainingTime, posts } = state;
  const isDark = theme === Theme.DARK;
  
  if (!user) return html`
    <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; height: 80vh; gap: 1rem;">
      <sl-spinner style="font-size: 3rem; --track-width: 6px;"></sl-spinner>
      <span style="font-family: var(--sl-font-mono); color: var(--sl-color-neutral-500);">DECRYPTING PROFILE...</span>
    </div>`;

  return html`
    <div style="${styles.container}">
      
      <header style="${styles.header}">
        <div style="display: flex; align-items: center; gap: var(--sl-spacing-medium);">
          <sl-avatar 
            image="${user.image}" 
            label="${user.firstName}" 
            shape="circle"
            style="--size: 5rem; border: 3px solid var(--sl-color-primary-500); padding: 2px;"
          ></sl-avatar>
          <div>
            <h1 style="margin: 0; font-size: var(--sl-font-size-2x-large); letter-spacing: -1px;">
              ${user.firstName} <span style="color: var(--sl-color-primary-500);">${user.lastName}</span>
            </h1>
            <div style="display: flex; gap: 8px; margin-top: 4px;">
              <sl-badge variant="neutral" pill>ID: ${user.id}</sl-badge>
              <sl-badge variant="primary" pill>${user.role}</sl-badge>
            </div>
          </div>
        </div>

        <div style="flex-grow: 1; display: flex; flex-direction: column; align-items: flex-end; min-width: 220px;">
          <div style="font-size: var(--sl-font-size-x-small); text-transform: uppercase; letter-spacing: 2px; color: var(--sl-color-neutral-500);">Security Heartbeat</div>
          <div style="font-family: var(--sl-font-mono); font-size: var(--sl-font-size-x-large); font-weight: bold; color: ${remainingTime < 10 ? 'var(--sl-color-danger-500)' : 'var(--sl-color-success-500)'}">
            ${remainingTime}s <sl-icon name="activity"></sl-icon>
          </div>
          <div style="display: flex; gap: var(--sl-spacing-small); margin-top: var(--sl-spacing-small);">
            <sl-button variant="danger" size="small" outline @click=${actions.onLogout}>
              <sl-icon slot="prefix" name="box-arrow-right"></sl-icon> Logout
            </sl-button>
            <sl-switch ?checked="${isDark}" @sl-change=${e => actions.onToggleTheme(e.target.checked)}>
              <span style="font-size: var(--sl-font-size-x-small);">Dark Mode</span>
            </sl-switch>
          </div>
        </div>
      </header>

      <main style="${styles.mainGrid}">
        ${VaultIdentity(user)}
        ${CryptoAssets(user)}
        ${FinancialSlots(user)}
      </main>

      ${posts?.length ? html`
        <div style="margin: var(--sl-spacing-2x-large) 0 var(--sl-spacing-medium); display: flex; align-items: center; gap: 10px;">
          <span style="font-weight: bold; color: var(--sl-color-neutral-600); white-space: nowrap;">LIVE ENCRYPTED STREAM</span>
          <div style="height: 1px; width: 100%; background: var(--sl-color-neutral-200);"></div>
        </div>
        <div style="${styles.postGrid}">
          ${posts.map(p => html`
            <sl-card style="--border-radius: var(--sl-border-radius-medium); border: none; box-shadow: var(--sl-shadow-x-small);">
              <div style="font-size: var(--sl-font-size-x-small); line-height: 1.5; color: var(--sl-color-neutral-700);">
                <sl-icon name="dot" style="color: var(--sl-color-primary-500);"></sl-icon> ${p.title}
              </div>
            </sl-card>
          `)}
        </div>
      ` : ''}
    </div>
  `;
};

const styles = {
    container: `
      max-width: 1400px;
      margin: 0 auto;
      padding: var(--sl-spacing-large);
      animation: fadeIn 0.4s ease-out;
    `,
    header: `
      display: flex;
      flex-wrap: wrap;
      gap: var(--sl-spacing-large);
      justify-content: space-between;
      align-items: center;
      margin-bottom: var(--sl-spacing-2x-large);
      border-bottom: 1px solid var(--sl-color-neutral-200);
      padding-bottom: var(--sl-spacing-large);
    `,
    // Aggressive Grid: Stacks to 1 column earlier (350px min)
    mainGrid: `
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
      gap: var(--sl-spacing-large);
    `,
    postGrid: `
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: var(--sl-spacing-medium);
    `,
    truncate: `
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      display: block;
    `,
    creditCard: `
      aspect-ratio: 1.586 / 1;
      width: 100%;
      min-width: 0; /* Prevents flex items from overflowing */
      background: linear-gradient(135deg, #232526 0%, #414345 100%);
      color: white;
      padding: clamp(0.8rem, 4%, 1.5rem); /* Responsive padding */
      border-radius: 12px;
      position: relative;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      box-shadow: var(--sl-shadow-large);
      margin-bottom: var(--sl-spacing-large);
      box-sizing: border-box;
    `,
    cardTextMain: `
      font-family: var(--sl-font-mono);
      font-size: clamp(1rem, 5cqi, 1.4rem); /* Scales with container width (cqi) */
      letter-spacing: 2px;
      filter: drop-shadow(0px 2px 2px rgba(0,0,0,0.4));
      z-index: 1;
    `
  };

/** * HELPER: Stylish Data Row 
 */
const DataRow = (label, value, isMono = false) => html`
  <dt style="color: var(--sl-color-neutral-500); font-size: 0.65rem; text-transform: uppercase; font-weight: bold; margin-top: 8px;">${label}</dt>
  <dd style="${styles.truncate}; margin: 0; font-size: 0.9rem; ${isMono ? 'font-family: var(--sl-font-mono); font-size: 0.8rem;' : ''}" title="${value}">
    ${value}
  </dd>
`;

const VaultIdentity = (user) => html`
  <sl-card style="border: none; box-shadow: var(--sl-shadow-medium);">
    <div slot="header" style="display: flex; align-items: center; gap: 8px; font-weight: bold; font-size: 0.8rem;">
      <sl-icon name="shield-lock" style="color: var(--sl-color-success-500); font-size: 1.2rem;"></sl-icon> 
      IDENTITY VAULT
    </div>
    <dl style="margin: 0;">
      ${DataRow('Username', user.username)}
      ${DataRow('Internal Email', user.email)}
      ${DataRow('Network Node (IP)', user.ip, true)}
      ${DataRow('Organization', user.company.name)}
    </dl>
  </sl-card>
`;

const FinancialSlots = (user) => {
  // 1. "Aggressive" High-Contrast Gradients for Dark Mode
  const cardGradients = {
    'Mastercard': {
      bg: 'linear-gradient(135deg, #ff5f6d 0%, #ffc371 100%)', // Vibrant Sunset
      glow: 'rgba(255, 95, 109, 0.3)'
    },
    'Visa': {
      bg: 'linear-gradient(135deg, #00d2ff 0%, #3a7bd5 100%)', // Electric Blue
      glow: 'rgba(0, 210, 255, 0.3)'
    },
    'American Express': {
      bg: 'linear-gradient(135deg, #5f2c82 0%, #49a09d 100%)', // Deep Purple to Teal
      glow: 'rgba(73, 160, 157, 0.3)'
    },
    'default': {
      bg: 'linear-gradient(135deg, #434343 0%, #000000 100%)', // Rich Black
      glow: 'rgba(255, 255, 255, 0.1)'
    }
  };

  const theme = cardGradients[user.bank.cardType] || cardGradients['default'];

  return html`
    <sl-card style="border: none; box-shadow: var(--sl-shadow-medium);">
      <div slot="header" style="display: flex; align-items: center; gap: 8px; font-weight: bold; font-size: 0.8rem;">
        <sl-icon name="credit-card" style="color: var(--sl-color-primary-500);"></sl-icon> 
        FINANCIAL ASSETS
      </div>

      <div style="width: 100%; container-type: inline-size;">
          <div style="${styles.creditCard} 
                      background: ${theme.bg}; 
                      box-shadow: 0 10px 20px ${theme.glow}; 
                      border: 1px solid rgba(255,255,255,0.1);">
            
            <div style="position: absolute; top: 0; left: 0; right: 0; height: 50%; background: linear-gradient(to bottom, rgba(255,255,255,0.15) 0%, rgba(255,255,255,0) 100%); pointer-events: none;"></div>

            <div style="display: flex; justify-content: space-between; align-items: flex-start; z-index: 1;">
              <div style="width: 40px; height: 30px; background: rgba(255,255,255,0.2); border: 1px solid rgba(255,255,255,0.3); border-radius: 4px; position: relative; overflow: hidden;">
                 <div style="position: absolute; top: 50%; left: 0; right: 0; height: 1px; background: rgba(255,255,255,0.3);"></div>
                 <div style="position: absolute; left: 50%; top: 0; bottom: 0; width: 1px; background: rgba(255,255,255,0.3);"></div>
              </div>
              <sl-icon name="wifi" style="font-size: 1.2rem; color: white;"></sl-icon>
            </div>

            <div style="${styles.cardTextMain} color: white; text-shadow: 0 2px 4px rgba(0,0,0,0.3);">
              ${user.bank.cardNumber.replace(/\d(?=\d{4})/g, "• ")}
            </div>

            <div style="display: flex; justify-content: space-between; align-items: flex-end; z-index: 1; color: white;">
              <div style="max-width: 60%;">
                <div style="font-size: 0.5rem; text-transform: uppercase; opacity: 0.8;">Card Holder</div>
                <div style="${styles.truncate} font-size: 0.8rem; font-weight: bold; letter-spacing: 0.5px;">
                  ${user.firstName} ${user.lastName}
                </div>
              </div>
              <div style="text-align: right;">
                <div style="font-size: 0.5rem; text-transform: uppercase; opacity: 0.8;">Expires</div>
                <div style="font-size: 0.8rem; font-family: var(--sl-font-mono);">${user.bank.cardExpire}</div>
              </div>
            </div>
          </div>
      </div>

      <dl style="margin: 0;">
        ${DataRow('IBAN', user.bank.iban, styles.truncate, true)}
        ${DataRow('Currency', user.bank.currency, styles.truncate)}
      </dl>
    </sl-card>
  `;
};

const CryptoAssets = (user) => html`
  <sl-card style="border: none; box-shadow: var(--sl-shadow-medium);">
    <div slot="header" style="display: flex; align-items: center; gap: 8px; font-weight: bold; font-size: 0.8rem;">
      <sl-icon name="currency-bitcoin" style="color: var(--sl-color-warning-500); font-size: 1.2rem;"></sl-icon> 
      LEDGER ASSETS
    </div>
    <dl style="margin: 0;">
      ${DataRow('Currency', user.crypto.coin, styles.truncate)}
      ${DataRow('Blockchain', user.crypto.network, styles.truncate)}
      <dt style="color: var(--sl-color-neutral-500); font-size: 0.65rem; text-transform: uppercase; font-weight: bold; margin-top: 8px;">Public Wallet Address</dt>
      <dd style="margin: 0; padding: 8px; background: var(--sl-color-neutral-100); border-radius: 4px; font-family: var(--sl-font-mono); font-size: 0.7rem; word-break: break-all; margin-top: 4px;">
        ${user.crypto.wallet}
      </dd>
    </dl>
  </sl-card>
`;