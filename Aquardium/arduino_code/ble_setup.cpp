#include "ble_setup.h"
#include "temperature_setup.h"
#include "turbidity_setup.h"
#include "servo_setup.h"

BLEService service("12345678-1234-5678-1234-56789abcdef0"); 
BLECharacteristic tempCharacteristic("12345678-1234-5678-1234-56789abcdef1", BLENotify, 10);
BLECharacteristic turbidityCharacteristic("12345678-1234-5678-1234-56789abcdef2", BLENotify, 10);
BLECharacteristic servoCharacteristic("12345678-1234-5678-1234-56789abcdef3", BLEWrite, 30); //formerly 30
BLECharacteristic feedNowCharacteristic("12345678-1234-5678-1234-56789abcdef4", BLEWrite, 5);
BLECharacteristic timeLastFedCharacteristic("12345678-1234-5678-1234-56789abcdef5", BLENotify, 20);
BLECharacteristic resetCharacteristic("12345678-1234-5678-1234-56789abcdef6", BLEWrite, 5);
BLECharacteristic ssidCharacteristic("12345678-1234-5678-1234-56789abcdef7", BLEWrite, 30); //formerly 30
BLECharacteristic passCharacteristic("12345678-1234-5678-1234-56789abcdef8", BLEWrite, 30); //formerly 30
BLEDevice central;

void bleSetup() {
  BLE.setLocalName(deviceID.c_str());
  BLE.setDeviceName(deviceID.c_str());
  if (!BLE.begin()) {
    Serial.println("BLE init failed");
    while (1);  // Halt execution
  }
  BLE.setAdvertisedService(service);
  service.addCharacteristic(servoCharacteristic);
  service.addCharacteristic(turbidityCharacteristic);
  service.addCharacteristic(tempCharacteristic);
  service.addCharacteristic(feedNowCharacteristic);
  service.addCharacteristic(timeLastFedCharacteristic);
  service.addCharacteristic(resetCharacteristic);
  service.addCharacteristic(ssidCharacteristic);
  service.addCharacteristic(passCharacteristic);
  servoCharacteristic.setEventHandler(BLEWritten, onServoCharacteristicWritten);
  feedNowCharacteristic.setEventHandler(BLEWritten, onFeedNowCharacteristicWritten);
  resetCharacteristic.setEventHandler(BLEWritten, onResetCharacteristicWritten);
  ssidCharacteristic.setEventHandler(BLEWritten, onSsidCharacteristicWritten);
  passCharacteristic.setEventHandler(BLEWritten, onPassCharacteristicWritten);
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

void writeCharArrayToEEPROM(int startAddr, const char* data, int maxLen) {
    int i = 0;
    for (; i < maxLen; i++) {
      char c = data[i];
      EEPROM.write(startAddr + i, c);
      if (c == '\0') break;
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

void onResetCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic) {
  Serial.println("Resetting Arduino...");
  NVIC_SystemReset();
}

void onSsidCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic) {
  char receivedData[ssidCharacteristic.valueLength() + 1];
  ssidCharacteristic.readValue((uint8_t*)receivedData, ssidCharacteristic.valueLength());
  receivedData[ssidCharacteristic.valueLength()] = '\0';
  Serial.println(receivedData);

  writeCharArrayToEEPROM(3, receivedData, 31);
}

void onPassCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic) {
  char receivedData[passCharacteristic.valueLength() + 1];
  passCharacteristic.readValue((uint8_t*)receivedData, passCharacteristic.valueLength());
  receivedData[passCharacteristic.valueLength()] = '\0';
  Serial.println(receivedData);

  writeCharArrayToEEPROM(34, receivedData, 63);
}

void BLEPoll() {
  BLE.poll();
}