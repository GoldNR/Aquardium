#ifndef BLE_SETUP_H
#define BLE_SETUP_H
#include <ArduinoBLE.h>
#include <ArduinoJson.h>
#include <EEPROM.h>

extern BLEService service; 
extern BLECharacteristic tempCharacteristic;
extern BLECharacteristic servoCharacteristic;
extern BLEDevice central;

extern const String deviceID;

void bleSetup();
void bleLoop();
void onServoCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic);

#endif