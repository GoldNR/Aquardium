#include "ble_setup.h"
#include "temperature_setup.h"

BLEService service("00001809-0000-1000-8000-00805f9b34fb"); 
BLECharacteristic tempCharacteristic("00002a6e-0000-1000-8000-00805f9b34fb", BLERead | BLENotify, 20);
BLECharacteristic servoCharacteristic("00002a5c-0000-1000-8000-00805f9b34fb", BLEWrite | BLEWriteWithoutResponse, 100);
BLEDevice central;

void bleSetup() {
  while (!BLE.begin()) {
    Serial.println("Starting...");
    delay(1000);
  }
  BLE.setLocalName(deviceID.c_str());
  BLE.setDeviceName(deviceID.c_str());
  BLE.setAdvertisedService(service);
  service.addCharacteristic(tempCharacteristic);
  service.addCharacteristic(servoCharacteristic);
  BLE.addService(service);
  BLE.advertise();
}

void bleLoop() {
  central = BLE.central();
  if (central && central.connected()) {
    tempCharacteristic.writeValue(tempReading.c_str(), false);

    if (servoCharacteristic.written()) {
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
  }
}