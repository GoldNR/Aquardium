#include "ultrasonic_setup.h"

#define TRIG_PIN 7
#define ECHO_PIN 6

long duration;
int distance_int;
std::deque<int> distValues;
bool lowFeed = false;
int average = 0;

void ussSetup() {
  pinMode(TRIG_PIN, OUTPUT);
  
  pinMode(ECHO_PIN, INPUT);
}

void ussLoop() {
  digitalWrite(TRIG_PIN, LOW);
  delayMicroseconds(2);

  digitalWrite(TRIG_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIG_PIN, LOW);

  duration = pulseIn(ECHO_PIN, HIGH);
  distance_int = duration * 0.34 / 2;

  distValues.push_back(distance_int);

  if (distValues.size() > 100) {
    distValues.pop_front();

    if (average >= 50) {
      lowFeed = true;
    }
  }
  if (distValues.size() == 100) {
    for (int d : distValues) {
      average += d;
    }
    average /= distValues.size();
    //Serial.println(average);
  }
}