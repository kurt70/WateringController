## Decision

Use edge authentication as the primary login architecture for external exposure of the Watering Controller app.

## Why

- The app is a single origin: Blazor WASM is served by the backend container, the frontend calls same-origin `/api/*`, and SignalR connects to `/hubs/watering`.
- This means one reverse-proxy auth gate can consistently protect UI, API, and WebSocket traffic together.
- The system controls physical behavior (pump start/stop and schedules), so internet exposure should minimize in-app auth complexity and prefer a hardened identity layer with MFA support.
- Current backend shape is effectively open today (no auth/authorization middleware and permissive CORS), so the app must not be published directly as-is.

## Direction

- Put login in front of the app using an OIDC-capable access proxy such as Cloudflare Access, oauth2-proxy, or Traefik ForwardAuth backed by a real identity provider.
- Publish only the proxy to the internet; keep the app container reachable only on a private Docker/network segment.
- Require auth for `/`, `/api/*`, and `/hubs/*`.
- Keep `/health` non-public or network-restricted; do not expose dev/test endpoints, MQTT, or observability dashboards publicly.

## Fallback

If no external IdP/proxy is available, use ASP.NET Core cookie auth with a single local admin account inside the app as a temporary fallback, not Basic Auth or long-lived browser tokens.
