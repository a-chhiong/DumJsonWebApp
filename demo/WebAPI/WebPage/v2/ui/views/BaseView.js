import { render, html } from 'html';
import { themeHub } from '../../objects/ThemeHub.js';

export class BaseView {
    constructor(container) {
        if (this.constructor === BaseView) {
            throw new Error("BaseView is abstract and cannot be instantiated directly.");
        }
        this.container = container;
        this.state = {};    // Internal UI state
        this._isMounted = false;
    }

    template() {
        throw new Error("View must implement a template().");
    }
    onMounted() {}
    onUnmount() {}

    attach() {
        if (!this.container) return;
        render(this.template(), this.container);
        this._isMounted = true;
        this.onMounted();
    }

    updateView() {
        if (!this.container || !this._isMounted) return;
        render(this.template(), this.container);
    }

    showLoader(message = "Processing...") {
        window.dispatchEvent(new CustomEvent('app:loader', { detail: { show: true, message } }));
    }

    hideLoader() {
        window.dispatchEvent(new CustomEvent('app:loader', { detail: { show: false } }));
    }

    // Fragments don't manage themes, they just provide the UI to trigger them
    toggleTheme() {
        themeHub.toggle();
    }

    dispose() {
        this.onUnmount();
        render(html``, this.container);
        this._isMounted = false;
    }

    get isMounted() { 
        return this._isMounted; 
    }

    static get routeName() {
        return this.name.replace('View', '').toLowerCase();
    }
}
