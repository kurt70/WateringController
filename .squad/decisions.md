# Squad Decisions

## Active Decisions

- 2026-03-23: Normalize Home.razor water level display to shared `waterLevelPercent` local. Decision: normalize variable to prevent Razor scope collisions between refreshed summary and metric UI. Implemented in a scoped UI change. (source: .squad/decisions/inbox/padme-home-compile-fix.md)

- 2026-03-23: Accept successful build as final practical validation in this environment. Decision: Use build+publish success as practical validation when no automated UI tests present. (source: .squad/decisions/inbox/r2-d2-final-ui-check.md)

- 2026-03-23: Repository sharing directive noted: squad-oppsettet skal være delt på tvers av maskiner. (source: .squad/decisions/inbox/copilot-directive-20260323T140509Z.md)

- 2026-03-24: Exclude local MCP config from commits. Decision: Do not track repository-local MCP configuration files (e.g. .copilot\mcp-config.json); keep them out of version control and document in onboarding notes. (source: .squad/decisions/inbox/scribe-commit-push.md)

- 2026-03-24: Confirm container registry workflow for image publish. Decision: Use infra/registry.local.env for local registry credentials and defaults; build+push validated using tools\publish-controller-image.ps1. Published image: registry.monge.place/watering-controller:latest. (source: .squad/decisions/inbox/poe-container-registry.md)


## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
