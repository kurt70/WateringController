# Workflow

> Shared progress checklist for validation and remaining setup work.
> `README.md` is the canonical runtime/setup guide; this file tracks what has and has not been verified.

## Legend
- [ ] Not done
- [x] Done
- [!] Blocked/needs input

## 0. Context Collected
- [x] Read docs/architecture.md.
- [x] Read docs/mqtt.md.
- [x] Read README.md.
- [x] Scanned backend core: Program.cs, AppServiceRegistration.cs, ScheduleService.cs, PumpCommandService.cs, AlarmService.cs, MQTT handlers.
- [x] Scanned frontend core: Program.cs, Home.razor, Control.razor, Schedules.razor, Test.razor, WateringHubClient.cs.
- [x] Located firmware under infra/firmware (pump-esp32 + level-esp32).

## 1. Environment & Secrets
- [x] Confirm MQTT broker host/port and auth (anonymous in dev; see README).
- [ ] Confirm DB path (WATERING_DB_PATH) if running in container.
- [x] Confirm dev mode vs prod mode for test endpoints (dev-only `/api/test/mqtt/*`).
- [ ] Confirm firmware config headers (infra/firmware/*/include/config.h) have correct WiFi/MQTT settings.
- [x] Confirm time zone expectations (backend schedules use UTC; UI converts local ↔ UTC).

## 1.1 Electronics Planning
- [ ] Confirm pump specs (voltage, current draw, duty cycle, max runtime).
- [ ] Confirm power supply sizing (12V rail for pump + 5V/3.3V for logic).
- [ ] Choose switching method (MOSFET vs relay) and flyback protection.
- [ ] Define sensor wiring (XKC-Y25-V, DS18B20, BME280/680) and pullups.
- [ ] Define grounding scheme (star ground, pump ground isolation).
- [ ] Confirm enclosure, cable lengths, and environmental protection.

## 2. Build & Run
- [x] Start local infrastructure with `docker compose -f infra/docker-compose.yml up -d mqtt aspire-dashboard`.
- [x] Verify backend health endpoint /health is OK.
- [x] Current checked-in dev flow uses backend-served UI at `http://localhost:5291/`.
- [!] Optional app container path (`http://localhost:8080/`) remains disabled until the commented `app` service in `infra/docker-compose.yml` is enabled.
- [x] Confirm backend can publish/subscribe to MQTT topics (test publish + latest cache).

## 3. MQTT Connectivity
- [x] Confirm MQTT broker reachable from backend.
- [x] Confirm topic prefix documentation matches code. Default is `home/veranda`, producing `home/veranda/WateringController/...`.

## 4. Safety Gate Checks
- [x] Publish water level below threshold -> pump blocked (verified 409 "Water level is empty.").
- [x] Publish stale/unknown water level -> pump blocked (verified 409 "Water level is stale." and "Water level is unknown.").
- [x] Publish OK water level -> pump allowed (verified 200 success + requestId).

## 5. Scheduling Validation
- [x] Create schedule (API or UI) is implemented (CRUD endpoints + UI).
- [ ] Observe schedule tick logs.
- [x] Verify run history recorded (verified scheduled run persisted for schedule id 22).
- [x] Verify schedule runs only once per day is implemented (lastRunDateUtc check).

## 6. Manual Controls
- [x] Manual start/stop endpoints implemented.
- [x] Control UI implemented (`/control`).
- [x] Manual start blocked if unsafe (verified 409 for empty/stale/unknown).
- [x] Manual stop issues pump stop command (verified MQTT publish on `.../pump/cmd`).
- [x] Manual start when safe results in pump cmd + state update (verified MQTT start command + `/api/pump/latest` update).

## 7. UI & SignalR
- [x] UI wiring implemented for water level/pump state/alarms (SignalR + HTTP fallbacks).
- [x] Connection status reflects SignalR disconnect/reconnect in UI.
- [x] Verify live updates end-to-end in browser (manually confirmed with repeated live waterlevel+alarm updates).

## 8. Tests
- [x] Run backend unit tests (scheduling + safety) - 69 passed.
- [ ] Run firmware unit tests (PlatformIO native env) if applicable.
- [x] Run frontend E2E tests (Playwright) - 6 passed.

## 9. Firmware Build & Flash (USB)
- [ ] Build + upload pump firmware (PlatformIO).
- [ ] Build + upload level firmware (PlatformIO).
- [ ] Verify serial logs for successful boot + MQTT connect.

## 9.1 Electronics Build & Validation
- [ ] Bench power test: verify rails under load (pump idle + pump running).
- [ ] Switch test: verify MOSFET/relay switching with flyback diode.
- [ ] Sensor test: verify stable readings on all sensors.
- [ ] Noise/EMI check: verify ESP32 stability during pump switching.
- [ ] Safety test: confirm pump does not run when level is empty/unknown.

## 10. Docs & Cleanup
- [x] Fix docs/mqtt.md code fences.
- [x] Update docs/mqtt.md and WORKFLOW topic prefix to match code (topics are `{prefix}/WateringController/...`).
- [x] Update README if dev steps or environment info changed.
- [x] Document test workflow updates (E2E + build-lock workaround) in `README.md` and keep `SESSION_RESUME.md` as a short companion.

---
## Status Log
- [x] Initial workflow drafted.
- [x] Governance and shared squad-state documentation aligned for multi-machine use.
