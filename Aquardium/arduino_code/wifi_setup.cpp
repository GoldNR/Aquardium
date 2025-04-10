#include "wifi_setup.h"
#include "temperature_setup.h"
#include "turbidity_setup.h"
//CodeJust
//J09295550934j

#define SSID "04FA_767de0"
#define PASSWORD "wlan89821f"
#define MQTT_SERVER "mqtt.eclipseprojects.io"

WiFiClient espClient;
PubSubClient client(espClient);

const String isOnlineMessage PROGMEM = "{\"id\":\"" + deviceID + "\",\"status\":\"online\"}";
const String willMessageStr PROGMEM = "{\"id\":\"" + deviceID + "\",\"status\":\"offline\"}";
const String servoTopic PROGMEM = deviceID + "/servo";

void reconnect() {
  if (client.connect(deviceID.c_str(), "status", 0, true, willMessageStr.c_str())) {
    client.subscribe(servoTopic.c_str());
    Serial.println(isOnlineMessage.c_str());
    Serial.println("Connected to MQTT broker.");
  } else Serial.println("Connecting...");
}

void callback(char* topic, byte* payload, unsigned int length) {
  if (strcmp(topic, servoTopic.c_str()) == 0) {
    StaticJsonDocument<100> doc;

    char jsonString[length + 1];
    memcpy(jsonString, payload, length);
    jsonString[length] = '\0';

    Serial.println(jsonString);

    DeserializationError error = deserializeJson(doc, jsonString);

    if (error) {
      Serial.print(F("deserializeJson() failed: "));
      Serial.println(error.f_str());
      return;
    }

    int hour = doc["hour"];
    int minute = doc["minute"];

    EEPROM.put(0, hour);
    EEPROM.put(1, minute);

    Serial.print("New time set: ");
    Serial.print(hour);
    Serial.print(":");
    Serial.println(minute);
  }
}

void wifiSetup() {
  WiFi.begin(SSID, PASSWORD);

  client.setServer(MQTT_SERVER, 1883);
  client.setCallback(callback);
  reconnect();
}

void wifiLoop() {
  if (client.connected()) {
    client.publish("status", isOnlineMessage.c_str());

    tempMessage = "{\"id\":\"" + deviceID + "\",\"temp\":\"" + tempReading + "\"}";
    client.publish("sensors/temperature", tempMessage.c_str());

    turbMessage = "{\"id\":\"" + deviceID + "\",\"turbidity\":\"" + turbReading + "\"}";
    client.publish("sensors/turbidity", turbMessage.c_str());

    client.loop();
  }
  else reconnect();
}