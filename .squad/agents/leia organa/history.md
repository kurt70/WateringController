# Leia Organa — History

## Core Context

- **Project:** Et IoT-basert vanningssystem med .NET-backend, Blazor-webapp, MQTT-integrasjon, ESP32-firmware og fysisk pumpe/sensorelektronikk.
- **Role:** Lead
- **Joined:** 2026-03-23T13:51:05.287Z

## Learnings

<!-- Append learnings below -->
- 2026-03-24: For this repo's externally exposed single-container deployment, prefer front-door OIDC authentication at the reverse proxy over building local login into the app. Reason: the Blazor WASM UI, minimal APIs, and SignalR hub all live on the same origin and can be gated together cleanly. Key paths reviewed: `src\backend\Program.cs`, `src\backend\AppServiceRegistration.cs`, `src\frontend\Services\WateringHubClient.cs`, `infra\docker-compose.yml`, `src\Dockerfile`, `README.md`.
