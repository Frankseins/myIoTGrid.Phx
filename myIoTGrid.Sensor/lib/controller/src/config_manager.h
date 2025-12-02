#ifndef CONFIG_MANAGER_H
#define CONFIG_MANAGER_H

#include "data_types.h"
#include <string>

namespace controller {

/**
 * ConfigManager - Manages persistent configuration storage
 *
 * Handles:
 * - Loading/saving NodeConfig to persistent storage
 * - Validation of configuration
 * - Default configuration generation
 */
class ConfigManager {
public:
    ConfigManager();
    ~ConfigManager() = default;

    /**
     * Check if a saved configuration exists
     * @return true if config exists in storage
     */
    bool hasConfig() const;

    /**
     * Load configuration from persistent storage
     * @return Loaded NodeConfig (check isValid())
     */
    data::NodeConfig loadConfig();

    /**
     * Save configuration to persistent storage
     * @param config Configuration to save
     * @return true if saved successfully
     */
    bool saveConfig(const data::NodeConfig& config);

    /**
     * Delete saved configuration
     * @return true if deleted (or didn't exist)
     */
    bool deleteConfig();

    /**
     * Get the current in-memory configuration
     * @return Current config reference
     */
    const data::NodeConfig& getConfig() const;

    /**
     * Set the current in-memory configuration
     * Does NOT persist to storage (call saveConfig for that)
     * @param config New configuration
     */
    void setConfig(const data::NodeConfig& config);

    /**
     * Get the device's serial number
     * Generated or loaded from storage
     * @return Serial number string
     */
    std::string getSerialNumber() const;

    /**
     * Create default configuration for a new device
     * @return Default NodeConfig
     */
    static data::NodeConfig createDefaultConfig();

private:
    data::NodeConfig config_;
    mutable std::string serialNumber_;
};

} // namespace controller

#endif // CONFIG_MANAGER_H
