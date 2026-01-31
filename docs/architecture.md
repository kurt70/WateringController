# System Architecture – Veranda Watering System

This document describes the architecture, responsibilities, and data flow
of the Veranda Watering System.

It complements `docs/mqtt.md` and explains *why* the system is structured
the way it is.

---

## 1. High-Level Overview

The system controls a veranda watering setup consisting of:
- A water pump
- A water barrel with level sensors
- Two ESP32 microcontrollers
- A backend service acting as policy engine
- A web-based user interface
- Home Assistant integration

The core design principle is **centralized decision-making with distributed failsafes**.

---

## 2. Components

### 2.1 Pump Controller (ESP32)

**Responsibilities**
- Control the physical water pump (on/off)
- Execute pump run commands received via MQTT
- Enforce local hardware safety:
  - max runtime
  - watchdog reset
- Report pump state via MQTT

**Non-responsibilities**
- Scheduling
- Safety policy decisions
- Autonomous pump start

The pump controller is a *dumb executor*.

---

### 2.2 Water Level Controller (ESP32)

**Responsibilities**
- Read several induction sensors mounted bottom → top in the water barrel
- Derive a water level percentage
- Periodically publish water level state via MQTT

**Non-responsibilities**
- Safety decisions
- Pump control
- Aggregation or interpretation beyond raw level

---

### 2.3 Backend Service (.NET 10 LTS)

The backend is the **authoritative brain** of the system.

**Responsibilities**
- Maintain watering schedules
- Evaluate safety rules before issuing commands
- Cache latest device state from MQTT
- Publish pump commands
- Aggregate system state
- Expose API for frontend
- Push realtime updates via SignalR
- Emit alarms

**Key Design Choice**
> If the backend is uncertain, the system defaults to *safe* (no watering).

---

### 2.4 MQTT Broker (Mosquitto)

**Responsibilities**
- Transport messages between system components
- Decouple ESP32 firmware from backend implementation
- Serve as integration point for Home Assistant

MQTT is used **only** for device-level communication.

---

### 2.5 Web Frontend

**Responsibilities**
- Allow user to:
  - configure watering schedules
  - manually trigger watering (if allowed)
  - view system status and alarms
- Display live updates via SignalR

**Non-responsibilities**
- Talking directly to ESP32
- Implementing safety rules

All frontend actions go through the backend API.

---

### 2.6 Home Assistant

**Responsibilities**
- Visualization
- Optional automation (notifications, dashboards)
- Read-only interaction with system state

Home Assistant does **not** control the pump directly.

---

## 3. Data Flow

### 3.1 Normal Scheduled Watering

1. Scheduler in backend determines it is time to water
2. Backend checks:
   - latest water level
   - freshness of level data
   - pump availability
3. If safe:
   - backend publishes `veranda/pump/cmd`
4. Pump ESP32:
   - starts pump
   - publishes `veranda/pump/state`
5. Backend:
   - updates system state
   - pushes updates to frontend and HA

---

### 3.2 Water Level Update

1. Water level ESP32 reads sensors
2. Publishes `veranda/waterlevel/state`
3. Backend:
   - caches level
   - re-evaluates safety
   - updates `veranda/system/state`
   - pushes updates via SignalR

---

### 3.3 Safety Block Scenario (Empty Barrel)

1. Scheduler attempts to run pump
2. Backend detects:
   - level below threshold OR
   - stale/missing level data
3. Backend:
   - does NOT publish pump command
   - emits `veranda/system/alarm`
4. Frontend + Home Assistant reflect blocked state

---

## 4. Safety Model

### 4.1 Authoritative Safety

- Backend decides **if** the pump may run
- ESP32 decides **how long** it is allowed to run

This layered approach prevents:
- runaway pumps
- watering when empty
- damage due to communication failures

---

### 4.2 Fail-Safe Defaults

| Scenario | Behavior |
|-------|----------|
| Backend offline | Pump does not run |
| MQTT offline | Pump does not run |
| Water level unknown | Pump does not run |
| ESP32 watchdog triggers | Pump stops |

---

## 5. Scheduling Model

- Scheduling logic runs in backend
- Implemented using a BackgroundService
- Schedule data stored persistently (SQLite)
- Time calculations are deterministic and testable
- Manual overrides are evaluated through same safety logic

---

## 6. Observability

### Logging
- Backend logs:
  - MQTT messages (summary)
  - pump decisions (allowed/denied)
  - alarms
- ESP32 logs locally (serial) for diagnostics

### State
- Latest known state is always available via:
  - MQTT retained messages
  - backend cache
  - frontend API

---

## 7. Extensibility

The architecture supports future additions:
- moisture sensors
- weather-based scheduling
- multiple watering zones
- mobile UI
- cloud broker

These can be added without changing core safety principles.

---

## 8. Architectural Principles Summary

- Centralized policy, distributed execution
- Safety over convenience
- Explicit contracts
- Clear ownership per component
- Default to safe behavior

Any architectural change MUST preserve these principles.
