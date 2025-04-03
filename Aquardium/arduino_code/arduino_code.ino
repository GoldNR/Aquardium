#include "wifi_setup.h"
#include "firebase_setup.h"
#include "ble_setup.h"
#include "temperature_setup.h"
#include "ultrasonic_setup.h"
#include "servo_setup.h"

const String deviceID PROGMEM = "arduino-1";          //Rename with an available name, must contain "arduino"

// ADJUST THE VALUES IN THE #DEFINE DIRECTIVES AT THE OTHER SOURCE FILES ACCORDINGLY

void setup() {
  Serial.begin(115200);
  bleSetup();
  wifiSetup();
  //firebaseSetup();
  tempSetup();
  ussSetup();
  servoSetup();
}

void loop() {
  bleLoop();
  wifiLoop();
  //firebaseLoop();
  tempLoop();
  ussLoop();
  servoLoop();
  delay(5000);
}