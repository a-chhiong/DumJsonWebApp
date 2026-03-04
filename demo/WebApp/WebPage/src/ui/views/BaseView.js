import { LitElement } from 'lit';
import { BaseViewModel } from '../viewmodels/BaseViewModel.js';

/**
 * @class BaseView
 * @description Base class for all Views. 
 * Provides type-safety and a consistent home for the ViewModel.
 */
export class BaseView extends LitElement {
    /**
     * @type {BaseViewModel | null}
     */
    viewModel = null;

    /**
     * We use the native connectedCallback only for the guard check.
     * The actual "Logic Startup" is handled by the ViewModel itself
     * as a Reactive Controller.
     */
    connectedCallback() {
        super.connectedCallback();
        console.debug(`[${this.constructor.name}] connectedCallback!`);
        if (this.viewModel && !(this.viewModel instanceof BaseViewModel)) {
            console.error(
                `[Architecture Error]: ${this.constructor.name}.viewModel must be an instance of BaseViewModel.`
            );
        }
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        console.debug(`[${this.constructor.name}] disconnectedCallback!`);
    }

    firstUpdated() {
        console.debug(`[${this.constructor.name}] firstUpdated!`);
    }

    /**
     * Standardized bridge to the AppShell's loader.
     * @param {boolean} show 
     * @param {string} message 
     */
    dispatchLoading(show, message = 'Processing...') {
        console.debug(`[${this.constructor.name}] dispatchLoading(${show}, ${message})`);
        window.dispatchEvent(new CustomEvent('app:loader', {
            detail: { show, message },
            bubbles: true,
            composed: true
        }));
    }
}