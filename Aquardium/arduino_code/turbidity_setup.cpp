#include "turbidity_setup.h"

int turbValue;
String turbReading;
String turbMessage;

void turbiditySetup() {}

void turbidityLoop() {
  Serial.println("Turbidity Loop started.");

  turbValue = analogRead(A0);
  turbReading = String(turbValue);
  // distance when empty: 58

  Serial.println("Turbidity Loop finished.");
}