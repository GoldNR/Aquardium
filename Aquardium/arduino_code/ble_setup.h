#ifndef BLE_SETUP_H
#define BLE_SETUP_H
#include <ArduinoBLE.h>
#include <ArduinoJson.h>
#include <EEPROM.h>

extern BLEService service;
extern BLECharacteristic tempCharacteristic;
extern BLECharacteristic turbidityCharacteristic;
extern BLECharacteristic servoCharacteristic;
extern BLECharacteristic feedNowCharacteristic;
extern BLECharacteristic timeLastFedCharacteristic;
extern BLEDevice central;

extern const String deviceID;

void bleSetup();
void bleLoop();
void onServoCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic);
void onFeedNowCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic);

#endif