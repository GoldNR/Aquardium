#include "pH_setup.h"

const int phPin = A1;      // Analog input pin for pH sensor
float voltage, phValue;
float phReadings[10];      // Array to store the latest 10 pH values

int readingIndex = 0;      // Index to keep track of where we are in the array
float acidVoltage = 4.64;  // Voltage at pH 4.0
float neutralVoltage = 4.16; // Voltage at pH 7.0
String pHReading;
String pHMessage;
String voltage_str;

void pHSetup() {
  for (int i = 0; i < 10; i++) {
    phReadings[i] = 0.0;
  }
}

void pHLoop() {
  int sensorValue = analogRead(phPin);          // Read raw analog value
  voltage = (sensorValue * 5.0) / 1023.0;       // Convert to voltage (assuming 5V ADC reference)

  // Linear conversion based on calibration points
  // slope = (pH7 - pH4) / (Vneutral - Vacid)
  float slope = (7.0 - 4.0) / (neutralVoltage - acidVoltage);
  float intercept = 7.0 - slope * neutralVoltage;

  phValue = slope * voltage + intercept;

  // Store the pH value in the array and update the index
  phReadings[readingIndex] = phValue;
  readingIndex = (readingIndex + 1) % 10; // Wrap around the index after reaching 10

  // Calculate the average pH of the last 10 readings
  float averagePH = 0.0;
  for (int i = 0; i < 10; i++) {
    averagePH += phReadings[i];
  }
  averagePH /= 10.0;
  pHReading = String(averagePH);
  voltage_str = String(voltage);
}
