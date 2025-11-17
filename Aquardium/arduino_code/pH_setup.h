#ifndef PH_SETUP
#define PH_SETUP
#include <Arduino.h>

extern float voltage;
extern float phValue;
extern float phReadings[10];
extern int readingIndex;
extern float acidVoltage;
extern float neutralVoltage;
extern String pHReading;
extern String pHMessage;
extern String voltage_str;

extern void pHSetup();
extern void pHLoop();

#endif