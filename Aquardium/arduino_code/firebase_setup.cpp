#include "firebase_setup.h"
#include "ultrasonic_setup.h"

#define API_KEY "AIzaSyB9-IlZ6V_DgM0SgvTcViypMwg9uBATrbM"
#define DATABASE_URL "https://aquardium-push-notif-default-rtdb.asia-southeast1.firebasedatabase.app/"

WiFiSSLClient ssl_client;
using AsyncClient = AsyncClientClass;
AsyncClient aClient(ssl_client);
UserAuth user_auth(API_KEY, "", "");
FirebaseApp app;
RealtimeDatabase database;
AsyncResult databaseResult;

void processData(AsyncResult &aResult)
{
    // Exits when no result available when calling from the loop.
  if (!aResult.isResult())
      return;

  if (aResult.isEvent())
    Firebase.printf("Event task: %s, msg: %s, code: %d\n", aResult.uid().c_str(), aResult.eventLog().message().c_str(), aResult.eventLog().code());

  if (aResult.isDebug())
    Firebase.printf("Debug task: %s, msg: %s\n", aResult.uid().c_str(), aResult.debug().c_str());

  if (aResult.isError())
    Firebase.printf("Error task: %s, msg: %s, code: %d\n", aResult.uid().c_str(), aResult.error().message().c_str(), aResult.error().code());

  if (aResult.available())
    Firebase.printf("task: %s, payload: %s\n", aResult.uid().c_str(), aResult.c_str());
}

static void auth_debug_print(AsyncResult &aResult) {
  if (aResult.isEvent())
    Firebase.printf("Event task: %s, msg: %s, code: %d\n", aResult.uid().c_str(), aResult.eventLog().message().c_str(), aResult.eventLog().code());

  if (aResult.isDebug())
    Firebase.printf("Debug task: %s, msg: %s\n", aResult.uid().c_str(), aResult.debug().c_str());

  if (aResult.isError())
    Firebase.printf("Error task: %s, msg: %s, code: %d\n", aResult.uid().c_str(), aResult.error().message().c_str(), aResult.error().code());
}

void firebaseSetup() {
  Firebase.printf("Firebase Client v%s\n", FIREBASE_CLIENT_VERSION);
  initializeApp(aClient, app, getAuth(user_auth), auth_debug_print, "🔐 authTask");
  app.getApp<RealtimeDatabase>(database);
  database.url(DATABASE_URL);
}

void firebaseLoop() {
  app.loop();

  if (app.ready()) {
    Serial.println("Updating... ");
    database.set<bool>(aClient, "/feeders/" + deviceID + "/low_feed", true);
    database.set<int>(aClient, "/feeders/" + deviceID + "/distance_value", distance_int);
  }

  processData(databaseResult);
}