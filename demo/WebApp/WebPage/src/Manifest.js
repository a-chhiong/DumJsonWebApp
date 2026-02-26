// Views
import { AppShell } from './ui/AppShell.js';
import { LaunchView } from './ui/views/LaunchView.js';
import { LoginView } from './ui/views/LoginView.js';
import { HomeView } from './ui/views/HomeView.js';

// Components
import { ProfileHeader } from './ui/components/ProfileHeader.js';
import { VaultIdentity } from './ui/components/VaultIdentity.js';
import { BiometricCard } from './ui/components/BiometricCard.js';
import { FinancialSlots } from './ui/components/FinancialSlots.js';
import { CryptoAssets } from './ui/components/CryptoAssets.js';

const tags = {
    // Views
    'app-shell': AppShell,
    'launch-view': LaunchView,
    'login-view': LoginView,
    'home-view': HomeView,
    // Components
    'profile-header': ProfileHeader,
    'biometric-card': BiometricCard,
    'vault-identity': VaultIdentity,
    'financial-slots': FinancialSlots,
    'crypto-assets': CryptoAssets,
};

export const manifesto = () => {
    console.group("🚀 UI Manifest");
    Object.entries(tags).forEach(([tag, cls]) => {
        if (!customElements.get(tag)) {
            console.debug(`Defining: <${tag}>`);
            customElements.define(tag, cls);
        }
    });
    console.groupEnd();
};