#ifndef TURBIDITY_SETUP_H
#define TURBIDITY_SETUP_H
#include <Arduino.h>

extern int turbValue;
extern String turbReading;
extern String turbMessage;

void turbiditySetup();
void turbidityLoop();

#endif