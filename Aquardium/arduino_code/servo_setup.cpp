#include "servo_setup.h"

#define SERVO_PIN 9

WiFiUDP Udp;
NTPClient timeClient(Udp, "time.google.com", 8 * 3600);
Servo servo;

int targetHour, targetMinute, pos, centerPos = 90;
bool hasRotatedForTheDay;

void servoSetup() {
  servo.attach(SERVO_PIN);
  RTC.begin();
  servo.write(centerPos);

  timeClient.begin();
}

void servoLoop() {
  timeClient.update();
  //Serial.println(timeClient.getFormattedTime());

  targetHour = (int) EEPROM.read(0);
  targetMinute = (int) EEPROM.read(1);

  if (timeClient.getHours() == targetHour && timeClient.getMinutes() == targetMinute && hasRotatedForTheDay == false) {
    servo.write(centerPos + 90);
    delay(500);

    servo.write(centerPos);
    delay(500);
    
    servo.write(centerPos - 90);
    delay(500);

    servo.write(centerPos - 180);
    delay(500); 

    servo.write(centerPos + 90);
    delay(500); 

    servo.write(centerPos);
    delay(500);

    //

    servo.write(centerPos + 90);
    delay(500);

    servo.write(centerPos);
    delay(500);
    
    servo.write(centerPos - 90);
    delay(500);

    servo.write(centerPos - 180);
    delay(500); 

    servo.write(centerPos + 90);
    delay(500); 

    servo.write(centerPos);
    delay(500);

    hasRotatedForTheDay = true;
  }

  else if (timeClient.getHours() <= targetHour && timeClient.getMinutes() < targetMinute && hasRotatedForTheDay == true)
    hasRotatedForTheDay = false;
}