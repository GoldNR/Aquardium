#include "temperature_setup.h"

#define ONE_WIRE_BUS 2

OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature tempSensor(&oneWire);

String tempMessage, tempReading;
char tempBuffer[8];

void tempSetup() {
  tempSensor.begin();
}

void tempLoop() {
  //Serial.println("Temp Loop started.");

  tempSensor.requestTemperatures();
  tempReading = dtostrf(tempSensor.getTempCByIndex(0), 1, 2, tempBuffer);
  
  //Serial.println("Temp Loop finished.");
}