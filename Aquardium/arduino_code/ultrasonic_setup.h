#ifndef ULTRASONIC_SETUP_H
#define ULTRASONIC_SETUP_H
#include <Arduino.h>
#include <deque>

extern long duration;
extern int distance_int;
extern bool lowFeed;
extern std::deque<int> distValues;
extern int average;

void ussSetup();
void ussLoop();

#endif