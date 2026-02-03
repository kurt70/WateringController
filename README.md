This repo hosts the Veranda Watering System.

Local dev (backend + frontend + MQTT broker via Docker Compose):
1) Build and run:
   `docker compose -f infra/docker-compose.yml up --build`
2) Open the app: `http://localhost:8080/`
3) MQTT broker: `localhost:1883`

Notes:
- The backend serves the Blazor WASM frontend from the same container.
- The MQTT broker is Eclipse Mosquitto (anonymous, local dev only).
- SQLite files are created in the backend working directory by default.
- MQTT topic prefix is configurable via `Mqtt__TopicPrefix` (default `WateringController`).

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
