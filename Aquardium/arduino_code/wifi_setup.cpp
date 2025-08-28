#include "wifi_setup.h"
#include "temperature_setup.h"
#include "turbidity_setup.h"
#include "servo_setup.h"

#define MQTT_SERVER "broker.hivemq.com"

WiFiClient espClient;
PubSubClient client(espClient); 

const String isOnlineMessage PROGMEM = "{\"id\":\"" + deviceID + "\",\"status\":\"online\"}";
const String willMessageStr PROGMEM = "{\"id\":\"" + deviceID + "\",\"status\":\"offline\"}";
const String servoTimeTopic PROGMEM = deviceID + "/servo";
const String rotateNowTopic PROGMEM = deviceID + "/now";
const String resetTopic PROGMEM = deviceID + "/reset";

String readStringFromEEPROM(int addrOffset) {
  char data[64];
  int len = 0;
  unsigned char k;

  k = EEPROM.read(addrOffset);
  while (k != '\0' && len < 63) {
    data[len] = k;
    len++;
    k = EEPROM.read(addrOffset + len);
  }
  data[len] = '\0';
  return String(data);
}

void reconnect() {
  if (WiFi.status() != WL_CONNECTED) {
    String ssid = readStringFromEEPROM(8);
    String pass = readStringFromEEPROM(38);
    WiFi.begin(ssid.c_str(), pass.c_str());
    Serial.println(ssid.c_str());
    Serial.println(pass.c_str());
  }
  if (client.connect(deviceID.c_str(), "status", 0, true, willMessageStr.c_str())) {
    client.subscribe(servoTimeTopic.c_str());
    client.subscribe(rotateNowTopic.c_str());
    Serial.println(isOnlineMessage.c_str());
    Serial.println("Connected to MQTT broker.");
  } else Serial.println("Connecting...");
}

void callback(char* topic, byte* payload, unsigned int length) {
  if (strcmp(topic, servoTimeTopic.c_str()) == 0) {
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

  else if (strcmp(topic, rotateNowTopic.c_str()) == 0) {
    rotateServo();
  }

  else if (strcmp(topic, resetTopic.c_str()) == 0) {
    Serial.println("Resetting Arduino...");
    NVIC_SystemReset();
  }
}

void wifiSetup() {
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

    timeLastFedMessage = "{\"id\":\"" + deviceID + "\",\"timeLastFed\":\"" + timeLastFed + "\"}";
    client.publish("sensors/timeLastFed", timeLastFedMessage.c_str());

    client.loop();
  }
  else reconnect();
}
