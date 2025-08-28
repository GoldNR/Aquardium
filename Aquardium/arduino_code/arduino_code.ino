#include "wifi_setup.h"
#include "firebase_setup.h"
#include "ble_setup.h"
#include "temperature_setup.h"
#include "ultrasonic_setup.h"
#include "servo_setup.h"
#include "turbidity_setup.h"

/* TAKEN DIGITAL PINS: 
    2: Temperature              7: Ultrasonic TRIG
    6: Ultrasonic ECHO          9: Servo
    
 TAKEN ANALOG PINS: AO: Turbidity

 TAKEN EEPROM ADDRESSES:
    1: Hour
    2: Minute
    3: Time Last Fed (TLF) Month
    4: TLF Day
    5: TLF Year
    6: TLF Hour
    7: TLF Minute
    8-37: Network SSID
    38-67: Network Password
*/

const String deviceID PROGMEM = "arduino-1";          //Rename with an available name, must contain "arduino"

// ADJUST THE VALUES IN THE #DEFINE DIRECTIVES AT THE OTHER SOURCE FILES ACCORDINGLY

void setup() {
  Serial.begin(115200);
  bleSetup();
  wifiSetup();
  firebaseSetup();
  tempSetup();
  ussSetup();
  servoSetup();
  turbiditySetup();
}

void loop() {
  bleLoop();
  BLEPoll();

  wifiLoop();
  BLEPoll();

  firebaseLoop();
  BLEPoll();

  tempLoop();

  BLEPoll();
  ussLoop();

  BLEPoll();
  servoLoop();

  BLEPoll();
  turbidityLoop();
}