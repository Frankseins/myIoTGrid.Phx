#ifndef NODE_CONTROLLER_H
#define NODE_CONTROLLER_H

#include "config_manager.h"
#include "connection_interface.h"
#include "sensor_interface.h"
#include "sensor_factory.h"
#include <memory>
#include <vector>
#include <map>

namespace controller {

/**
 * NodeController - Main controller orchestrating all components
 *
 * Responsibilities:
 * - Initialize HAL and components
 * - Register with Hub or load saved config
 * - Create sensors based on configuration
 * - Execute measurement loop at configured interval
 * - Send readings to Hub via connection
 */
class NodeController {
public:
    NodeController();
    ~NodeController() = default;

    /**
     * Initialize the controller
     * Sets up HAL, loads config, registers if needed
     * @return true if initialization successful
     */
    bool setup();

    /**
     * Main loop iteration
     * Should be called repeatedly from main()
     * Handles timing and executes readings when interval reached
     */
    void loop();

    /**
     * Check if controller is running
     * @return true if setup completed successfully
     */
    bool isRunning() const;

    /**
     * Get current configuration
     */
    const data::NodeConfig& getConfig() const;

    /**
     * Force re-registration with Hub
     * Deletes saved config and registers fresh
     */
    bool reregister();

private:
    ConfigManager configManager_;
    std::unique_ptr<connection::IConnection> connection_;
    std::map<std::string, std::unique_ptr<sensor::ISensor>> sensors_;

    bool running_;
    uint32_t lastReadTime_;
    uint32_t readingCount_;

    /**
     * Initialize network connection (WiFi for ESP32)
     */
    bool initNetwork();

    /**
     * Build Hub endpoint URL from environment or defaults
     */
    std::string buildHubEndpoint();

    /**
     * Register with Hub and get configuration
     */
    bool registerWithHub();

    /**
     * Create connection based on config mode
     */
    std::unique_ptr<connection::IConnection> createConnection(const data::ConnectionConfig& connConfig);

    /**
     * Initialize sensors from configuration
     */
    void initSensors();

    /**
     * Build NodeInfo for registration
     */
    data::NodeInfo buildNodeInfo();

    /**
     * Execute one measurement cycle
     * Reads all sensors and sends readings
     */
    void executeReadingCycle();

    /**
     * Create and send a reading for a sensor
     */
    void sendSensorReading(const std::string& type, sensor::ISensor* sensor);
};

} // namespace controller

#endif // NODE_CONTROLLER_H
