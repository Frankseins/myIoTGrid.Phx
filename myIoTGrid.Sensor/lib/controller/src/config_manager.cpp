#include "config_manager.h"
#include "json_serializer.h"
#include "hal/hal.h"
#include "config.h"

namespace controller {

ConfigManager::ConfigManager()
    : config_()
    , serialNumber_()
{
}

bool ConfigManager::hasConfig() const {
    return hal::storage_exists(config::STORAGE_KEY_CONFIG);
}

data::NodeConfig ConfigManager::loadConfig() {
    if (!hasConfig()) {
        hal::log_warn("ConfigManager: No saved configuration found");
        return data::NodeConfig();
    }

    std::string json = hal::storage_load(config::STORAGE_KEY_CONFIG);
    if (json.empty()) {
        hal::log_error("ConfigManager: Failed to load config from storage");
        return data::NodeConfig();
    }

    data::NodeConfig config;
    if (!data::JsonSerializer::deserializeNodeConfig(json, config)) {
        hal::log_error("ConfigManager: Failed to parse saved config");
        return data::NodeConfig();
    }

    hal::log_info("ConfigManager: Loaded config for device: " + config.deviceId);
    config_ = config;
    return config;
}

bool ConfigManager::saveConfig(const data::NodeConfig& config) {
    if (!config.isValid()) {
        hal::log_error("ConfigManager: Cannot save invalid config");
        return false;
    }

    std::string json = data::JsonSerializer::serializeNodeConfig(config);

    if (!hal::storage_save(config::STORAGE_KEY_CONFIG, json)) {
        hal::log_error("ConfigManager: Failed to save config to storage");
        return false;
    }

    config_ = config;
    hal::log_info("ConfigManager: Saved config for device: " + config.deviceId);
    return true;
}

bool ConfigManager::deleteConfig() {
    config_ = data::NodeConfig();
    return hal::storage_delete(config::STORAGE_KEY_CONFIG);
}

const data::NodeConfig& ConfigManager::getConfig() const {
    return config_;
}

void ConfigManager::setConfig(const data::NodeConfig& config) {
    config_ = config;
}

std::string ConfigManager::getSerialNumber() const {
    if (serialNumber_.empty()) {
        serialNumber_ = hal::get_device_serial();
    }
    return serialNumber_;
}

data::NodeConfig ConfigManager::createDefaultConfig() {
    data::NodeConfig config;

    config.deviceId = "";  // Will be assigned by Hub
    config.name = "New Sensor";
    config.location = "Unknown";
    config.intervalSeconds = config::DEFAULT_INTERVAL_SECONDS;

    // Default sensors (all enabled for simulation)
    config.sensors.push_back(data::SensorConfig("temperature", true, -1));
    config.sensors.push_back(data::SensorConfig("humidity", true, -1));
    config.sensors.push_back(data::SensorConfig("pressure", true, -1));

    // Default HTTP connection
    config.connection.mode = "http";
    config.connection.endpoint = std::string(config::DEFAULT_HUB_PROTOCOL) + "://" +
                                  config::DEFAULT_HUB_HOST + ":" +
                                  std::to_string(config::DEFAULT_HUB_PORT);

    return config;
}

} // namespace controller
