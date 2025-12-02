/**
 * myIoTGrid Sensor - Main Entry Point
 *
 * This firmware runs on:
 * - ESP32 hardware (real or simulated sensors)
 * - Native platform (Linux/Docker simulator)
 *
 * The main loop:
 * 1. Initializes HAL and controller
 * 2. Registers with Hub or loads saved config
 * 3. Reads sensors at configured interval
 * 4. Sends readings to Hub via HTTP REST API
 */

#include "node_controller.h"
#include "hal/hal.h"
#include "config.h"

// Global controller instance
static controller::NodeController nodeController;

#ifdef PLATFORM_NATIVE
// Native platform - standard main function
int main() {
    // Try to initialize
    while (!nodeController.setup()) {
        hal::log_error("Setup failed, retrying in 5 seconds...");
        hal::delay_ms(5000);
    }

    // Main loop
    while (true) {
        nodeController.loop();
    }

    return 0;
}

#else
// Arduino/ESP32 platform - setup() and loop() functions

void setup() {
    // Initialize serial for debugging
    Serial.begin(115200);
    delay(1000); // Wait for serial

    Serial.println();
    Serial.println("myIoTGrid Sensor Starting...");

    // Try to initialize
    while (!nodeController.setup()) {
        hal::log_error("Setup failed, retrying in 5 seconds...");
        hal::delay_ms(5000);
    }
}

void loop() {
    nodeController.loop();
}

#endif
