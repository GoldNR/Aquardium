#include "servo_setup.h"

#define SERVO_PIN 9

WiFiUDP Udp;
NTPClient timeClient(Udp, "time.google.com", 8 * 3600);
uRTCLib rtc(0x68);
Servo servo;

int targetHour, targetMinute, pos, centerPos = 90,
  year, month, day, hour, minute, second, weekday;
bool hasRotatedForTheDay1 = false,
     hasRotatedForTheDay2 = false,
     hasRotatedForTheDay3 = false;
String timeLastFed;
String timeLastFedMessage;

void epochToDateTime(unsigned long epoch, int &year, int &month, int &day, int &hour, int &minute, int &second, int &weekday) {
  second = epoch % 60;
  epoch /= 60;
  minute = epoch % 60;
  epoch /= 60;
  hour = epoch % 24;
  epoch /= 24;

  // Days since Jan 1, 1970
  unsigned long days = epoch;

  // Calculate weekday (0 = Thursday Jan 1, 1970)
  weekday = (days + 4) % 7 + 1; // uRTCLib: 1 = Sunday

  int y = 1970;
  while (true) {
    bool leap = (y % 4 == 0 && (y % 100 != 0 || y % 400 == 0));
    int daysInYear = leap ? 366 : 365;
    if (days >= daysInYear) {
      days -= daysInYear;
      y++;
    } else {
      break;
    }
  }

  year = y;

  int monthDays[] = {31,28,31,30,31,30,31,31,30,31,30,31};
  if (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) {
    monthDays[1] = 29;
  }

  int m = 0;
  while (days >= monthDays[m]) {
    days -= monthDays[m];
    m++;
  }

  month = m + 1;
  day = days + 1;
}

void rotateServo() {
  servo.write(0);
  delay(500);

  servo.write(180);
  delay(500);

  servo.write(90);

  /*
  servo.write(0);
  delay(550);

  servo.write(90);
  delay(3000);
  */

  EEPROM.put(3, rtc.month());
  EEPROM.put(4, rtc.day());
  EEPROM.put(5, rtc.year());
  EEPROM.put(6, rtc.hour());
  EEPROM.put(7, rtc.minute());
}

void servoSetup() {
  servo.attach(SERVO_PIN);
  servo.write(centerPos);

  timeClient.begin();
  timeClient.update();

  URTCLIB_WIRE.begin();
  unsigned long unixTime = timeClient.getEpochTime();
  epochToDateTime(unixTime, year, month, day, hour, minute, second, weekday);
  rtc.set(second, minute, hour, weekday, day, month, (year % 100));
}

void servoLoop() {
  //Serial.println("Servo Loop started.");

  rtc.refresh();

  int targetHour1 = (int) EEPROM.read(0);
  int targetMinute1 = (int) EEPROM.read(1);
  int targetHour2 = (int) EEPROM.read(68);
  int targetMinute2 = (int) EEPROM.read(69);
  int targetHour3 = (int) EEPROM.read(70);
  int targetMinute3 = (int) EEPROM.read(71);

  int currentHour, currentMinute;

  if (WiFi.status() == WL_CONNECTED) {
    if (timeClient.update()) {
      currentHour = timeClient.getHours();
      currentMinute = timeClient.getMinutes();
    }

    else {
      currentHour = rtc.hour();
      currentMinute = rtc.minute();
    }
  }

  auto checkAndRotate = [&](int targetHour, int targetMinute, bool &hasRotatedFlag) {
    if (currentHour == targetHour && currentMinute == targetMinute && !hasRotatedFlag) {
      rotateServo();
      hasRotatedFlag = true;
    }
    else if ((currentHour < targetHour || (currentHour == targetHour && currentMinute < targetMinute)) && hasRotatedFlag) {
      hasRotatedFlag = false;
    }
  };

  checkAndRotate(targetHour1, targetMinute1, hasRotatedForTheDay1);
  checkAndRotate(targetHour2, targetMinute2, hasRotatedForTheDay2);
  checkAndRotate(targetHour3, targetMinute3, hasRotatedForTheDay3);

  timeLastFed = String(EEPROM.read(3)) + "/"
              + String(EEPROM.read(4)) + "/"
              + String(EEPROM.read(5)) + " "
              + String(EEPROM.read(6)) + ":"
              + String(EEPROM.read(7));

  //Serial.println("Servo Loop finished.");
}