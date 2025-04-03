#ifndef SERVO_SETUP_H
#define SERVO_SETUP_H
#include <Servo.h>
#include <RTC.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include <EEPROM.h>
#include "wifi_setup.h"

extern Servo servo;
extern WiFiUDP Udp;
extern NTPClient timeClient;
extern int targetHour;
extern int targetMinute;
extern int pos;
extern bool hasRotatedForTheDay;

void servoSetup();
void servoLoop();

#endif