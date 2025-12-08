/**
 * myIoTGrid.Sensor - State Machine
 * Node Provisioning State Machine for ESP32
 *
 * States:
 * - UNCONFIGURED: Initial state, no configuration stored
 * - PAIRING: BLE pairing mode active, waiting for Hub configuration
 * - CONFIGURED: WiFi configured and connected, operational
 * - ERROR: Error state with recovery mechanism
 */

#ifndef STATE_MACHINE_H
#define STATE_MACHINE_H

#include <Arduino.h>
#include <functional>

/**
 * Node provisioning states
 */
enum class NodeState {
    UNCONFIGURED,  // No configuration, ready for BLE pairing
    PAIRING,       // BLE pairing mode active
    CONFIGURED,    // Configured, connecting to WiFi
    OPERATIONAL,   // Fully operational, sending data
    ERROR,         // Error state (temporary, will retry)
    RE_PAIRING     // Re-provisioning mode: BLE active + WiFi retry parallel
};

/**
 * State transition events
 */
enum class StateEvent {
    BOOT,              // Device booted
    CONFIG_FOUND,      // Configuration found in NVS
    NO_CONFIG,         // No configuration in NVS
    BLE_PAIR_START,    // BLE pairing started
    BLE_CONFIG_RECEIVED, // Configuration received via BLE
    WIFI_CONNECTED,    // WiFi connection successful
    WIFI_FAILED,       // WiFi connection failed
    API_VALIDATED,     // API key validated with Hub
    API_FAILED,        // API validation failed
    RESET_REQUESTED,   // Factory reset requested
    ERROR_OCCURRED,    // Error occurred
    RETRY_TIMEOUT,     // Retry timeout expired

    // RE_PAIRING Events (Story 1)
    MAX_RETRIES_REACHED,   // Maximum retry count exceeded → enter RE_PAIRING
    NEW_WIFI_RECEIVED,     // New WiFi credentials received via BLE
    OLD_WIFI_FOUND,        // Old WiFi credentials worked during retry
    WIFI_RETRY_TIMER       // Timer tick for parallel WiFi retry (every 30s)
};

/**
 * State callbacks
 */
using StateEnterCallback = std::function<void(NodeState previousState)>;
using StateExitCallback = std::function<void(NodeState nextState)>;

/**
 * State Machine for Node Provisioning
 */
class StateMachine {
public:
    StateMachine();

    /**
     * Process a state event
     */
    void processEvent(StateEvent event);

    /**
     * Get current state
     */
    NodeState getState() const { return _currentState; }

    /**
     * Get state name as string
     */
    static const char* getStateName(NodeState state);

    /**
     * Get event name as string
     */
    static const char* getEventName(StateEvent event);

    /**
     * Check if in specific state
     */
    bool isInState(NodeState state) const { return _currentState == state; }

    /**
     * Register callbacks for state enter/exit
     */
    void onEnterState(NodeState state, StateEnterCallback callback);
    void onExitState(NodeState state, StateExitCallback callback);

    /**
     * Get retry count for current error
     */
    int getRetryCount() const { return _retryCount; }

    /**
     * Reset retry count
     */
    void resetRetryCount() { _retryCount = 0; }

    /**
     * Increment retry count
     */
    void incrementRetryCount() { _retryCount++; }

    /**
     * Get maximum retry count before giving up
     */
    static constexpr int MAX_RETRIES = 3;

    /**
     * Get max retries (for display)
     */
    int getMaxRetries() const { return MAX_RETRIES; }

    /**
     * Get retry delay in milliseconds
     */
    int getRetryDelay() const;

private:
    NodeState _currentState;
    int _retryCount;

    // Callbacks (6 states including RE_PAIRING)
    StateEnterCallback _enterCallbacks[6];
    StateExitCallback _exitCallbacks[6];

    /**
     * Transition to a new state
     */
    void transitionTo(NodeState newState);

    /**
     * Get index for state
     */
    static int stateIndex(NodeState state);
};

#endif // STATE_MACHINE_H
