#include "node_controller.h"
#include "http_connection.h"
#include "hal/hal.h"
#include "config.h"

namespace controller {

NodeController::NodeController()
    : configManager_()
    , connection_(nullptr)
    , sensors_()
    , running_(false)
    , lastReadTime_(0)
    , readingCount_(0)
{
}

bool NodeController::setup() {
    hal::log_info("===========================================");
    hal::log_info("  myIoTGrid Sensor - Starting...");
    hal::log_info("===========================================");
    hal::log_info("Firmware Version: " FIRMWARE_VERSION);
    hal::log_info("Hardware Type: " HARDWARE_TYPE);
    hal::log_info("Simulation Mode: " + std::string(SIMULATE_SENSORS ? "ON" : "OFF"));
    hal::log_info("-------------------------------------------");

    // Initialize HAL
    hal::init();

    // Get serial number
    std::string serial = configManager_.getSerialNumber();
    hal::log_info("Serial Number: " + serial);

    // Initialize network
    if (!initNetwork()) {
        hal::log_error("Failed to initialize network");
        return false;
    }

    // Build endpoint from environment/defaults
    std::string endpoint = buildHubEndpoint();
    hal::log_info("Hub Endpoint: " + endpoint);

    // Check for existing config
    data::NodeConfig config;
    if (configManager_.hasConfig()) {
        hal::log_info("Loading saved configuration...");
        config = configManager_.loadConfig();

        if (config.isValid()) {
            hal::log_info("Loaded config for device: " + config.deviceId);
        } else {
            hal::log_warn("Saved config invalid, will re-register");
        }
    }

    // If no valid config, register with Hub
    if (!config.isValid()) {
        // Create temporary connection for registration
        connection::ConnectionConfig connConfig;
        connConfig.mode = "http";
        connConfig.endpoint = endpoint;
        connection_ = createConnection(connConfig);

        if (!registerWithHub()) {
            hal::log_error("Failed to register with Hub");
            hal::log_info("Will retry in " +
                         std::to_string(config::REGISTRATION_RETRY_DELAY_MS / 1000) + " seconds...");
            return false;
        }

        config = configManager_.getConfig();
    }

    // Create connection based on config
    if (!connection_ || connection_->getEndpoint() != config.connection.endpoint) {
        connection_ = createConnection(config.connection);
    }

    // Initialize sensors
    initSensors();

    running_ = true;
    lastReadTime_ = 0; // Force immediate first reading

    hal::log_info("-------------------------------------------");
    hal::log_info("Setup complete. Starting measurement loop.");
    hal::log_info("Interval: " + std::to_string(config.intervalSeconds) + " seconds");
    hal::log_info("===========================================");

    return true;
}

void NodeController::loop() {
    if (!running_) {
        hal::delay_ms(1000);
        return;
    }

    uint32_t now = hal::millis();
    const data::NodeConfig& config = configManager_.getConfig();
    uint32_t intervalMs = config.intervalSeconds * 1000;

    // Check if interval has passed
    if (now - lastReadTime_ >= intervalMs) {
        executeReadingCycle();
        lastReadTime_ = now;
    }

    // Small delay to prevent busy-waiting
    hal::delay_ms(100);
}

bool NodeController::isRunning() const {
    return running_;
}

const data::NodeConfig& NodeController::getConfig() const {
    return configManager_.getConfig();
}

bool NodeController::reregister() {
    hal::log_info("Re-registration requested");
    configManager_.deleteConfig();
    running_ = false;
    sensors_.clear();
    return setup();
}

bool NodeController::initNetwork() {
#ifdef PLATFORM_ESP32
    // ESP32: Connect to WiFi
    std::string ssid = hal::get_env(config::ENV_WIFI_SSID, config::DEFAULT_WIFI_SSID);
    std::string password = hal::get_env(config::ENV_WIFI_PASSWORD, config::DEFAULT_WIFI_PASSWORD);

    if (ssid.empty()) {
        hal::log_error("WiFi SSID not configured");
        return false;
    }

    hal::log_info("Connecting to WiFi: " + ssid);

    if (!hal::network_connect(ssid, password)) {
        hal::log_error("Failed to connect to WiFi");
        return false;
    }

    hal::log_info("WiFi connected. IP: " + hal::network_get_ip());
    return true;
#else
    // Native: Network always available
    return hal::network_is_connected();
#endif
}

std::string NodeController::buildHubEndpoint() {
    std::string host = hal::get_env(config::ENV_HUB_HOST, config::DEFAULT_HUB_HOST);
    std::string port = hal::get_env(config::ENV_HUB_PORT, std::to_string(config::DEFAULT_HUB_PORT));

    return std::string(config::DEFAULT_HUB_PROTOCOL) + "://" + host + ":" + port;
}

bool NodeController::registerWithHub() {
    hal::log_info("Registering with Hub...");

    if (!connection_) {
        hal::log_error("No connection available for registration");
        return false;
    }

    // Build node info
    data::NodeInfo info = buildNodeInfo();

    // Try to register
    for (int attempt = 1; attempt <= config::HTTP_RETRY_COUNT; ++attempt) {
        hal::log_info("Registration attempt " + std::to_string(attempt) + "...");

        data::NodeConfig config = connection_->registerNode(info);

        if (config.isValid()) {
            // Save config
            configManager_.saveConfig(config);
            hal::log_info("Registration successful!");
            return true;
        }

        if (attempt < config::HTTP_RETRY_COUNT) {
            hal::log_warn("Registration failed, retrying in " +
                         std::to_string(config::REGISTRATION_RETRY_DELAY_MS / 1000) + "s...");
            hal::delay_ms(config::REGISTRATION_RETRY_DELAY_MS);
        }
    }

    return false;
}

std::unique_ptr<connection::IConnection> NodeController::createConnection(
    const data::ConnectionConfig& connConfig
) {
    if (connConfig.mode == "http") {
        return std::make_unique<connection::HttpConnection>(connConfig.endpoint);
    }

    // TODO: Add MQTT and LoRaWAN in future sprints
    hal::log_warn("Unknown connection mode: " + connConfig.mode + ", using HTTP");
    return std::make_unique<connection::HttpConnection>(connConfig.endpoint);
}

void NodeController::initSensors() {
    sensors_.clear();

    const data::NodeConfig& config = configManager_.getConfig();

    hal::log_info("Initializing sensors...");

    for (const auto& sensorConfig : config.sensors) {
        if (!sensorConfig.enabled) {
            hal::log_info("  Sensor " + sensorConfig.type + ": DISABLED");
            continue;
        }

        auto sensor = sensor::SensorFactory::create(
            sensorConfig.type,
            sensorConfig.pin,
            SIMULATE_SENSORS
        );

        if (!sensor) {
            hal::log_error("  Sensor " + sensorConfig.type + ": FAILED (unknown type)");
            continue;
        }

        if (!sensor->begin()) {
            hal::log_error("  Sensor " + sensorConfig.type + ": FAILED (init error)");
            continue;
        }

        hal::log_info("  Sensor " + sensorConfig.type + ": OK (" + sensor->getName() + ")");
        sensors_[sensorConfig.type] = std::move(sensor);
    }

    hal::log_info("Initialized " + std::to_string(sensors_.size()) + " sensors");
}

data::NodeInfo NodeController::buildNodeInfo() {
    data::NodeInfo info;

    info.serialNumber = configManager_.getSerialNumber();
    info.firmwareVersion = FIRMWARE_VERSION;
    info.hardwareType = HARDWARE_TYPE;

    // Get supported capabilities (all supported sensor types)
    info.capabilities = sensor::SensorFactory::getSupportedTypes();

    return info;
}

void NodeController::executeReadingCycle() {
    readingCount_++;
    hal::log_info("--- Reading cycle #" + std::to_string(readingCount_) + " ---");

    if (sensors_.empty()) {
        hal::log_warn("No sensors configured");
        return;
    }

    for (auto& [type, sensor] : sensors_) {
        if (sensor && sensor->isReady()) {
            sendSensorReading(type, sensor.get());
        }
    }
}

void NodeController::sendSensorReading(const std::string& type, sensor::ISensor* sensor) {
    float value = sensor->read();

    if (std::isnan(value)) {
        hal::log_error("Sensor " + type + " returned NaN");
        return;
    }

    data::Reading reading;
    reading.deviceId = configManager_.getConfig().deviceId;
    reading.type = type;
    reading.value = value;
    reading.unit = sensor->getUnit();
    reading.timestamp = hal::timestamp();

    hal::log_info("  " + type + ": " + std::to_string(value) + " " + reading.unit);

    if (connection_) {
        auto result = connection_->sendReading(reading);
        if (!result.success) {
            hal::log_error("Failed to send reading: " + result.errorMessage);
        }
    }
}

} // namespace controller
