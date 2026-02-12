#include <Arduino.h>
#include <WiFi.h>
#include <AsyncMqttClient.h>
#include <ArduinoJson.h>
#include <ArduinoOTA.h>
#include <WebServer.h>
#include <Preferences.h>
#include <time.h>
#include "config.h"
#include "pump_logic.h"

static AsyncMqttClient mqttClient;
static bool mqttConnected = false;
static uint32_t lastMqttAttemptMs = 0;
static uint32_t wifiConnectStartMs = 0;

static PumpLogic pumpLogic(WATERLEVEL_STALE_MS);

static uint32_t lastStatePublishMs = 0;
static bool subscribed = false;
static bool otaReady = false;
static String incomingTopic;
static String incomingPayload;
static size_t incomingTotal = 0;
static bool configPortalActive = false;
static WebServer configServer(80);
static Preferences preferences;
static String wifiSsid;
static String wifiPassword;

static String topicPumpCmd()
{
  return String(MQTT_PREFIX) + "/WateringController/pump/cmd";
}

static String topicPumpState()
{
  return String(MQTT_PREFIX) + "/WateringController/pump/state";
}

static String topicWaterLevel()
{
  return String(MQTT_PREFIX) + "/WateringController/waterlevel/state";
}

static void setRelay(bool on)
{
  digitalWrite(RELAY_PIN, RELAY_ACTIVE_HIGH ? (on ? HIGH : LOW) : (on ? LOW : HIGH));
}

static std::string isoUtcNow()
{
  time_t now = time(nullptr);
  if (now < 1700000000)
  {
    return std::string("1970-01-01T00:00:00Z");
  }

  struct tm tmUtc;
  gmtime_r(&now, &tmUtc);
  char buf[25];
  strftime(buf, sizeof(buf), "%Y-%m-%dT%H:%M:%SZ", &tmUtc);
  return std::string(buf);
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

static void publishPumpState()
{
  JsonDocument doc;
  const PumpLogicState& state = pumpLogic.State();
  doc["running"] = state.pumpRunning;
  if (state.pumpRunning)
  {
    doc["since"] = state.pumpStartIso.c_str();
  }
  else
  {
    doc["since"] = nullptr;
  }
  doc["lastRunSeconds"] = state.pumpRunSeconds;
  doc["lastRequestId"] = state.lastRequestId.c_str();
  doc["reportedAt"] = isoUtcNow().c_str();

  String payload;
  serializeJson(doc, payload);
  mqttClient.publish(
    topicPumpState().c_str(),
    1,
    true,
    payload.c_str(),
    payload.length());
  lastStatePublishMs = millis();
}

static void applyDecision(const PumpDecision& decision)
{
  if (decision.action == PumpDecision::Action::None)
  {
    return;
  }

  const uint32_t nowMs = millis();
  if (decision.action == PumpDecision::Action::Start)
  {
    pumpLogic.ApplyDecision(decision, nowMs, isoUtcNow());
    setRelay(true);
  }
  else
  {
    pumpLogic.ApplyDecision(decision, nowMs, "");
    setRelay(false);
  }

  publishPumpState();
}

static void handlePumpCmd(const JsonDocument& doc)
{
  const char* action = doc["action"] | "start";
  const char* requestId = doc["requestId"] | "";
  const int runSeconds = doc["runSeconds"] | 0;

  const PumpDecision decision = pumpLogic.EvaluateCommand(
    std::string(action),
    runSeconds,
    std::string(requestId),
    millis());
  applyDecision(decision);
}

static void mqttCallback(
  char* topic,
  char* payload,
  AsyncMqttClientMessageProperties,
  size_t len,
  size_t index,
  size_t total)
{
  if (index == 0)
  {
    incomingTopic = topic;
    incomingPayload = "";
    incomingPayload.reserve(total);
    incomingTotal = total;
  }

  incomingPayload.concat(payload, len);

  if (index + len < total)
  {
    return;
  }

  JsonDocument doc;
  DeserializationError err = deserializeJson(doc, incomingPayload);
  if (err)
  {
    return;
  }

  if (incomingTopic == topicPumpCmd())
  {
    handlePumpCmd(doc);
  }
  else if (incomingTopic == topicWaterLevel())
  {
    int level = doc["levelPercent"] | -1;
    pumpLogic.UpdateWaterLevel(level, millis());
  }
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

void setup()
{
  Serial.begin(115200);
  pinMode(RELAY_PIN, OUTPUT);
  setRelay(false);

  loadWifiCredentials();

  ensureWifi();
  ensureOta();
  ensureTime();
  mqttClient.setServer(MQTT_HOST, MQTT_PORT);
  mqttClient.setCredentials(MQTT_USER, MQTT_PASS);
  mqttClient.setClientId(MQTT_CLIENT_ID);
  mqttClient.onMessage(mqttCallback);
  mqttClient.onConnect([](bool) { mqttConnected = true; subscribed = false; });
  mqttClient.onDisconnect([](AsyncMqttClientDisconnectReason) {
    mqttConnected = false;
    subscribed = false;
    applyDecision(pumpLogic.OnMqttDisconnected());
  });
}

void loop()
{
  ensureWifi();
  ensureOta();
  ensureTime();
  ensureMqtt();

  if (configPortalActive)
  {
    configServer.handleClient();
    delay(10);
    return;
  }

  if (mqttConnected)
  {
    if (!subscribed)
    {
      mqttClient.subscribe(topicPumpCmd().c_str(), 1);
      mqttClient.subscribe(topicWaterLevel().c_str(), 1);
      subscribed = true;
      publishPumpState();
    }
    ArduinoOTA.handle();
  }
  else
  {
    applyDecision(pumpLogic.OnMqttDisconnected());
    delay(200);
    return;
  }

  applyDecision(pumpLogic.OnTick(millis()));

  if (millis() - lastStatePublishMs >= STATE_PUBLISH_INTERVAL_MS)
  {
    publishPumpState();
  }
}
