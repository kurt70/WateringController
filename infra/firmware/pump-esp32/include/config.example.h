#pragma once

// Wi-Fi
static const char* WIFI_SSID = "CHANGE_ME";
static const char* WIFI_PASSWORD = "CHANGE_ME";
static const char* WIFI_AP_SSID = "WateringController-Setup";
static const char* WIFI_AP_PASSWORD = "watering-setup";
static const uint32_t WIFI_CONNECT_TIMEOUT_MS = 20000;

// MQTT
static const char* MQTT_HOST = "CHANGE_ME";
static const uint16_t MQTT_PORT = 1883;
static const char* MQTT_USER = nullptr;
static const char* MQTT_PASS = nullptr;
static const char* MQTT_CLIENT_ID = "pump-esp32";

// Topic prefix (base) -> <PREFIX>/WateringController/...
static const char* MQTT_PREFIX = "home/veranda";

// Time
static const char* NTP_SERVER = "pool.ntp.org";

// OTA
static const bool OTA_ENABLED = true;
static const char* OTA_HOSTNAME = "pump-esp32";
static const char* OTA_PASSWORD = "CHANGE_ME";

// GPIO (ESP32-S3 safe defaults; update per board)
static const uint8_t RELAY_PIN = 21;
static const bool RELAY_ACTIVE_HIGH = true;

// Safety
static const uint32_t WATERLEVEL_STALE_MS = 10UL * 60UL * 1000UL;

// Publish state periodically even if unchanged
static const uint32_t STATE_PUBLISH_INTERVAL_MS = 60UL * 1000UL;
