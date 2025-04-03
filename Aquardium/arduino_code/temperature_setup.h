#ifndef TEMPERATURE_SETUP_H
#define TEMPERATURE_SETUP_H
#include <Arduino.h>
#include <DallasTemperature.h>

extern OneWire oneWire;
extern DallasTemperature tempSensor;

extern String tempMessage;
extern String tempReading;
extern char tempBuffer[8];

void tempSetup();
void tempLoop();

#endif