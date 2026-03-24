# Poe Dameron — History

## Core Context

- **Project:** Et IoT-basert vanningssystem med .NET-backend, Blazor-webapp, MQTT-integrasjon, ESP32-firmware og fysisk pumpe/sensorelektronikk.
- **Role:** Backend Dev
- **Joined:** 2026-03-23T13:51:05.288Z

## Learnings

<!-- Append learnings below -->
- 2026-03-24: Chose cookie-based ASP.NET Core Identity (with optional Microsoft/Google external login) as the authentication approach for the externally exposed container. This fits the current backend-served Blazor WASM + SignalR shape, is easiest to roll out, and avoids JWT complexity unless public APIs are needed. Key: persist Data Protection keys, use env vars for secrets, and secure with TLS proxy.
- 2026-03-24: Reused the existing compose app service for container image builds, added a gitignored `infra/registry.local.env` workflow for registry auth/tagging, and defaulted image publishing to `registry.monge.place/watering-controller`.
- 2026-03-24: Added infra/registry.local.env.example, tools\publish-controller-image.ps1, and updated src/Dockerfile to support container builds and publishing.

- 2026-03-24: Reused the existing compose app service for container image builds, added a gitignored `infra/registry.local.env` workflow for registry auth/tagging, and defaulted image publishing to `registry.monge.place/watering-controller`.
- 2026-03-24: Verified the publish helper builds and pushes the controller image to registry.monge.place using infra\registry.local.env; current published tag resolved to latest.


2026-03-24 10:12:00Z - Poe Dameron executed publish workflow
- Built image using infra\registry.local.env
- Pushed registry.monge.place/watering-controller:latest
- No product code changes

Related inbox decision: .squad/decisions/inbox/poe-container-registry.md
