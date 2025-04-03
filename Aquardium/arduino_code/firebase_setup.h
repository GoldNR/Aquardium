#ifndef FIREBASE_SETUP_H
#define FIREBASE_SETUP_H
#include <FirebaseClient.h>
#include <WiFiSSLClient.h>
#include <Arduino.h>

extern WiFiSSLClient ssl_client;
extern UserAuth user_auth;
extern FirebaseApp app;
extern RealtimeDatabase database;
extern AsyncResult databaseResult;

extern const String deviceID;

void firebaseSetup();
void firebaseLoop();

#endif