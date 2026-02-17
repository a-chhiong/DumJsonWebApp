import { LitElement, html, css } from 'lit';

export class LaunchView extends LitElement {
    // 1. Component-specific Styles (The old CSS moves here)
    static styles = css`
        :host {
            display: block;
            height: 100%;
            width: 100%;
        }
        .launch-screen {
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100vh;
            text-align: center;
        }
        .title {
            font-size: 2rem;
            margin-bottom: 1.5rem;
            color: var(--sl-color-neutral-900);
        }
        .status-message {
            margin-top: 1rem;
            color: var(--sl-color-neutral-600);
            font-family: var(--sl-font-sans);
        }
    `;

    // 2. Reactive Properties (Replaces this.state)
    static properties = {
        status: { type: String }
    };

    constructor() {
        super();
        this.status = "Initializing Secure Sandbox...";
    }

    // 3. The Template (Combined from your LaunchTemplate)
    render() {
        return html`
          <div class="launch-screen">
            <div class="launch-content">
              <h1 class="title">👽 DummyJSON Demo</h1>
              
              <sl-spinner style="font-size: 3rem;"></sl-spinner>
              
              <p class="status-message">${this.status}</p>
            </div>
          </div>
        `;
    }
}

// Register the custom element tag
customElements.define('launch-view', LaunchView);