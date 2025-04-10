#include "turbidity_setup.h"

int turbValue;
String turbReading;
String turbMessage;

void turbiditySetup() {}

void turbidityLoop() {
  turbValue = analogRead(A0);
  turbReading = String(turbValue);
  Serial.println(turbValue);
}