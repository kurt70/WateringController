This repo hosts the Watering Controller system.

Local dev (backend + frontend + MQTT broker via Docker Compose):
1) Build and run:
   `docker compose -f infra/docker-compose.yml up --build`
2) Open the app: `http://localhost:8080/`
3) MQTT broker: `localhost:1883`

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
- Safety: `Safety__WaterLevelStaleMinutes`
- Scheduling: `Scheduling__CheckIntervalSeconds`
- DevMqtt (dev only): `DevMqtt__AutoStart`
- Env-only: `WATERING_DB_PATH` (overrides database file path)

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

