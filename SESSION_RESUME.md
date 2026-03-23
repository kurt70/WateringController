# Session Resume

Session name: WateringController

Quick shared reminders if you are returning to this project after a pause.
For canonical setup and runtime commands, use `README.md`.

## Runtime
- Backend runs on `http://localhost:5291/` when started via `dotnet run --project src/backend/WateringController.Backend.csproj`.
- Local infrastructure should be up: `docker compose -f infra/docker-compose.yml up -d mqtt aspire-dashboard`.

## E2E Tests
- Playwright browsers install: `pwsh src/frontend.e2e/bin/Debug/net10.0/playwright.ps1 install`.
- Run tests: `dotnet test src/frontend.e2e/WateringController.Frontend.E2E.csproj`.
- Set `E2E_BASE_URL` if the backend is not running on `http://localhost:5291`.

## Troubleshooting
- If UI changes don’t appear, rebuild then restart backend.
- If build fails with frontend file locks, run `dotnet build-server shutdown` and retry.
