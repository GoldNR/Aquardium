#ifndef WIFI_SETUP_H
#define WIFI_SETUP_H
#include <Arduino.h>
#include <WiFiS3.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <EEPROM.h>

extern WiFiClient espClient;
extern PubSubClient client;

extern const String deviceID;
extern const String isOnlineMessage PROGMEM;
extern const String willMessageStr PROGMEM;
extern const String servoTopic PROGMEM;

void reconnect();
void callback(char* topic, byte* payload, unsigned int length);
void wifiSetup();
void wifiLoop();

#endif