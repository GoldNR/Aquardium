#include "pH_setup.h"

const int pHpin = A1;
float pHValue;
String pHReading, pHMessage;

void pHSetup() {

}

void pHLoop() {
  //Serial.println("pH Loop started.");

  int sensorValue = analogRead(pHpin);
  pHValue = map(sensorValue, 0, 1023, 0, 14);
  pHReading = String(pHValue);

  //Serial.println("pH Loop finished.");
}