# Session Resume

Session name: WateringController

Quick reminders if you are returning to this project after a pause.

## Runtime
- Backend runs on `http://localhost:5291/` when started via `dotnet run --project src/backend/WateringController.Backend.csproj`.
- MQTT broker (Docker) should be up: `docker compose -f infra/docker-compose.yml up -d mqtt`.

## E2E Tests
- Playwright browsers install: `pwsh src/frontend.e2e/bin/Debug/net10.0/playwright.ps1 install`.
- Run tests: `dotnet test src/frontend.e2e/WateringController.Frontend.E2E.csproj`.

## Troubleshooting
- If UI changes don’t appear, rebuild then restart backend.
- If build fails with frontend file locks, run `dotnet build-server shutdown` and retry.
