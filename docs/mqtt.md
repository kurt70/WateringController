# MQTT Contract – Veranda Watering System

This document defines all MQTT topics and payload contracts used by the system.

Components:
- ESP32 Pump Controller
- ESP32 Water Level Controller
- Backend (.NET 10 LTS) – authoritative policy engine
- Web Frontend (via backend)
- Home Assistant (read-only + automation)

The backend is the sole authority for scheduling and safety decisions.

---

## Broker
- Eclipse Mosquitto
- Transport: TCP
- Payload format: JSON (UTF-8)
- QoS: 1 unless otherwise specified

---

## Topic Naming Convention

veranda/<component>/<type>

- `<component>`: `pump` | `waterlevel` | `system`
- `<type>`: `cmd` | `state` | `alarm`

---


### Components
- `pump`
- `waterlevel`
- `system`

### Message Types
- `cmd`    → commands
- `state`  → current state
- `alarm`  → alarms/events

---

## 4. Command Topics

### 4.1 `veranda/pump/cmd`

#### Purpose
Command the pump controller to run the pump for a fixed duration.

#### Publisher
- Backend ONLY

#### Subscriber
- Pump ESP32 ONLY

#### Retained
- No

#### Payload Schema
```json
{
  "runSeconds": 30,
  "requestId": "uuid",
  "reason": "schedule",
  "issuedAt": "2026-01-15T07:00:00Z"
}

#### Field Definitions
| Field	| Type | Required | Description |
|-------|------|----------|-------------|
| runSeconds | int | yes | Pump | runtime in seconds |
| requestId	| string (UUID)| yes | Correlation id |
| reason | string | yes | schedule | manual | test|
| issuedAt | string (UTC) | yes | When backend issued command|

## 5. Device State Topics

### 5.1 `veranda/pump/state`

#### Purpose
Report current and last-known pump state.

#### Publisher
- Pump ESP32

#### Subscriber
- Backend
- Home Assistant

#### Retained
- Yes

#### Payload Schema
```json
{
  "running": true,
  "since": "2026-01-15T07:00:01Z",
  "lastRunSeconds": 30,
  "lastRequestId": "uuid",
  "reportedAt": "2026-01-15T07:00:01Z"
}

#### Field Definitions
| Field	| Type | Required | Description |
|-------|------|----------|-------------|
| running | bool | yes | Pump currently running |
| since	| string | conditional (UTC) | Required if running = true |
| lastRunSeconds | int | yes | Duration of last run |
| lastRequestId | string  | optional | Correlates to last cmd |
| reportedAt | string | yes | Time (UTC) state was reported |

### 5.2 `veranda/waterlevel/state`

#### Purpose
Report water level derived from induction sensors.

#### Publisher
- Water Level ESP32

#### Subscriber
- Backend
- Home Assistant

#### Retained
- Yes

#### Payload Schema
```json
{
  "levelPercent": 63,
  "sensors": [true, true, false, false],
  "measuredAt": "2026-01-15T06:55:00Z",
  "reportedAt": "2026-01-15T06:55:01Z"
}


#### Field Definitions
| Field	| Type | Required | Description |
|-------|------|----------|-------------|
| levelPercent | int | yes | 0–100 derived level |
| ssensors | bool[] | yes | Bottom → top sensors |
| measuredAt | string | yes | When (UTC) level was measured |
| reportedAt | string  | yes | When published |

## 6. Backend State Topics

### 6.1 `veranda/system/state`

#### Purpose
Expose aggregated system state to UI and Home Assistant.

#### Publisher
- Backend

#### Subscriber
- Frontend
- Home Assistant

#### Retained
- Yes

#### Payload Schema
```json
{
  "pumpRunning": false,
  "waterLevelPercent": 63,
  "safeToRun": true,
  "nextScheduledRun": "2026-01-16T07:00:00Z",
  "lastRun": "2026-01-15T07:00:00Z",
  "evaluatedAt": "2026-01-15T07:01:00Z"
}

## 8. Alarm Topics

### 8.1 `veranda/system/alarm`

#### Purpose
Emit safety or system alarms.

#### Publisher
- Backend

#### Subscriber
- Frontend
- Home Assistant

#### Retained
- No

#### Payload Schema
```json
{
  "type": "LOW_WATER",
  "severity": "warning",
  "message": "Pump run blocked due to low water level",
  "raisedAt": "2026-01-15T07:00:00Z"
}



#### Alarm Types
| Type	| Description |
|-------|------|----------|-------------|
| LOW_WATER | Water below threshold |
| LEVEL_UNKNOWN | No recent level data |
| MQTT_DISCONNECTED | Broker connection lost |
| SCHEDULER_ERROR | Backend scheduling failure |



