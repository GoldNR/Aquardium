#ifndef SERVO_SETUP_H
#define SERVO_SETUP_H
//#include <Arduino.h>
#include <Servo.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include <EEPROM.h>
#include "uRTCLib.h"
#include "wifi_setup.h"

extern Servo servo;
extern WiFiUDP Udp;
extern NTPClient timeClient;
extern uRTCLib rtc;
extern int targetHour;
extern int targetMinute;
extern int pos;
extern int centerPos;
extern int year;
extern int month;
extern int day;
extern int hour;
extern int minute;
extern int second;
extern int weekday;
extern bool hasRotatedForTheDay;
extern String timeLastFed;
extern String timeLastFedMessage;

void epochToDateTime(unsigned long epoch, int &year, int &month, int &day, int &hour, int &minute, int &second, int &weekday);
void rotateServo();
void servoSetup();
void servoLoop();

#endif