import { LaunchView } from './views/Launch/LaunchView.js';
import { LoginView } from './views/Login/LoginView.js';
import { HomeView } from './views/Home/HomeView.js';

export class Router {
    constructor(container) {
        this.container = container;
        this.currentView = null;
        this.routes = new Map();
        const vc = [LaunchView, LoginView, HomeView];
        vc.forEach(v => {
            this.routes.set(v.routeName, v);
        });
        this._setupListeners();
    }

    _setupListeners() {
        // Like a FragmentManager listening for back-stack changes
        window.addEventListener('hashchange', () => this._syncToHistory());
    }

    navigate(routeName) {
        // Prevent redundant "Fragment Transactions"
        if (this.currentView?.constructor.routeName === routeName) return;

        this.dispose();

        // Update the "Intent" URL
        if (window.location.hash !== `#/${routeName}`) {
            window.location.hash = `#/${routeName}`;
        }

        // Instantiate and Attach the new "Fragment"
        const ViewClass = this.routes.get(routeName);
        this.currentView = new ViewClass(this.container);
        this.currentView.attach(); 
    }

    toLaunch() {
        this.navigate(LaunchView.routeName);
    }

    toHome() {
        this.navigate(HomeView.routeName);
    }

    toLogin() {
        this.navigate(LoginView.routeName);
    }

    _syncToHistory() {
        // Extract the slug from #/home -> home
        const slug = window.location.hash.replace('#/', '') || LaunchView.routeName;
        const ViewClass = this.routes.get(slug);
        
        if (ViewClass) this.navigate(ViewClass.routeName);
    }

    dispose() {
        if (this.currentView) {
            this.currentView.dispose();
            this.currentView = null;
        }
    }
}