#include <Arduino.h>
#include <WiFi.h>
#include <AsyncMqttClient.h>
#include <ArduinoJson.h>
#include <ArduinoOTA.h>
#include <WebServer.h>
#include <Preferences.h>
#include <time.h>
#include "config.h"
#include "water_level_logic.h"

static AsyncMqttClient mqttClient;
static bool mqttConnected = false;
static uint32_t lastMqttAttemptMs = 0;
static uint32_t wifiConnectStartMs = 0;

static WaterLevelLogic logic(PUBLISH_INTERVAL_MS);
static bool otaReady = false;
static bool configPortalActive = false;
static WebServer configServer(80);
static Preferences preferences;
static String wifiSsid;
static String wifiPassword;

static String isoUtcNow()
{
  time_t now = time(nullptr);
  if (now < 1700000000)
  {
    return String("1970-01-01T00:00:00Z");
  }

  struct tm tmUtc;
  gmtime_r(&now, &tmUtc);
  char buf[25];
  strftime(buf, sizeof(buf), "%Y-%m-%dT%H:%M:%SZ", &tmUtc);
  return String(buf);
}

static void loadWifiCredentials()
{
  preferences.begin("wifi", true);
  wifiSsid = preferences.getString("ssid", "");
  wifiPassword = preferences.getString("pass", "");
  preferences.end();

  if (wifiSsid.length() == 0 && WIFI_SSID && strlen(WIFI_SSID) > 0 && String(WIFI_SSID) != "CHANGE_ME")
  {
    wifiSsid = WIFI_SSID;
    wifiPassword = WIFI_PASSWORD;
  }
}

static void saveWifiCredentials(const String& ssid, const String& password)
{
  preferences.begin("wifi", false);
  preferences.putString("ssid", ssid);
  preferences.putString("pass", password);
  preferences.end();
}

static void startConfigPortal()
{
  if (configPortalActive)
  {
    return;
  }

  configPortalActive = true;
  WiFi.mode(WIFI_AP);
  if (WIFI_AP_PASSWORD && strlen(WIFI_AP_PASSWORD) >= 8)
  {
    WiFi.softAP(WIFI_AP_SSID, WIFI_AP_PASSWORD);
  }
  else
  {
    WiFi.softAP(WIFI_AP_SSID);
  }

  configServer.on("/", HTTP_GET, []()
  {
    const char page[] =
      "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>WiFi Setup</title></head>"
      "<body><h2>WiFi Setup</h2>"
      "<form method=\"POST\" action=\"/save\">"
      "<label>SSID</label><br/>"
      "<input name=\"ssid\" maxlength=\"64\"/><br/>"
      "<label>Password</label><br/>"
      "<input name=\"pass\" type=\"password\" maxlength=\"64\"/><br/>"
      "<button type=\"submit\">Save</button>"
      "</form></body></html>";
    configServer.send(200, "text/html", page);
  });

  configServer.on("/save", HTTP_POST, []()
  {
    String ssid = configServer.arg("ssid");
    String pass = configServer.arg("pass");
    if (ssid.length() == 0)
    {
      configServer.send(400, "text/plain", "SSID required.");
      return;
    }
    saveWifiCredentials(ssid, pass);
    configServer.send(200, "text/plain", "Saved. Rebooting...");
    delay(1000);
    ESP.restart();
  });

  configServer.begin();
}

static String topicWaterLevel()
{
  return String(MQTT_PREFIX) + "/WateringController/waterlevel/state";
}

static void ensureWifi()
{
  if (configPortalActive)
  {
    return;
  }

  if (WiFi.status() == WL_CONNECTED)
  {
    wifiConnectStartMs = 0;
    return;
  }

  if (wifiSsid.length() == 0)
  {
    startConfigPortal();
    return;
  }

  WiFi.mode(WIFI_STA);
  if (wifiConnectStartMs == 0)
  {
    wifiConnectStartMs = millis();
    WiFi.begin(wifiSsid.c_str(), wifiPassword.c_str());
  }
  else if (millis() - wifiConnectStartMs > WIFI_CONNECT_TIMEOUT_MS)
  {
    startConfigPortal();
  }
}

static void ensureOta()
{
  if (!OTA_ENABLED || otaReady || WiFi.status() != WL_CONNECTED)
  {
    return;
  }

  ArduinoOTA.setHostname(OTA_HOSTNAME);
  if (OTA_PASSWORD && strlen(OTA_PASSWORD) > 0)
  {
    ArduinoOTA.setPassword(OTA_PASSWORD);
  }

  ArduinoOTA.begin();
  otaReady = true;
}

static void ensureMqtt()
{
  if (mqttConnected || WiFi.status() != WL_CONNECTED)
  {
    return;
  }

  const uint32_t now = millis();
  if (now - lastMqttAttemptMs < 5000)
  {
    return;
  }

  lastMqttAttemptMs = now;
  mqttClient.connect();
}

static void ensureTime()
{
  static bool configured = false;
  if (!configured)
  {
    configTime(0, 0, NTP_SERVER);
    configured = true;
  }
}

static std::array<bool, 4> readSensors()
{
  std::array<bool, 4> sensors{ { false, false, false, false } };
  for (int i = 0; i < 4; i++)
  {
    sensors[i] = digitalRead(SENSOR_PINS[i]) == HIGH;
  }
  return sensors;
}

static void publishState(const std::array<bool, 4>& sensors)
{
  if (!mqttConnected)
  {
    return;
  }

  WaterLevelSnapshot snapshot = logic.BuildSnapshot(sensors);
  JsonDocument doc;
  doc["levelPercent"] = snapshot.levelPercent;
  JsonArray arr = doc["sensors"].to<JsonArray>();
  for (int i = 0; i < 4; i++)
  {
    arr.add(snapshot.sensors[i]);
  }

  const String nowIso = isoUtcNow();
  doc["measuredAt"] = nowIso;
  doc["reportedAt"] = nowIso;

  String payload;
  serializeJson(doc, payload);
  mqttClient.publish(
    topicWaterLevel().c_str(),
    1,
    true,
    payload.c_str(),
    payload.length());
}

void setup()
{
  Serial.begin(115200);
  for (int i = 0; i < 4; i++)
  {
    pinMode(SENSOR_PINS[i], INPUT);
  }

  loadWifiCredentials();

  ensureWifi();
  ensureOta();
  ensureTime();
  mqttClient.setServer(MQTT_HOST, MQTT_PORT);
  mqttClient.setCredentials(MQTT_USER, MQTT_PASS);
  mqttClient.setClientId(MQTT_CLIENT_ID);
  mqttClient.onConnect([](bool) { mqttConnected = true; });
  mqttClient.onDisconnect([](AsyncMqttClientDisconnectReason) { mqttConnected = false; });
}

void loop()
{
  ensureWifi();
  ensureOta();
  ensureTime();
  ensureMqtt();
  ArduinoOTA.handle();

  if (configPortalActive)
  {
    configServer.handleClient();
    delay(10);
    return;
  }

  std::array<bool, 4> sensors = readSensors();
  const uint32_t nowMs = millis();
  const bool changed = logic.HasChanged(sensors);

  if (logic.ShouldPublish(changed, nowMs))
  {
    publishState(sensors);
    logic.MarkPublished(sensors, nowMs);
  }

  delay(50);
}
