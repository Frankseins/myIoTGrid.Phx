#include "http_connection.h"
#include "hal/hal.h"
#include "config.h"

namespace connection {

HttpConnection::HttpConnection(const std::string& endpoint)
    : endpoint_(endpoint)
    , connected_(false)
    , configCallback_(nullptr)
{
}

bool HttpConnection::connect() {
    // For HTTP, we just verify we can reach the endpoint
    hal::log_info("HttpConnection: Connecting to " + endpoint_);

    // Try a simple health check
    std::string healthUrl = buildUrl("/health");
    auto response = hal::http_get(healthUrl, config::HTTP_TIMEOUT_MS);

    if (response.success) {
        connected_ = true;
        hal::log_info("HttpConnection: Connected successfully");
        return true;
    }

    // Health endpoint might not exist, that's okay
    // Just mark as "connected" since HTTP is stateless
    connected_ = true;
    hal::log_info("HttpConnection: Ready (health check skipped)");
    return true;
}

bool HttpConnection::isConnected() const {
    return connected_;
}

void HttpConnection::disconnect() {
    connected_ = false;
    hal::log_info("HttpConnection: Disconnected");
}

data::NodeConfig HttpConnection::registerNode(const data::NodeInfo& info) {
    data::NodeConfig config;

    std::string url = buildUrl(config::API_REGISTER);
    std::string json = data::JsonSerializer::serializeNodeInfo(info);

    hal::log_info("HttpConnection: Registering node at " + url);
    hal::log_info("HttpConnection: Payload: " + json);

    auto response = postWithRetry(url, json, config::HTTP_RETRY_COUNT);

    if (!response.success) {
        hal::log_error("HttpConnection: Registration failed - " + response.errorMessage);
        return config; // Return invalid config
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
        hal::log_error("HttpConnection: Registration failed with status " +
                      std::to_string(response.statusCode));
        hal::log_error("HttpConnection: Response: " + response.body);
        return config;
    }

    // Parse response
    if (!data::JsonSerializer::deserializeNodeConfig(response.body, config)) {
        hal::log_error("HttpConnection: Failed to parse config response");
        hal::log_error("HttpConnection: Response body: " + response.body);
        return config;
    }

    hal::log_info("HttpConnection: Registration successful");
    hal::log_info("HttpConnection: Device ID: " + config.deviceId);
    hal::log_info("HttpConnection: Interval: " + std::to_string(config.intervalSeconds) + "s");

    // Notify callback if set
    if (configCallback_) {
        configCallback_(config);
    }

    return config;
}

ConnectionResult HttpConnection::sendReading(const data::Reading& reading) {
    std::string url = buildUrl(config::API_READINGS);
    std::string json = data::JsonSerializer::serializeReading(reading);

    hal::log_debug("HttpConnection: Sending reading to " + url);
    hal::log_debug("HttpConnection: Payload: " + json);

    auto response = postWithRetry(url, json, config::HTTP_RETRY_COUNT);

    if (!response.success) {
        return ConnectionResult::error(response.errorMessage, response.statusCode);
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
        return ConnectionResult::error(
            "HTTP " + std::to_string(response.statusCode) + ": " + response.body,
            response.statusCode
        );
    }

    hal::log_info("HttpConnection: Reading sent successfully (" +
                 reading.type + " = " + std::to_string(reading.value) + " " + reading.unit + ")");

    return ConnectionResult::ok();
}

void HttpConnection::onConfigReceived(ConfigCallback callback) {
    configCallback_ = callback;
}

std::string HttpConnection::getMode() const {
    return "http";
}

void HttpConnection::setEndpoint(const std::string& endpoint) {
    endpoint_ = endpoint;
}

std::string HttpConnection::getEndpoint() const {
    return endpoint_;
}

std::string HttpConnection::buildUrl(const std::string& path) const {
    std::string url = endpoint_;

    // Remove trailing slash from endpoint
    if (!url.empty() && url.back() == '/') {
        url.pop_back();
    }

    // Ensure path starts with /
    if (path.empty() || path[0] != '/') {
        url += '/';
    }

    url += path;
    return url;
}

hal::HttpResponse HttpConnection::postWithRetry(const std::string& url,
                                                const std::string& json,
                                                int retries) {
    hal::HttpResponse response;

    for (int attempt = 1; attempt <= retries; ++attempt) {
        response = hal::http_post(url, json, config::HTTP_TIMEOUT_MS);

        if (response.success) {
            return response;
        }

        if (attempt < retries) {
            hal::log_warn("HttpConnection: Attempt " + std::to_string(attempt) +
                         " failed, retrying in 1s...");
            hal::delay_ms(1000);
        }
    }

    hal::log_error("HttpConnection: All " + std::to_string(retries) + " attempts failed");
    return response;
}

} // namespace connection
