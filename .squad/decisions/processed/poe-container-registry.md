# Processed Decision: Poe container registry workflow

Date: 2026-03-24

Decision: Use infra/registry.local.env as the local workflow for registry credentials and tagging inputs when building and pushing the controller image. Default image: registry.monge.place/watering-controller. Build+push validated using tools\publish-controller-image.ps1; published image for this run: registry.monge.place/watering-controller:latest.

Source: .squad/decisions/inbox/poe-container-registry.md
Processed-by: Scribe

Notes:
- infra/registry.local.env is gitignored; keep infra/registry.local.env.example committed.
- No product code changes were required for the publish workflow.
