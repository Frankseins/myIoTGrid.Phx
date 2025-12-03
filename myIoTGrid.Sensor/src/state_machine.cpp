/**
 * myIoTGrid.Sensor - State Machine Implementation
 */

#include "state_machine.h"

StateMachine::StateMachine()
    : _currentState(NodeState::UNCONFIGURED)
    , _retryCount(0) {
    for (int i = 0; i < 5; i++) {
        _enterCallbacks[i] = nullptr;
        _exitCallbacks[i] = nullptr;
    }
}

void StateMachine::processEvent(StateEvent event) {
    Serial.printf("[StateMachine] Processing event: %s in state: %s\n",
                  getEventName(event), getStateName(_currentState));

    switch (_currentState) {
        case NodeState::UNCONFIGURED:
            switch (event) {
                case StateEvent::CONFIG_FOUND:
                    transitionTo(NodeState::CONFIGURED);
                    break;
                case StateEvent::NO_CONFIG:
                case StateEvent::BLE_PAIR_START:
                    transitionTo(NodeState::PAIRING);
                    break;
                case StateEvent::ERROR_OCCURRED:
                    transitionTo(NodeState::ERROR);
                    break;
                default:
                    break;
            }
            break;

        case NodeState::PAIRING:
            switch (event) {
                case StateEvent::BLE_CONFIG_RECEIVED:
                    // Stay in PAIRING until WiFi connects
                    break;
                case StateEvent::WIFI_CONNECTED:
                    transitionTo(NodeState::CONFIGURED);
                    break;
                case StateEvent::WIFI_FAILED:
                    transitionTo(NodeState::ERROR);
                    break;
                case StateEvent::RESET_REQUESTED:
                    transitionTo(NodeState::UNCONFIGURED);
                    break;
                case StateEvent::ERROR_OCCURRED:
                    transitionTo(NodeState::ERROR);
                    break;
                default:
                    break;
            }
            break;

        case NodeState::CONFIGURED:
            switch (event) {
                case StateEvent::API_VALIDATED:
                    // API key valid - transition to OPERATIONAL
                    resetRetryCount();
                    transitionTo(NodeState::OPERATIONAL);
                    break;
                case StateEvent::API_FAILED:
                case StateEvent::WIFI_FAILED:
                    transitionTo(NodeState::ERROR);
                    break;
                case StateEvent::RESET_REQUESTED:
                    transitionTo(NodeState::UNCONFIGURED);
                    break;
                case StateEvent::ERROR_OCCURRED:
                    transitionTo(NodeState::ERROR);
                    break;
                default:
                    break;
            }
            break;

        case NodeState::OPERATIONAL:
            switch (event) {
                case StateEvent::WIFI_FAILED:
                    transitionTo(NodeState::CONFIGURED);  // Try to reconnect
                    break;
                case StateEvent::API_FAILED:
                case StateEvent::ERROR_OCCURRED:
                    transitionTo(NodeState::ERROR);
                    break;
                case StateEvent::RESET_REQUESTED:
                    transitionTo(NodeState::UNCONFIGURED);
                    break;
                default:
                    break;
            }
            break;

        case NodeState::ERROR:
            switch (event) {
                case StateEvent::RETRY_TIMEOUT:
                    if (_retryCount < MAX_RETRIES) {
                        incrementRetryCount();
                        // Try to recover based on what's available
                        transitionTo(NodeState::PAIRING);
                    } else {
                        // Max retries reached, stay in error
                        Serial.println("[StateMachine] Max retries reached, staying in ERROR state");
                    }
                    break;
                case StateEvent::RESET_REQUESTED:
                    resetRetryCount();
                    transitionTo(NodeState::UNCONFIGURED);
                    break;
                case StateEvent::WIFI_CONNECTED:
                    resetRetryCount();
                    transitionTo(NodeState::CONFIGURED);
                    break;
                case StateEvent::BLE_PAIR_START:
                    transitionTo(NodeState::PAIRING);
                    break;
                default:
                    break;
            }
            break;
    }
}

void StateMachine::transitionTo(NodeState newState) {
    if (_currentState == newState) {
        return;
    }

    Serial.printf("[StateMachine] Transition: %s -> %s\n",
                  getStateName(_currentState), getStateName(newState));

    NodeState previousState = _currentState;

    // Call exit callback
    int exitIdx = stateIndex(_currentState);
    if (_exitCallbacks[exitIdx]) {
        _exitCallbacks[exitIdx](newState);
    }

    _currentState = newState;

    // Call enter callback
    int enterIdx = stateIndex(newState);
    if (_enterCallbacks[enterIdx]) {
        _enterCallbacks[enterIdx](previousState);
    }
}

void StateMachine::onEnterState(NodeState state, StateEnterCallback callback) {
    _enterCallbacks[stateIndex(state)] = callback;
}

void StateMachine::onExitState(NodeState state, StateExitCallback callback) {
    _exitCallbacks[stateIndex(state)] = callback;
}

int StateMachine::getRetryDelay() const {
    // Exponential backoff: 1s, 2s, 4s, 8s, 16s
    return 1000 * (1 << _retryCount);
}

int StateMachine::stateIndex(NodeState state) {
    return static_cast<int>(state);
}

const char* StateMachine::getStateName(NodeState state) {
    switch (state) {
        case NodeState::UNCONFIGURED: return "UNCONFIGURED";
        case NodeState::PAIRING: return "PAIRING";
        case NodeState::CONFIGURED: return "CONFIGURED";
        case NodeState::OPERATIONAL: return "OPERATIONAL";
        case NodeState::ERROR: return "ERROR";
        default: return "UNKNOWN";
    }
}

const char* StateMachine::getEventName(StateEvent event) {
    switch (event) {
        case StateEvent::BOOT: return "BOOT";
        case StateEvent::CONFIG_FOUND: return "CONFIG_FOUND";
        case StateEvent::NO_CONFIG: return "NO_CONFIG";
        case StateEvent::BLE_PAIR_START: return "BLE_PAIR_START";
        case StateEvent::BLE_CONFIG_RECEIVED: return "BLE_CONFIG_RECEIVED";
        case StateEvent::WIFI_CONNECTED: return "WIFI_CONNECTED";
        case StateEvent::WIFI_FAILED: return "WIFI_FAILED";
        case StateEvent::API_VALIDATED: return "API_VALIDATED";
        case StateEvent::API_FAILED: return "API_FAILED";
        case StateEvent::RESET_REQUESTED: return "RESET_REQUESTED";
        case StateEvent::ERROR_OCCURRED: return "ERROR_OCCURRED";
        case StateEvent::RETRY_TIMEOUT: return "RETRY_TIMEOUT";
        default: return "UNKNOWN";
    }
}
