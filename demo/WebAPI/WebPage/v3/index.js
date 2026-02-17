/**
 * index.js - The App Bootstrapper
 */

import { themeManager } from './src/managers/ThemeManager.js';
import { vaultManager } from './src/managers/VaultManager.js';
import { sessionManager } from './src/managers/SessionManager.js';
import { tokenManager } from './src/managers/TokenManager.js';
import { dpopManager } from './src/managers/DPoPManger.js';
import { apiManager } from './src/managers/ApiManager.js';

import './src/ui/app-shell.js';

/**
 * The Bootstrap function acts like Android's 'onCreate' or 'main' method.
 */
async function bootstrap() {

    console.log("🛠️ Creating Splash..."); 
    // 1. Show the launch screen immediately in the body
    const splash = document.createElement('launch-view');
    document.body.appendChild(splash);
    
    try {
        console.log("🛠️ Creating Managers..."); 

        // A. INITIALIZE SERVICES
        // 1. Initialize Vault & Session
        await vaultManager.init();
        const sessionIdx = await sessionManager.init();

        // 2. Initialize Security Layer (DPoP & Tokens)
        // Using Promise.all here just like your v2 code for efficiency
        await Promise.all([
            tokenManager.init(sessionIdx),
            dpopManager.init(sessionIdx)
        ]);

        // 3. Finalize API Layer
        apiManager.init();
        
        console.log("🛠️ Creating App Shell...");
        // B. CREATE THE UI
        // Now that ApiManager is initialized, it is safe to create the Shell
        const shell = document.createElement('app-shell');
        document.body.appendChild(shell);
        
        // C. SYNC STATE
        // Ensure our ThemeManager is finished with its initial check.
        const currentTheme = themeManager.current;
        console.log(`🌓 Theme Manager active: ${currentTheme}`);
        
        // Give the browser one frame to paint the shell before resolving
        requestAnimationFrame(() => {
            shell.setAttribute('resolved', '');
            setTimeout(() => splash.remove(), 500);
            console.log("✅ App Shell Resolved.");
        });
        
    } catch (err) {
        console.error("🚨 Critical Boot Failure:", err);
        // Fallback: If things break, we should at least show something to the user
        document.body.innerHTML = `<div style="color: white; padding: 20px;">Failed to load system.</div>`;
    }
}

// Kick off the boot process
await bootstrap();