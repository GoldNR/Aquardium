#include "servo_setup.h"

#define SERVO_PIN 9

WiFiUDP Udp;
NTPClient timeClient(Udp, "time.google.com", 8 * 3600);
uRTCLib rtc(0x68);
Servo servo;

int targetHour, targetMinute, pos, centerPos = 90,
  year, month, day, hour, minute, second, weekday;
bool hasRotatedForTheDay;
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

  timeLastFed = String(rtc.month()) + "/" + String(rtc.day()) + "/" + String(rtc.year()) + " " + String(rtc.hour()) + ":" + String(rtc.minute());
  Serial.print("Time last rotated: ");
  Serial.println(timeLastFed.c_str());
}

void servoSetup() {
  servo.attach(SERVO_PIN);
  servo.write(centerPos);

  timeClient.begin();
  timeClient.update();

  URTCLIB_WIRE.begin();
  unsigned long unixTime = timeClient.getEpochTime();
  epochToDateTime(unixTime, year, month, day, hour, minute, second, weekday);
  rtc.set(second, minute, hour, weekday, day, month, year);
}

void servoLoop() {
  rtc.refresh();

  targetHour = (int) EEPROM.read(0);
  targetMinute = (int) EEPROM.read(1);

  if (timeClient.update()) {
    if (timeClient.getHours() == targetHour && timeClient.getMinutes() == targetMinute && hasRotatedForTheDay == false) {
      rotateServo();
      hasRotatedForTheDay = true;
    }

    else if (timeClient.getHours() <= targetHour && timeClient.getMinutes() < targetMinute && hasRotatedForTheDay == true)
      hasRotatedForTheDay = false;
  }

  else {
    if (rtc.hour() == targetHour && rtc.minute() == targetMinute && hasRotatedForTheDay == false) {
      rotateServo();
      hasRotatedForTheDay = true;
    }

    else if (rtc.hour() <= targetHour && rtc.minute() < targetMinute && hasRotatedForTheDay == true)
      hasRotatedForTheDay = false;
  }
}