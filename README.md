This repo hosts the Watering Controller system.

Local dev (backend + frontend + MQTT + Aspire Dashboard via Docker Compose):
1) Build and run:
   `docker compose -f infra/docker-compose.yml up --build`
2) Open the app: `http://localhost:8080/`
3) MQTT broker: `localhost:1883`
4) Aspire Dashboard: `http://localhost:18888/`

Backend-only dev (serves frontend from backend):
1) Run backend: `dotnet run --project src/backend/WateringController.Backend.csproj`
2) Open the app: `http://localhost:5291/`

Testing:
- Backend unit tests:
  - `dotnet test src/backend.tests/WateringController.Backend.Tests.csproj`
- Frontend E2E tests (Playwright):
  1) Build: `dotnet build src/frontend.e2e/WateringController.Frontend.E2E.csproj`
  2) Install browsers (once): `pwsh src/frontend.e2e/bin/Debug/net10.0/playwright.ps1 install`
  3) Run: `dotnet test src/frontend.e2e/WateringController.Frontend.E2E.csproj`
  - Set `E2E_BASE_URL` if not using `http://localhost:5291`

Common issues:
- Frontend build file locks (Defender/MSBuild): run `dotnet build-server shutdown` and retry.

Notes:
- The backend serves the Blazor WASM frontend from the same container.
- The MQTT broker is Eclipse Mosquitto (anonymous, local dev only).
- Electronics diagrams and wiring: `docs/electronics.md`
- SQLite files are created in the backend working directory by default.
- MQTT topic prefix is configurable via `Mqtt__TopicPrefix` (default `home/veranda`).

Configuration reference:
- Mqtt: `Mqtt__Host`, `Mqtt__Port`, `Mqtt__UseTls`, `Mqtt__ClientId`, `Mqtt__Username`, `Mqtt__Password`, `Mqtt__KeepAliveSeconds`, `Mqtt__ReconnectSeconds`, `Mqtt__TopicPrefix`
- Database: `Database__ConnectionString`
- Safety: `Safety__WaterLevelStaleMinutes`, `Safety__AutoStopCheckIntervalSeconds`
- Scheduling: `Scheduling__CheckIntervalSeconds`
- OpenTelemetry: `OpenTelemetry__Enabled`, `OpenTelemetry__ServiceName`, `OpenTelemetry__ServiceVersion`, `OpenTelemetry__OtlpEndpoint`, `OpenTelemetry__ExportLogs`, `OpenTelemetry__ExportMetrics`
- DevMqtt (dev only): `DevMqtt__AutoStart`
- Env-only: `WATERING_DB_PATH` (overrides database file path)

OpenTelemetry (logs + metrics):
- In Development, OpenTelemetry is enabled by default (`appsettings.Development.json`).
- Default OTLP endpoint is `http://localhost:4317` (gRPC).
- Start dashboard and broker together:
  - `docker compose -f infra/docker-compose.yml up -d mqtt aspire-dashboard`
- Example:
  - `OpenTelemetry__Enabled=true`
  - `OpenTelemetry__OtlpEndpoint=http://localhost:4317`
  - `OpenTelemetry__Site=home/veranda`

Frontend OpenTelemetry (browser traces):
- Enabled by default in `src/frontend/wwwroot/appsettings.Development.json`.
- Sends traces to backend same-origin proxy `/api/otel/v1/traces` (configured as `OpenTelemetry:OtlpHttpEndpoint=/api/otel` in frontend settings).
- Backend forwards frontend traces to Aspire OTLP/HTTP (`OpenTelemetry:FrontendTraceProxyTarget`, default `http://localhost:4318/v1/traces`).
- Includes custom UI events for SignalR lifecycle and key actions on Control/Schedules pages.
- Fallback: frontend also posts event payloads to `/api/otel/client-event` so frontend activity is visible in backend logs if browser OTEL export fails.
- Config keys:
  - `OpenTelemetry:Enabled`
  - `OpenTelemetry:ServiceName`
  - `OpenTelemetry:OtlpHttpEndpoint`

Azure Application Insights (via OpenTelemetry Collector):
- Template collector config: `infra/otel-collector-config.azure.yaml`
- Requires env var: `APPLICATIONINSIGHTS_CONNECTION_STRING`
- Typical switch from Aspire Dashboard to Azure collector:
  1) Start collector using the template (see commented `otel-collector-azure` service in `infra/docker-compose.yml`).
  2) Point backend OTLP to collector:
     - `OpenTelemetry__OtlpEndpoint=http://localhost:4317`
  3) Keep frontend proxy target:
     - `OpenTelemetry__FrontendTraceProxyTarget=http://localhost:4318/v1/traces`

Azure table mapping expectations:
- `Requests`: ASP.NET Core incoming spans.
- `Dependencies`: outgoing HTTP spans and other dependency spans.
- `Traces`: structured backend logs and relayed frontend logs.
- `Events`: relayed frontend events are emitted as explicit frontend trace spans (`event.name=...`), and are queryable with consistent dimensions.

Canonical custom property keys (kept consistent across logs/traces/relayed events):
- `site`
- `component`
- `request.id`
- `http.route`
- `schedule.id`
- `device.id`
- `safety.reason`
- `event.name`
- `event.source`
- `event.sent_at`

These keys are centrally defined in `src/backend/Telemetry/TelemetryDimensions.cs`.

Database path override:
- Set `WATERING_DB_PATH` to point at a mounted volume path when running in a container.
- Example: `WATERING_DB_PATH=/data/watering.db`

Firmware update (PlatformIO):
- Build + upload via USB:
  - `.\tools\update-firmware.ps1 -Target pump`
  - `.\tools\update-firmware.ps1 -Target level -Port COM5`
- OTA (future):
  - `.\tools\update-firmware.ps1 -Target pump -Mode ota -Port 192.168.1.50`

Dev test utilities:
- /test (development only) publishes MQTT test messages.
- Test endpoints: /api/test/mqtt/waterlevel, /api/test/mqtt/pumpstate, /api/test/mqtt/systemstate, /api/test/mqtt/alarm, /api/test/mqtt/pumpcmd

