#ifndef CONNECTION_INTERFACE_H
#define CONNECTION_INTERFACE_H

#include "data_types.h"
#include <functional>

namespace connection {

/**
 * Callback type for configuration updates
 */
using ConfigCallback = std::function<void(const data::NodeConfig&)>;

/**
 * Connection result structure
 */
struct ConnectionResult {
    bool success;
    std::string errorMessage;
    int statusCode;

    ConnectionResult() : success(false), statusCode(0) {}

    static ConnectionResult ok() {
        ConnectionResult r;
        r.success = true;
        r.statusCode = 200;
        return r;
    }

    static ConnectionResult error(const std::string& msg, int code = 0) {
        ConnectionResult r;
        r.success = false;
        r.errorMessage = msg;
        r.statusCode = code;
        return r;
    }
};

/**
 * Interface for connection implementations
 *
 * Supports different connection modes:
 * - HTTP (REST API)
 * - MQTT (future Sprint S2)
 * - LoRaWAN (future Sprint S3)
 */
class IConnection {
public:
    virtual ~IConnection() = default;

    /**
     * Connect to the Hub
     * @return true if connection successful
     */
    virtual bool connect() = 0;

    /**
     * Check if currently connected
     * @return true if connected
     */
    virtual bool isConnected() const = 0;

    /**
     * Disconnect from the Hub
     */
    virtual void disconnect() = 0;

    /**
     * Register this node with the Hub
     * Sends NodeInfo and receives NodeConfig
     *
     * @param info Node information to register
     * @return Received NodeConfig (check isValid())
     */
    virtual data::NodeConfig registerNode(const data::NodeInfo& info) = 0;

    /**
     * Send a sensor reading to the Hub
     *
     * @param reading Reading to send
     * @return ConnectionResult with status
     */
    virtual ConnectionResult sendReading(const data::Reading& reading) = 0;

    /**
     * Set callback for configuration updates
     * Called when Hub pushes new configuration
     *
     * @param callback Function to call on config update
     */
    virtual void onConfigReceived(ConfigCallback callback) = 0;

    /**
     * Get the connection mode identifier
     * @return Mode string ("http", "mqtt", "lorawan")
     */
    virtual std::string getMode() const = 0;
};

} // namespace connection

#endif // CONNECTION_INTERFACE_H
