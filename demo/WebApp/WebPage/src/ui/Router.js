export class Router {
    constructor(container) {
        // This 'container' is the #outlet div from AppShell's Shadow DOM
        this.container = container;
        this.currentView = null;
        
        // Register your "Fragments"
        this.routes = new Map([
            ['launch', 'launch-view'],
            ['login', 'login-view'],
            ['home', 'home-view']
        ]);

        this._setupListeners();
    }

    _setupListeners() {
        window.addEventListener('hashchange', () => this._syncToHistory());
    }

    _syncToHistory() {
        const slug = window.location.hash.replace('#/', '') || 'launch';
        this.navigate(slug);
    }

    navigate(routeName) {
        const tagName = this.routes.get(routeName);
        if (!tagName) return;

        // 1. Prevent redundant transactions (Don't reload if already there)
        if (this.container.firstChild?.tagName.toLowerCase() === tagName) return;

        // 2. Update History (Intent URL)
        if (window.location.hash !== `#/${routeName}`) {
            window.location.hash = `#/${routeName}`;
        }

        // 3. Perform the Transaction
        // Clear the container (This automatically triggers 'disconnectedCallback' in the old view)
        this.container.innerHTML = '';

        // Create the new Custom Element View
        const newView = document.createElement(tagName);
        
        // 4. Attach to the DOM
        this.container.appendChild(newView);
        this.currentView = newView;
    }

    // High-level Intents (Matches your v2 API)
    toLaunch() { this.navigate('launch'); }
    toHome() { this.navigate('home'); }
    toLogin() { this.navigate('login'); }

    dispose() {
        // In Web Components, removing from DOM = Disposal
        if (this.container) {
            this.container.innerHTML = '';
            this.currentView = null;
        }
    }
}