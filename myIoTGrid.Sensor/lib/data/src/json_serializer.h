#ifndef JSON_SERIALIZER_H
#define JSON_SERIALIZER_H

#include "data_types.h"
#include <string>

namespace data {

/**
 * JSON Serialization utilities using ArduinoJson
 * Provides serialization/deserialization for all data types
 */
class JsonSerializer {
public:
    /**
     * Serialize a Reading to JSON string
     * @param reading Reading to serialize
     * @return JSON string
     */
    static std::string serializeReading(const Reading& reading);

    /**
     * Serialize a NodeInfo to JSON string (for registration)
     * @param info NodeInfo to serialize
     * @return JSON string
     */
    static std::string serializeNodeInfo(const NodeInfo& info);

    /**
     * Serialize a NodeConfig to JSON string
     * @param config NodeConfig to serialize
     * @return JSON string
     */
    static std::string serializeNodeConfig(const NodeConfig& config);

    /**
     * Deserialize JSON string to NodeConfig
     * @param json JSON string
     * @param config Output NodeConfig
     * @return true if successful
     */
    static bool deserializeNodeConfig(const std::string& json, NodeConfig& config);

    /**
     * Deserialize JSON string to Reading
     * @param json JSON string
     * @param reading Output Reading
     * @return true if successful
     */
    static bool deserializeReading(const std::string& json, Reading& reading);
};

} // namespace data

#endif // JSON_SERIALIZER_H
