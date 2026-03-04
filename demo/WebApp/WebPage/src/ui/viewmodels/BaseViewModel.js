import { Subject, takeUntil } from 'rxjs';

/**
 * @class BaseViewModel
 * @description A Lifecycle-aware Lit Controller that manages RxJS state.
 */
export class BaseViewModel {
    constructor(host) {
        if (!host) throw new Error(`[${this.constructor.name}] must be initialized with a 'host' (this)`);

        this.host = host;
        this.destroy$ = new Subject();

        // Register the ViewModel as a Reactive Controller with Lit
        host.addController(this);
    }

    // --- Lit Controller Interface ---
    
    hostConnected() {
        console.debug(`[${this.constructor.name}] hostConnected!`); 
    }

    hostDisconnected() {
        console.debug(`[${this.constructor.name}] hostDisconnected!`); 
        // Automatic cleanup of all RxJS streams tied to this ViewModel
        this.destroy$.next();
        this.destroy$.complete();
    }

    /**
     * Binds a stream to a unique state object.
     * @returns {{ value: *, source$: Observable }}
     */
    bind(source$, initialValue = null) {
        // Create the "State Box" for this specific binding
        const state = { 
            value: initialValue,
            source$: source$.pipe(takeUntil(this.destroy$)) 
        };

        state.source$.subscribe({
            next: (val) => {
                // Check against the state box's own value, not 'this.value'
                if (state.value !== val) {
                    state.value = val;
                    this.host.requestUpdate();
                }
            },
            error: (err) => console.error(`[${this.constructor.name}] Stream Error:`, err)
        });

        return state;
    }
}