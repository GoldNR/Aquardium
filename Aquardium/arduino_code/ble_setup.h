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
extern BLECharacteristic resetCharacteristic;
extern BLECharacteristic ssidCharacteristic;
extern BLECharacteristic passCharacteristic;
extern BLEDevice central;

extern const String deviceID;

void bleSetup();
void bleLoop();
void writeCharArrayToEEPROM(int startAddr, const char* data, int maxLen);
void onServoCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic);
void onFeedNowCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic);
void onResetCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic);
void onSsidCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic);
void onPassCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic);
void BLEPoll();

#endif