# Poe decision: container registry workflow

- Reused the existing compose-based app container setup instead of inventing a second path.
- Enabled the app service behind the `app` profile so normal local infra stays unchanged.
- Standardized the default image target as `registry.monge.place/watering-controller:latest`, with env overrides for image name and tag.
- Added a local-only `infra/registry.local.env` workflow for registry credentials and tagging inputs, while keeping only the example file committed.
