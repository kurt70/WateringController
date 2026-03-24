# R2-D2 — History

## Core Context

- **Project:** Et IoT-basert vanningssystem med .NET-backend, Blazor-webapp, MQTT-integrasjon, ESP32-firmware og fysisk pumpe/sensorelektronikk.
- **Role:** QA & Integration
- **Joined:** 2026-03-23T13:51:05.292Z

## Learnings

<!-- Append learnings below -->
- 2026-03-23: Ran frontend validation after the user installed .NET 10 SDK. dotnet --list-sdks shows 9.0.306 and 10.0.201; dotnet --version reports 10.0.201.
- Build attempt of src\frontend\WateringController.Frontend.csproj succeeded in restore but failed to compile with 4 errors in src\frontend\Pages\Home.razor: duplicate local variable 'level' and an unassigned use of 'level'.
- Blocker: compile-time errors in Home.razor; request frontend author to fix variable scoping or rename locals. Validation cannot proceed until build succeeds.

2026-03-23T19:37:23 - Confirmed build & publish succeeded for src\frontend\WateringController.Frontend.csproj.
2026-03-24T19:19:02.7970455+01:00 - r2-d2: Performed container validation: docker available; compose config valid; dotnet present. Did not run full docker build to avoid heavy network/CPU. See infra/docker-compose.yml for app service (commented).
