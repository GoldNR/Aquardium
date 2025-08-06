#include "ble_setup.h"
#include "temperature_setup.h"
#include "turbidity_setup.h"
#include "servo_setup.h"

BLEService service("12345678-1234-5678-1234-56789abcdef0"); 
BLECharacteristic tempCharacteristic("12345678-1234-5678-1234-56789abcdef1", BLENotify, 10);
BLECharacteristic turbidityCharacteristic("12345678-1234-5678-1234-56789abcdef2", BLENotify, 10);
BLECharacteristic servoCharacteristic("12345678-1234-5678-1234-56789abcdef3", BLEWrite, 30);
BLECharacteristic feedNowCharacteristic("12345678-1234-5678-1234-56789abcdef4", BLEWrite, 5);
BLECharacteristic timeLastFedCharacteristic("12345678-1234-5678-1234-56789abcdef5", BLENotify, 20);
BLEDevice central;

void bleSetup() {
  BLE.setLocalName(deviceID.c_str());
  BLE.setDeviceName(deviceID.c_str());
  while (!BLE.begin()) {
    Serial.println("Starting...");
    delay(1000);
  }
  BLE.setAdvertisedService(service);
  service.addCharacteristic(servoCharacteristic);
  service.addCharacteristic(turbidityCharacteristic);
  service.addCharacteristic(tempCharacteristic);
  service.addCharacteristic(feedNowCharacteristic);
  service.addCharacteristic(timeLastFedCharacteristic);
  servoCharacteristic.setEventHandler(BLEWritten, onServoCharacteristicWritten);
  feedNowCharacteristic.setEventHandler(BLEWritten, onFeedNowCharacteristicWritten);
  BLE.addService(service);
  BLE.advertise();
}

void bleLoop() {
  BLE.poll();
  central = BLE.central();
  if (central && central.connected()) {
    tempCharacteristic.writeValue(tempReading.c_str(), false);
    turbidityCharacteristic.writeValue(turbReading.c_str(), false);
    timeLastFedCharacteristic.writeValue(timeLastFed.c_str(), false);
  }
}

void onServoCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic) {
  Serial.println("RECEIVED SERVO MESSAGE");
  char receivedData[servoCharacteristic.valueLength() + 1];
  servoCharacteristic.readValue((uint8_t*)receivedData, servoCharacteristic.valueLength());
  receivedData[servoCharacteristic.valueLength()] = '\0';
  StaticJsonDocument<100> doc;
  DeserializationError error = deserializeJson(doc, receivedData);

  Serial.println(receivedData);

  if (error) {
    Serial.print(F("deserializeJson() failed: "));
    Serial.println(error.f_str());
    return;
  }

  int hour = doc["hour"];
  int minute = doc["minute"];

  EEPROM.put(0, hour);
  EEPROM.put(1, minute);

  Serial.print("New time set: ");
  Serial.print(hour);
  Serial.print(":");
  Serial.println(minute);
}

void onFeedNowCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic) {
  rotateServo();
}