# Scribe — History

## Core Context

- **Project:** Et IoT-basert vanningssystem med .NET-backend, Blazor-webapp, MQTT-integrasjon, ESP32-firmware og fysisk pumpe/sensorelektronikk.
- **Role:** Session Logger
- **Joined:** 2026-03-23T13:51:05.294Z

## Learnings

<!-- Append learnings below -->

- 2026-03-24T19:45:00+01:00: Containerization session — orchestration notes:
  - Agents: Poe Dameron (added compose profile, Dockerfile and publishing tooling), R2-D2 (validated compose and confirmed build), Scribe (session log).
  - Changes: Enabled profile-backed app service in infra/docker-compose.yml; updated src/Dockerfile; added tools\publish-controller-image.ps1; added infra\registry.local.env.example; added infra\registry.local.env to .gitignore.
  - Validation: Compose config checked; backend Release tests passed; controller image built successfully locally. Recommendation: add a .dockerignore to speed builds and reduce context size.

- 2026-03-24 10:12:00Z: Poe Dameron executed publish workflow
  - Built image using infra\registry.local.env
  - Pushed registry.monge.place/watering-controller:latest
  - No product code changes
  - Orchestration log: .squad/orchestration-log/2026-03-24_Poe-Dameron-publish.log
  - Agents: Poe Dameron (added compose profile, Dockerfile and publishing tooling), R2-D2 (validated compose and confirmed build), Scribe (session log).
  - Changes: Enabled profile-backed app service in infra/docker-compose.yml; updated src/Dockerfile; added tools\publish-controller-image.ps1; added infra\registry.local.env.example; added infra/registry.local.env to .gitignore.
  - Validation: Compose config checked; backend Release tests passed; controller image built successfully locally. Recommendation: add a .dockerignore to speed builds and reduce context size.


- 2026-03-23T20:38:41.5684795+01:00: Committed UI refresh and squad docs; excluded .copilot\mcp-config.json

2026-03-23T21:42:12.4190283+01:00 - Updated repository-local git email to kurt70@users.noreply.github.com and amended last commit (42cf1db75fc2f026c5862b4d6b65ef66b73b9cd5) to use repo identity.
