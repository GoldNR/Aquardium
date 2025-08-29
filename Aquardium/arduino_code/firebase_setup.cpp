#include "firebase_setup.h"
#include "temperature_setup.h"
#include "ultrasonic_setup.h"
#include "CONFIDENTIAL.h"

WiFiSSLClient wifi;
HttpClient httpClient = HttpClient(wifi, DATABASE_URL, 443);
String path = "/feeders/" + deviceID + ".json?auth=" + DATABASE_SECRET;

void firebaseSetup() {
  
}

void firebaseLoop() {
  if (WiFi.status() == WL_CONNECTED) {
    String jsonData = "{\"temperature\": " + tempReading + 
                      ", \"ultrasonic\": " + String(average) + "}";

    httpClient.beginRequest();
    httpClient.patch(path);
    httpClient.sendHeader("Content-Type", "application/json");
    httpClient.sendHeader("Content-Length", jsonData.length());
    httpClient.beginBody();
    httpClient.print(jsonData);
    httpClient.endRequest();

    int statusCode = httpClient.responseStatusCode();
    String response = httpClient.responseBody();

    Serial.print("HTTP Status: ");
    Serial.println(statusCode);
    Serial.print("Response: ");
    Serial.println(response);

    delay(5000);
  }
}