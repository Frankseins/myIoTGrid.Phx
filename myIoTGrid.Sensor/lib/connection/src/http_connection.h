#ifndef HTTP_CONNECTION_H
#define HTTP_CONNECTION_H

#include "connection_interface.h"
#include "json_serializer.h"
#include "hal/hal.h"
#include <string>

namespace connection {

/**
 * HTTP-based connection to Hub API
 *
 * Uses REST API endpoints:
 * - POST /api/devices/register - Register node
 * - POST /api/readings - Send sensor reading
 */
class HttpConnection : public IConnection {
public:
    /**
     * Create HTTP connection
     * @param endpoint Base URL of Hub API (e.g., "http://localhost:5000")
     */
    explicit HttpConnection(const std::string& endpoint);

    ~HttpConnection() override = default;

    // IConnection interface
    bool connect() override;
    bool isConnected() const override;
    void disconnect() override;
    data::NodeConfig registerNode(const data::NodeInfo& info) override;
    ConnectionResult sendReading(const data::Reading& reading) override;
    void onConfigReceived(ConfigCallback callback) override;
    std::string getMode() const override;

    /**
     * Set the base endpoint URL
     * @param endpoint Base URL (e.g., "http://localhost:5000")
     */
    void setEndpoint(const std::string& endpoint);

    /**
     * Get the current endpoint URL
     */
    std::string getEndpoint() const;

private:
    std::string endpoint_;
    bool connected_;
    ConfigCallback configCallback_;

    /**
     * Build full URL for an API path
     * @param path API path (e.g., "/api/readings")
     * @return Full URL
     */
    std::string buildUrl(const std::string& path) const;

    /**
     * Send HTTP POST with retry logic
     * @param url Full URL
     * @param json JSON body
     * @param retries Number of retries
     * @return HTTP response
     */
    hal::HttpResponse postWithRetry(const std::string& url,
                                    const std::string& json,
                                    int retries = 3);
};

} // namespace connection

#endif // HTTP_CONNECTION_H
