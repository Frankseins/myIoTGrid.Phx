/**
 * myIoTGrid.Sensor - LED Controller
 * Status LED patterns for different node states
 *
 * LED Patterns (Story 3 - Sprint S1.2):
 * - UNCONFIGURED:  Slow blink (1s on, 1s off)
 * - PAIRING:       Fast blink (200ms on, 200ms off)
 * - CONFIGURED:    Double blink (2x quick blink, then pause)
 * - OPERATIONAL:   Solid on with brief off every 5s (heartbeat)
 * - ERROR:         Triple fast blink (3x, then pause)
 * - RE_PAIRING:    Double blink every 2s (2x blink, 2s pause) - WICHTIG!
 */

#ifndef LED_CONTROLLER_H
#define LED_CONTROLLER_H

#include <Arduino.h>
#include "state_machine.h"

/**
 * LED Pattern types matching NodeState
 */
enum class LEDPattern {
    OFF,            // LED completely off
    SOLID,          // LED always on
    SLOW_BLINK,     // UNCONFIGURED: 1s on, 1s off
    FAST_BLINK,     // PAIRING: 200ms on, 200ms off
    DOUBLE_BLINK,   // CONFIGURED: 2x quick blink, then 1s pause
    HEARTBEAT,      // OPERATIONAL: Solid with brief off every 5s
    TRIPLE_BLINK,   // ERROR: 3x quick blink, then 1s pause
    RE_PAIRING_BLINK // RE_PAIRING: 2x blink every 2s (distinctive pattern)
};

/**
 * LED Controller for status indication
 */
class LEDController {
public:
    LEDController();

    /**
     * Initialize LED controller with pin number
     * @param pin GPIO pin for status LED (default: 2 for built-in LED on most ESP32)
     * @param activeLow True if LED is active low (on when pin is LOW)
     */
    void init(int pin = 2, bool activeLow = false);

    /**
     * Set LED pattern based on node state
     * Automatically maps NodeState to appropriate LEDPattern
     */
    void setStatePattern(NodeState state);

    /**
     * Set specific LED pattern
     */
    void setPattern(LEDPattern pattern);

    /**
     * Get current pattern
     */
    LEDPattern getPattern() const { return _currentPattern; }

    /**
     * Update LED state (call in loop)
     * Handles timing and pattern execution
     */
    void update();

    /**
     * Turn LED on
     */
    void on();

    /**
     * Turn LED off
     */
    void off();

    /**
     * Check if LED is currently on
     */
    bool isOn() const { return _ledOn; }

    /**
     * Get pattern name for debugging
     */
    static const char* getPatternName(LEDPattern pattern);

private:
    int _pin;
    bool _activeLow;
    bool _initialized;
    bool _ledOn;
    LEDPattern _currentPattern;

    // Timing state
    unsigned long _lastUpdate;
    int _blinkPhase;       // Current phase in multi-blink patterns
    bool _inPause;         // True during pause phase of patterns

    // Pattern timing constants
    static constexpr unsigned long SLOW_BLINK_MS = 1000;
    static constexpr unsigned long FAST_BLINK_MS = 200;
    static constexpr unsigned long QUICK_BLINK_MS = 150;
    static constexpr unsigned long HEARTBEAT_INTERVAL_MS = 5000;
    static constexpr unsigned long HEARTBEAT_OFF_MS = 100;
    static constexpr unsigned long PATTERN_PAUSE_MS = 1000;
    static constexpr unsigned long RE_PAIRING_PAUSE_MS = 2000;  // 2s pause for RE_PAIRING
    static constexpr unsigned long RE_PAIRING_BLINK_MS = 200;   // Blink duration

    /**
     * Internal: Set hardware LED state
     */
    void setHardwareLED(bool on);
};

#endif // LED_CONTROLLER_H
