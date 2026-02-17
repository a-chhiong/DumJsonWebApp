import { BaseView } from '../BaseView.js';
import { LaunchTemplate } from './LaunchTemplate.js';

export class LaunchView extends BaseView {
    constructor(container) {
        super(container);
        // Initial state for the boot sequence
        this.state = {
            status: 'Initializing System...',
            error: null
        };
    }

    // This fulfills the contract required by BaseView
    template() {
        return LaunchTemplate(this.state);
    }
}