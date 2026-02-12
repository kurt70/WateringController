# Electronics Diagrams

This document provides diagrams and wiring tables for the pump + level stations.

## Mermaid (System Wiring)
```mermaid
graph LR
  PSU[12V PSU]
  BuckPump[12V->5V Buck (Pump Station)]
  BuckLevel[12V->5V Buck (Level Station)]
  ESPPump[ESP32 (Pump Station)]
  ESPLevel[ESP32 (Level Station)]
  Relay[Relay SRD-05VDC-SL-C]
  Pump[12V Pump]
  DiodePump[Flyback Diode]
  DiodeCoil[Coil Diode]
  BC547[BC547 Driver]
  XKC[XKC-Y25-V Level Sensor]
  DS18[DS18B20 Temp Sensor]
  BME[BME280/BME680]

  PSU -->|12V| BuckPump
  PSU -->|12V| Pump
  PSU -->|12V (cable)| BuckLevel
  BuckPump -->|5V| ESPPump
  BuckPump -->|5V| Relay
  BuckLevel -->|5V| ESPLevel

  ESPPump -->|GPIO via 1k| BC547
  BC547 --> Relay
  DiodeCoil --- Relay

  Relay -->|NO/COM| Pump
  DiodePump --- Pump

  ESPLevel -->|GPIO| XKC
  ESPLevel -->|GPIO + 4.7k pullup| DS18
  ESPLevel -->|I2C| BME
```

## Wiring Table (Pump Station)
| From | To | Notes |
|------|----|-------|
| 12V PSU + | Buck IN+ | 12V feed to buck |
| 12V PSU - | Buck IN- | GND |
| Buck OUT+ (5V) | ESP32 5V/VIN | Logic power |
| Buck OUT- | ESP32 GND | Logic ground |
| Buck OUT+ (5V) | Relay coil + | 5V coil |
| Relay coil - | BC547 Collector | Coil switching |
| BC547 Emitter | GND | Common ground |
| ESP32 GPIO | 1k resistor -> BC547 Base | Driver |
| 1N400x diode | Across relay coil | Katode to 5V, anode to coil- |
| Relay COM | 12V PSU + | Pump power feed |
| Relay NO | Pump + | Switched 12V |
| Pump - | 12V PSU - | GND |
| 1N400x diode | Across pump | Katode to Pump +, anode to Pump - |

## Wiring Table (Level Station)
| From | To | Notes |
|------|----|-------|
| 12V feed (cable) | Buck IN+ | From pump station |
| GND (cable) | Buck IN- | Shared ground |
| Buck OUT+ (5V) | ESP32 5V/VIN | Logic power |
| Buck OUT- | ESP32 GND | Logic ground |
| ESP32 5V | XKC-Y25-V VCC | Level sensor |
| ESP32 GND | XKC-Y25-V GND | |
| ESP32 GPIO | XKC-Y25-V OUT | |
| ESP32 5V | DS18B20 VCC | Temp sensor |
| ESP32 GND | DS18B20 GND | |
| ESP32 GPIO | DS18B20 DATA | Add 4.7k pullup to 5V |
| ESP32 3.3V | BME280/BME680 VCC | I2C sensor |
| ESP32 GND | BME280/BME680 GND | |
| ESP32 GPIO | BME280/BME680 SDA/SCL | I2C lines |

## SVG Diagram
See: `docs/diagrams/pump-level-wiring.svg`
