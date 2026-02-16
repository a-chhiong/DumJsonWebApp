import { html } from 'html';

export const LaunchTemplate = (status = "Initializing Secure Sandbox...") => html`
  <div class="view-container launch-screen">
    <div class="launch-content">
      <h1 class="title">👽 DummyJSON Demo</h1>
      
      <!-- Shoelace spinner instead of raw div -->
      <sl-spinner style="font-size: 2rem;"></sl-spinner>
      
      <!-- Status message -->
      <p class="status-message">${status}</p>
    </div>
  </div>
`;
