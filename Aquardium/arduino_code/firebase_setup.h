#ifndef FIREBASE_SETUP_H
#define FIREBASE_SETUP_H
#include <WiFiS3.h>
#include <ArduinoHttpClient.h>
#include <Arduino.h>

extern const String deviceID;
extern WiFiSSLClient wifi; 
extern HttpClient httpClient;
extern String path;

void firebaseSetup();
void firebaseLoop();

#endif