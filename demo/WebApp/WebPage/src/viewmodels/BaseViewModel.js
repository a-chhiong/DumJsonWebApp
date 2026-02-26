/**
 * @class BaseViewModel
 * @description Provides lifecycle hooks and memory management for View state.
 */
export class BaseViewModel {
    constructor() {
        // Initialize common logic here
    }

    /** Called when the View enters the DOM */
    onConnect() {
        console.log(`[${this.constructor.name}] onConnect`);
    }

    /** Called when the View leaves the DOM */
    onDisconnect() {
        console.log(`[${this.constructor.name}] onDisconnect`);
    }
}