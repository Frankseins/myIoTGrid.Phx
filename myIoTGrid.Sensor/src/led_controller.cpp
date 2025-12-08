/**
 * myIoTGrid.Sensor - LED Controller Implementation
 */

#include "led_controller.h"

LEDController::LEDController()
    : _pin(2)
    , _activeLow(false)
    , _initialized(false)
    , _ledOn(false)
    , _currentPattern(LEDPattern::OFF)
    , _lastUpdate(0)
    , _blinkPhase(0)
    , _inPause(false)
{
}

void LEDController::init(int pin, bool activeLow) {
    _pin = pin;
    _activeLow = activeLow;
    _initialized = true;

    pinMode(_pin, OUTPUT);
    off();  // Start with LED off

    Serial.printf("[LED] Initialized on pin %d (active %s)\n",
                  _pin, _activeLow ? "LOW" : "HIGH");
}

void LEDController::setStatePattern(NodeState state) {
    LEDPattern pattern;

    switch (state) {
        case NodeState::UNCONFIGURED:
            pattern = LEDPattern::SLOW_BLINK;
            break;
        case NodeState::PAIRING:
            pattern = LEDPattern::FAST_BLINK;
            break;
        case NodeState::CONFIGURED:
            pattern = LEDPattern::DOUBLE_BLINK;
            break;
        case NodeState::OPERATIONAL:
            pattern = LEDPattern::HEARTBEAT;
            break;
        case NodeState::ERROR:
            pattern = LEDPattern::TRIPLE_BLINK;
            break;
        case NodeState::RE_PAIRING:
            pattern = LEDPattern::RE_PAIRING_BLINK;
            break;
        default:
            pattern = LEDPattern::OFF;
            break;
    }

    setPattern(pattern);
}

void LEDController::setPattern(LEDPattern pattern) {
    if (_currentPattern != pattern) {
        _currentPattern = pattern;
        _lastUpdate = millis();
        _blinkPhase = 0;
        _inPause = false;

        Serial.printf("[LED] Pattern changed to: %s\n", getPatternName(pattern));

        // Handle immediate state for some patterns
        switch (pattern) {
            case LEDPattern::OFF:
                off();
                break;
            case LEDPattern::SOLID:
            case LEDPattern::HEARTBEAT:
                on();
                break;
            default:
                // Other patterns start with LED on
                on();
                break;
        }
    }
}

void LEDController::update() {
    if (!_initialized) return;

    unsigned long now = millis();
    unsigned long elapsed = now - _lastUpdate;

    switch (_currentPattern) {
        case LEDPattern::OFF:
            // Nothing to do
            break;

        case LEDPattern::SOLID:
            // Nothing to do, LED stays on
            break;

        case LEDPattern::SLOW_BLINK:
            // 1s on, 1s off
            if (elapsed >= SLOW_BLINK_MS) {
                _lastUpdate = now;
                if (_ledOn) {
                    off();
                } else {
                    on();
                }
            }
            break;

        case LEDPattern::FAST_BLINK:
            // 200ms on, 200ms off
            if (elapsed >= FAST_BLINK_MS) {
                _lastUpdate = now;
                if (_ledOn) {
                    off();
                } else {
                    on();
                }
            }
            break;

        case LEDPattern::DOUBLE_BLINK:
            // 2x quick blink, then 1s pause
            // Phase 0: LED on (first blink)
            // Phase 1: LED off
            // Phase 2: LED on (second blink)
            // Phase 3: LED off (long pause)
            if (_inPause) {
                if (elapsed >= PATTERN_PAUSE_MS) {
                    _lastUpdate = now;
                    _inPause = false;
                    _blinkPhase = 0;
                    on();
                }
            } else {
                if (elapsed >= QUICK_BLINK_MS) {
                    _lastUpdate = now;
                    _blinkPhase++;

                    if (_blinkPhase == 1 || _blinkPhase == 3) {
                        off();
                    } else if (_blinkPhase == 2) {
                        on();
                    } else if (_blinkPhase >= 4) {
                        _inPause = true;
                        _blinkPhase = 0;
                    }
                }
            }
            break;

        case LEDPattern::HEARTBEAT:
            // Solid on with brief off every 5s
            if (_ledOn) {
                if (elapsed >= HEARTBEAT_INTERVAL_MS) {
                    _lastUpdate = now;
                    off();
                }
            } else {
                if (elapsed >= HEARTBEAT_OFF_MS) {
                    _lastUpdate = now;
                    on();
                }
            }
            break;

        case LEDPattern::TRIPLE_BLINK:
            // 3x quick blink, then 1s pause
            if (_inPause) {
                if (elapsed >= PATTERN_PAUSE_MS) {
                    _lastUpdate = now;
                    _inPause = false;
                    _blinkPhase = 0;
                    on();
                }
            } else {
                if (elapsed >= QUICK_BLINK_MS) {
                    _lastUpdate = now;
                    _blinkPhase++;

                    if (_blinkPhase == 1 || _blinkPhase == 3 || _blinkPhase == 5) {
                        off();
                    } else if (_blinkPhase == 2 || _blinkPhase == 4) {
                        on();
                    } else if (_blinkPhase >= 6) {
                        _inPause = true;
                        _blinkPhase = 0;
                    }
                }
            }
            break;

        case LEDPattern::RE_PAIRING_BLINK:
            // RE_PAIRING: 2x blink every 2s
            // Distinctive pattern: 2 quick blinks, then 2 second pause
            // Phase 0: LED on (first blink)
            // Phase 1: LED off (short)
            // Phase 2: LED on (second blink)
            // Phase 3: LED off (2s pause)
            if (_inPause) {
                if (elapsed >= RE_PAIRING_PAUSE_MS) {
                    _lastUpdate = now;
                    _inPause = false;
                    _blinkPhase = 0;
                    on();
                }
            } else {
                if (elapsed >= RE_PAIRING_BLINK_MS) {
                    _lastUpdate = now;
                    _blinkPhase++;

                    if (_blinkPhase == 1 || _blinkPhase == 3) {
                        off();
                    } else if (_blinkPhase == 2) {
                        on();
                    } else if (_blinkPhase >= 4) {
                        _inPause = true;
                        _blinkPhase = 0;
                    }
                }
            }
            break;
    }
}

void LEDController::on() {
    setHardwareLED(true);
    _ledOn = true;
}

void LEDController::off() {
    setHardwareLED(false);
    _ledOn = false;
}

void LEDController::setHardwareLED(bool on) {
    if (!_initialized) return;

    if (_activeLow) {
        digitalWrite(_pin, on ? LOW : HIGH);
    } else {
        digitalWrite(_pin, on ? HIGH : LOW);
    }
}

const char* LEDController::getPatternName(LEDPattern pattern) {
    switch (pattern) {
        case LEDPattern::OFF: return "OFF";
        case LEDPattern::SOLID: return "SOLID";
        case LEDPattern::SLOW_BLINK: return "SLOW_BLINK";
        case LEDPattern::FAST_BLINK: return "FAST_BLINK";
        case LEDPattern::DOUBLE_BLINK: return "DOUBLE_BLINK";
        case LEDPattern::HEARTBEAT: return "HEARTBEAT";
        case LEDPattern::TRIPLE_BLINK: return "TRIPLE_BLINK";
        case LEDPattern::RE_PAIRING_BLINK: return "RE_PAIRING_BLINK";
        default: return "UNKNOWN";
    }
}
