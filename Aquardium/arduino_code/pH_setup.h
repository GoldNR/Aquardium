#ifndef PH_SETUP
#define PH_SETUP
#include <Arduino.h>

extern const int pHpin;
extern float pHValue;
extern String pHReading;
extern String pHMessage;

extern void pHSetup();
extern void pHLoop();

#endif