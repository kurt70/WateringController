---
name: "external-access-auth-architecture"
description: "Auth pattern for externally exposed single-origin Blazor WASM + ASP.NET Core apps"
domain: "security"
confidence: "high"
source: "manual"
---

## Context

Use this when a .NET backend serves a Blazor WebAssembly frontend from the same container or origin, and the app exposes minimal APIs and SignalR to the public internet.

## Patterns

### Prefer edge OIDC for internet exposure

- Put login at the reverse proxy or access layer, backed by a real identity provider.
- Publish only the proxy externally; keep the application container private behind it.
- Protect the whole origin, not just the HTML shell: `/`, `/api/*`, and `/hubs/*`.
- Favor providers and proxies that support MFA, session policies, and allowed-user or allowed-group rules.

### Match auth shape to the app transport shape

- Same-origin Blazor WASM + minimal APIs + SignalR works well with front-door auth because browser page loads, XHR/fetch calls, and WebSocket negotiation all traverse the same proxy.
- If the app later needs roles or per-user decisions, the app can validate forwarded identity or OIDC claims, but the first security boundary should still be the edge.

### Keep unsafe/internal surfaces off the internet

- Do not expose raw app ports directly if a proxy is the auth boundary.
- Do not expose MQTT brokers, telemetry dashboards, OTLP collectors, dev/test endpoints, or local database files publicly.
- Health endpoints should be internal or tightly network-restricted.

## Examples

- Watering Controller review: backend serves Blazor WASM from the same container, frontend calls same-origin `/api/*`, and SignalR connects to `/hubs/watering`; recommended architecture is edge OIDC auth with the app container private on the Docker network.

## Anti-Patterns

- **Edge auth with direct app internet exposure** — lets attackers bypass the intended auth boundary.
- **Basic Auth for browser-based operator login** — poor UX, weak session controls, and no good MFA story.
- **Long-lived API tokens for human UI sessions** — too easy to leak and hard to revoke cleanly in browsers.
