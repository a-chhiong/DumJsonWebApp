/**
 * LifecycleHub - The central connection between Lit and RxJS.
 * It manages stream subscriptions automatically based on the 
 * component's presence in the DOM (Connected/Disconnected).
 */
export class LifecycleHub {
    constructor(host, source$) {
        this.host = host;
        this.source$ = source$;
        this.value = null;
        this._subscription = null;

        // Register the controller with the LitElement
        host.addController(this);
    }

    /**
     * Lit lifecycle callback: Element entered DOM
     * We map this to our internal 'hook()'
     */
    hostConnected() {
        this.attach();
    }

    /**
     * Lit lifecycle callback: Element left DOM
     * We map this to our internal 'release()'
     */
    hostDisconnected() {
        this.detach();
    }

    /**
     * hook() - Equivalent to onStart / onResume
     * Establishes the subscription.
     */
    attach() {
        if (this._subscription) return; // Prevent double-hooking

        this._subscription = this.source$.subscribe({
            next: (val) => {
                this.value = val;
                this.host.requestUpdate(); 
            },
            error: (err) => console.error(`[LifecycleHub] Stream Error:`, err)
        });
        console.log(`[LifecycleHub] Attached: ${this.host.tagName}`);
    }

    /**
     * release() - Equivalent to onStop / onDestroy
     * Tears down the subscription.
     */
    detach() {
        if (this._subscription) {
            this._subscription.unsubscribe();
            this._subscription = null;
            console.log(`[LifecycleHub] Detached: ${this.host.tagName}`);
        }
    }
}