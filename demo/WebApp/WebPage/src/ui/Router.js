export class Router {
    constructor(container) {
        // This 'container' is the #outlet div from AppShell's Shadow DOM
        this.container = container;
        this.currentView = null;
        
        // Register your "Fragments"
        this.routes = new Map([
            ['login', 'login-view'],
            ['home', 'home-view']
        ]);

        this._setupListeners();
    }

    /**
     * Helper to check if a specific route is currently active.
     */
    isAt(routeName) {
        const tagName = this.routes.get(routeName);
        if (!tagName || !this.container.firstChild) {
            console.debug('[Router] tagName:', tagName);
            console.debug('[Router] firstChild:', this.container.firstChild);
            return null;
        }
        return this.container.firstChild.tagName.toLowerCase() === tagName;
    }

    /**
     * Boolean helper for the AppShell's logic.
     */
    get isAtSecuredView() {
        return this.isAt('home');
    }

    _setupListeners() {
        window.addEventListener('hashchange', () => {
            console.debug(`[Router] hashchange!`);
            this._syncToHistory();
        });
    }

    _syncToHistory() {
        const slug = window.location.hash.replace(/^#\/?/, '') || 'login';
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

    /**
     * Helper to navigate around safe area
     */
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