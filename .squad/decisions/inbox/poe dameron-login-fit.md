# Login/Auth Approach for WateringController

## Decision

**We will implement cookie-based authentication using ASP.NET Core Identity, with optional Microsoft/Google external login providers.**

- This fits the current architecture: backend serves Blazor WASM, API, and SignalR hub on the same origin.
- Cookie auth is simplest for browser users and works seamlessly with SignalR and Blazor WASM.
- External login (Microsoft/Google) can be added to feed the same session cookie, improving UX and security.
- JWT bearer auth is deferred unless/until we need public APIs or non-browser clients.

## Rollout Plan
1. Add ASP.NET Core Identity to backend, configure for SQLite.
2. Add login/logout endpoints and minimal UI in Blazor WASM.
3. Protect API endpoints and SignalR hub with `[Authorize]`.
4. Persist Data Protection keys to a Docker volume for cookie stability.
5. Add Microsoft/Google login as external providers (optional, after core works).
6. Ensure secrets (connection strings, provider keys) are injected via environment variables or Docker secrets, not in images.

## Tricky Spots
- **SignalR:** Cookie auth works out of the box for browser clients, but requires correct CORS and SameSite settings.
- **Container:** Data Protection keys must be persisted (volume or Redis) or logins break after restart.
- **TLS:** For secure cookies, run behind a TLS proxy and configure forwarded headers.
- **Config:** Never bake secrets into images; use env vars or Docker secrets.

---
Poe Dameron, 2026-03-24
