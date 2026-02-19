# DumJsonWebApp

A lightweight, framework-less frontend architecture built with **Lit** and **Native Web Standards**, featuring a custom security layer and an Android-inspired design pattern.

## 🚀 The Vision

This project is an exploration of "Framework-less" development. Instead of reaching for a heavy CLI-based framework, I’ve implemented the core pillars of a modern SPA (Single Page Application) from the ground up:

* **Custom Router:** A lightweight, manifest-based navigation engine.
* **Android-Inspired Lifecycle:** An explicit `Bootstrap` and `Manifest` sequence, separating component registration from application logic.
* **Secure by Design:** Integrated **Token Service** and **DPoP (Demonstrating Proof-of-Possession)** validation on the fly.
* **Decoupled State:** Pure JS "Managers" handle business logic (Vault, Session, Auth), keeping the UI components thin and reactive.

## 🛠️ Technical Stack

* **Frontend:** [Lit](https://lit.dev/) (for ultra-fast, standards-based web components).
* **API:** Powered by [DummyJSON](https://dummyjson.com/), augmented with custom security middleware.
* **Architecture:** Service-Oriented Architecture (SOA) with a central "App Shell" orchestrator.

## 🏗️ Project Structure

The project follows a strict organizational pattern to ensure maintainability:

* `src/managers`: Singleton state providers (The "Source of Truth").
* `src/ui/Manifest.js`: The "Android Manifest" for your Web Components.
* `src/ui/Bootstrap.js`: The application's `onCreate` equivalent.
* `src/ui/views`: Top-level routed screens.

---

## 🏛️ Directory Philosophy (The "Why")

Unlike many web projects where logic is scattered, this project follows a **Separation of Concerns** inspired by mobile architecture:

### 1. The Registry (`Manifest.js`)

Consider this the `AndroidManifest.xml`. By centralizing all `customElements.define` calls here, we eliminate "Side-Effect Imports." The rest of the app doesn't need to know where a view lives; it just needs to know its tag name.

### 2. The Orchestrator (`Bootstrap.js`)

This is the application's `Main` or `onCreate`. It manages the critical startup sequence:

1. **Prepare UI** (Register Manifest)
2. **Show Splash** (Launch View)
3. **Init Services** (Vault/Tokens/Session)
4. **Mount App** (App Shell)

### 3. The Data Layer (`src/managers`)

Managers are framework-agnostic singletons. They handle the "Work" (API calls, Storage, Encryption). By keeping them outside of the UI, the business logic remains testable in a pure JS environment without a DOM.

### 4. The Bridge (`index.js`)

A minimal entry point that connects the **Manifest** to the **Bootstrap**. It acts as the final gatekeeper, ensuring the environment is ready before a single pixel is rendered.

---
