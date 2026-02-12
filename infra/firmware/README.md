Firmware notes (ESP32-S3)
=========================

This folder holds two PlatformIO projects:
- level-esp32 (water level sensors)
- pump-esp32 (pump controller)

Quick start
-----------
1) Copy config example:
   - level-esp32/include/config.example.h -> config.h
   - pump-esp32/include/config.example.h -> config.h
2) Fill in Wi-Fi and MQTT settings.
3) Build and upload:
   - pio run -t upload
4) OTA (if enabled):
   - pio run -t upload --upload-port <device-hostname-or-ip>

OTA notes
---------
- Set OTA_ENABLED = true
- Set OTA_HOSTNAME to a unique name
- Set OTA_PASSWORD if you want authentication

Example OTA upload
------------------
- Level node:
  pio run -t upload --upload-port waterlevel-esp32.local
- Pump node:
  pio run -t upload --upload-port pump-esp32.local

PowerShell examples (Windows)
-----------------------------
cd infra\\firmware\\level-esp32
pio run -t upload --upload-port waterlevel-esp32.local

cd ..\\pump-esp32
pio run -t upload --upload-port pump-esp32.local
