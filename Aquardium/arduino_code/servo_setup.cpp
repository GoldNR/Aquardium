#include "servo_setup.h"

#define SERVO_PIN 9

WiFiUDP Udp;
NTPClient timeClient(Udp, "time.google.com", 8 * 3600);
Servo servo;

int targetHour, targetMinute, pos;
bool hasRotatedForTheDay;

void servoSetup() {
  servo.attach(SERVO_PIN);
  RTC.begin();

  timeClient.begin();
}

void servoLoop() {
  timeClient.update();
  //Serial.println(timeClient.getFormattedTime());

  targetHour = (int) EEPROM.read(0);
  targetMinute = (int) EEPROM.read(1);

  if (timeClient.getHours() == targetHour && timeClient.getMinutes() == targetMinute && hasRotatedForTheDay == false) {
    for (pos = 0; pos <= 180; pos += 1) {
      servo.write(pos);
      delay(15);
    }
    for (pos = 180; pos >= 0; pos -= 1) {
      servo.write(pos);
      delay(15);
    }
    hasRotatedForTheDay = true;
  }

  else if (timeClient.getHours() <= targetHour && timeClient.getMinutes() < targetMinute && hasRotatedForTheDay == true)
    hasRotatedForTheDay = false;
}