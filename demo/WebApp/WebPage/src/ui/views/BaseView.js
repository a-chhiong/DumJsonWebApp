import { LitElement } from 'lit';
import { BaseViewModel } from '../../viewmodels/BaseViewModel.js';

/**
 * @class BaseView
 * @description Base class for all Views that require a lifecycle-managed ViewModel.
 * @extends LitElement
 */
export class BaseView extends LitElement {
    /**
     * The ViewModel associated with this View.
     * Must extend BaseViewModel.
     * @type {BaseViewModel | null}
     */
    viewModel = null;

    connectedCallback() {
        super.connectedCallback();
        
        if (this.viewModel) {
            // 🚨 The Runtime Guard: Ensure it extends our specific Base
            if (!(this.viewModel instanceof BaseViewModel)) {
                console.error(
                    `[Architecture Error]: ${this.constructor.name}.viewModel must be an instance of BaseViewModel. ` +
                    `Got: ${this.viewModel?.constructor?.name}`
                );
                return;
            }
            
            this.viewModel.onConnect();
        }
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        
        // Clean up the ViewModel lifecycle
        if (this.viewModel) {
            this.viewModel.onDisconnect();
            // Optional: Breaking the reference helps Garbage Collection in large apps
            this.viewModel = null;
        }
    }
}