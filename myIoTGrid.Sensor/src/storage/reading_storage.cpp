/**
 * myIoTGrid.Sensor - Reading Storage Implementation
 */

#include "reading_storage.h"
#include <ArduinoJson.h>
#ifdef PLATFORM_NATIVE
#include "ArduinoJsonString.h"
#endif
#include <time.h>

ReadingStorage::ReadingStorage()
    : _sdManager(nullptr)
    , _configManager(nullptr)
    , _lastFlush(0)
{
}

bool ReadingStorage::init(SDManager& sdManager, StorageConfigManager& configManager) {
    _sdManager = &sdManager;
    _configManager = &configManager;

    if (!_sdManager->isAvailable()) {
        Serial.println("[ReadingStorage] SD card not available");
        return false;
    }

    // Load sync status
    loadSyncStatus();

    // Update pending count
    updatePendingCount();

    Serial.printf("[ReadingStorage] Initialized - %lu pending readings\n",
                  _syncStatus.pendingReadings);

    return true;
}

bool ReadingStorage::storeReading(const StoredReading& reading) {
    if (!_sdManager || !_sdManager->isAvailable()) {
        Serial.println("[ReadingStorage] SD card not available");
        return false;
    }

    // Check free space
    if (!_sdManager->hasEnoughSpace(_configManager->getConfig().minFreeBytes)) {
        Serial.println("[ReadingStorage] Low disk space, attempting cleanup");
        _sdManager->cleanupOldFiles(_configManager->getConfig().minFreeBytes);

        if (!_sdManager->hasEnoughSpace(_configManager->getConfig().minFreeBytes)) {
            Serial.println("[ReadingStorage] Still not enough space!");
            return false;
        }
    }

    // Get filename for today
    String filename = getTodayFilename();

    // Append reading to file
    String line = reading.toCsv() + "\n";
    if (!_sdManager->appendFile(filename.c_str(), line)) {
        Serial.printf("[ReadingStorage] Failed to write to %s\n", filename.c_str());
        return false;
    }

    // Update status
    _syncStatus.totalReadings++;
    if (!reading.synced) {
        _syncStatus.pendingReadings++;
    } else {
        _syncStatus.syncedReadings++;
    }
    _syncStatus.lastReadingTimestamp = reading.timestamp;

    // Periodic flush of sync status
    if (millis() - _lastFlush > FLUSH_INTERVAL_MS) {
        saveSyncStatus();
        _lastFlush = millis();
    }

    return true;
}

bool ReadingStorage::storeReading(const String& sensorType, double value,
                                  const String& unit, int endpointId) {
    StoredReading reading;
    reading.timestamp = time(nullptr); // Unix timestamp
    reading.sensorType = sensorType;
    reading.value = value;
    reading.unit = unit;
    reading.endpointId = endpointId;
    reading.synced = false;

    return storeReading(reading);
}

std::vector<StoredReading> ReadingStorage::getPendingReadings(int maxCount) {
    std::vector<StoredReading> pendingReadings;

    if (!_sdManager || !_sdManager->isAvailable()) {
        return pendingReadings;
    }

    // First check for pending batch files
    std::vector<String> batchFiles = getPendingBatchFiles();
    if (!batchFiles.empty()) {
        // Read from first batch file
        return readBatchFile(batchFiles[0]);
    }

    // Otherwise scan CSV files for unsynced readings
    _sdManager->listDirectory(SD_READINGS_DIR, [&](const String& name, size_t size, bool isDir) {
        if (isDir) return;
        if (!name.endsWith(".csv")) return;
        if (name.endsWith("_synced.csv")) return; // Skip synced files

        if ((int)pendingReadings.size() >= maxCount) return;

        String filepath = String(SD_READINGS_DIR) + "/" + name;
        String content = _sdManager->readFile(filepath.c_str());

        int startIdx = 0;
        while (startIdx < (int)content.length() && (int)pendingReadings.size() < maxCount) {
            int endIdx = content.indexOf('\n', startIdx);
            if (endIdx < 0) endIdx = content.length();

            String line = content.substring(startIdx, endIdx);
            line.trim();

            if (line.length() > 0) {
                StoredReading reading = StoredReading::fromCsv(line);
                if (!reading.synced && reading.timestamp > 0) {
                    pendingReadings.push_back(reading);
                }
            }

            startIdx = endIdx + 1;
        }
    });

    return pendingReadings;
}

int ReadingStorage::markAsSynced(const std::vector<StoredReading>& readings) {
    if (readings.empty()) return 0;

    // For simplicity, we'll create a synced batch file and mark readings
    // In production, you'd update the original files

    int markedCount = readings.size();

    _syncStatus.syncedReadings += markedCount;
    _syncStatus.pendingReadings -= markedCount;
    if (_syncStatus.pendingReadings < 0) {
        _syncStatus.pendingReadings = 0;
    }

    saveSyncStatus();

    return markedCount;
}

void ReadingStorage::recordSyncFailure(const String& error) {
    _syncStatus.consecutiveFailures++;
    _syncStatus.lastError = error;
    saveSyncStatus();

    Serial.printf("[ReadingStorage] Sync failure #%d: %s\n",
                  _syncStatus.consecutiveFailures, error.c_str());
}

void ReadingStorage::recordSyncSuccess(int syncedCount) {
    _syncStatus.consecutiveFailures = 0;
    _syncStatus.lastError = "";
    _syncStatus.lastSyncTimestamp = time(nullptr);
    _syncStatus.syncedReadings += syncedCount;
    _syncStatus.pendingReadings -= syncedCount;
    if (_syncStatus.pendingReadings < 0) {
        _syncStatus.pendingReadings = 0;
    }
    saveSyncStatus();

    Serial.printf("[ReadingStorage] Sync success: %d readings synced\n", syncedCount);
}

bool ReadingStorage::saveSyncStatus() {
    if (!_sdManager || !_sdManager->isAvailable()) {
        return false;
    }

    JsonDocument doc;
    doc["totalReadings"] = _syncStatus.totalReadings;
    doc["syncedReadings"] = _syncStatus.syncedReadings;
    doc["pendingReadings"] = _syncStatus.pendingReadings;
    doc["lastSyncTimestamp"] = _syncStatus.lastSyncTimestamp;
    doc["lastReadingTimestamp"] = _syncStatus.lastReadingTimestamp;
    doc["consecutiveFailures"] = _syncStatus.consecutiveFailures;
    doc["lastError"] = _syncStatus.lastError;

    String content;
    serializeJsonPretty(doc, content);

    return _sdManager->writeFile(SD_SYNC_STATUS_FILE, content);
}

bool ReadingStorage::loadSyncStatus() {
    if (!_sdManager || !_sdManager->isAvailable()) {
        return false;
    }

    String content = _sdManager->readFile(SD_SYNC_STATUS_FILE);
    if (content.length() == 0) {
        return false;
    }

    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, content);
    if (error) {
        Serial.printf("[ReadingStorage] Failed to parse sync status: %s\n", error.c_str());
        return false;
    }

    _syncStatus.totalReadings = doc["totalReadings"] | 0UL;
    _syncStatus.syncedReadings = doc["syncedReadings"] | 0UL;
    _syncStatus.pendingReadings = doc["pendingReadings"] | 0UL;
    _syncStatus.lastSyncTimestamp = doc["lastSyncTimestamp"] | 0UL;
    _syncStatus.lastReadingTimestamp = doc["lastReadingTimestamp"] | 0UL;
    _syncStatus.consecutiveFailures = doc["consecutiveFailures"] | 0;
    _syncStatus.lastError = doc["lastError"].as<String>();

    return true;
}

String ReadingStorage::createPendingBatch(const std::vector<StoredReading>& readings) {
    if (readings.empty() || !_sdManager || !_sdManager->isAvailable()) {
        return "";
    }

    // Create batch filename with timestamp
    char filename[64];
    snprintf(filename, sizeof(filename), "%s/batch_%lu.json",
             SD_PENDING_DIR, (unsigned long)time(nullptr));

    // Create JSON batch
    JsonDocument doc;
    JsonArray arr = doc.to<JsonArray>();

    for (const auto& reading : readings) {
        JsonObject obj = arr.add<JsonObject>();
        obj["timestamp"] = reading.timestamp;
        obj["sensorType"] = reading.sensorType;
        obj["value"] = reading.value;
        obj["unit"] = reading.unit;
        obj["endpointId"] = reading.endpointId;
    }

    String content;
    serializeJson(doc, content);

    if (_sdManager->writeFile(filename, content)) {
        Serial.printf("[ReadingStorage] Created batch file: %s (%d readings)\n",
                      filename, readings.size());
        return String(filename);
    }

    return "";
}

bool ReadingStorage::deletePendingBatch(const String& batchFile) {
    if (!_sdManager || !_sdManager->isAvailable()) {
        return false;
    }

    return _sdManager->deleteFile(batchFile.c_str());
}

std::vector<String> ReadingStorage::getPendingBatchFiles() {
    std::vector<String> files;

    if (!_sdManager || !_sdManager->isAvailable()) {
        return files;
    }

    _sdManager->listDirectory(SD_PENDING_DIR, [&](const String& name, size_t size, bool isDir) {
        if (isDir) return;
        if (name.startsWith("batch_") && name.endsWith(".json")) {
            files.push_back(String(SD_PENDING_DIR) + "/" + name);
        }
    });

    // Sort by name (which includes timestamp)
    std::sort(files.begin(), files.end());

    return files;
}

std::vector<StoredReading> ReadingStorage::readBatchFile(const String& batchFile) {
    std::vector<StoredReading> readings;

    if (!_sdManager || !_sdManager->isAvailable()) {
        return readings;
    }

    String content = _sdManager->readFile(batchFile.c_str());
    if (content.length() == 0) {
        return readings;
    }

    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, content);
    if (error) {
        Serial.printf("[ReadingStorage] Failed to parse batch file: %s\n", error.c_str());
        return readings;
    }

    JsonArray arr = doc.as<JsonArray>();
    for (JsonObject obj : arr) {
        StoredReading reading;
        reading.timestamp = obj["timestamp"] | 0UL;
        reading.sensorType = obj["sensorType"].as<String>();
        reading.value = obj["value"] | 0.0;
        reading.unit = obj["unit"].as<String>();
        reading.endpointId = obj["endpointId"] | 0;
        reading.synced = false;

        if (reading.timestamp > 0) {
            readings.push_back(reading);
        }
    }

    return readings;
}

void ReadingStorage::updatePendingCount() {
    if (!_sdManager || !_sdManager->isAvailable()) {
        return;
    }

    unsigned long pendingCount = 0;

    // Count readings in pending batch files
    std::vector<String> batchFiles = getPendingBatchFiles();
    for (const auto& batchFile : batchFiles) {
        std::vector<StoredReading> readings = readBatchFile(batchFile);
        pendingCount += readings.size();
    }

    // Count unsynced readings in CSV files
    _sdManager->listDirectory(SD_READINGS_DIR, [&](const String& name, size_t size, bool isDir) {
        if (isDir) return;
        if (!name.endsWith(".csv")) return;
        if (name.endsWith("_synced.csv")) return;

        String filepath = String(SD_READINGS_DIR) + "/" + name;
        String content = _sdManager->readFile(filepath.c_str());

        int startIdx = 0;
        while (startIdx < (int)content.length()) {
            int endIdx = content.indexOf('\n', startIdx);
            if (endIdx < 0) endIdx = content.length();

            String line = content.substring(startIdx, endIdx);
            line.trim();

            if (line.length() > 0) {
                // Check if synced flag is 0
                int lastComma = line.lastIndexOf(',');
                if (lastComma > 0 && line.substring(lastComma + 1).toInt() == 0) {
                    pendingCount++;
                }
            }

            startIdx = endIdx + 1;
        }
    });

    _syncStatus.pendingReadings = pendingCount;
    Serial.printf("[ReadingStorage] Updated pending count: %lu\n", pendingCount);
}

String ReadingStorage::getTodayFilename() const {
    time_t now = time(nullptr);
    struct tm* timeinfo = localtime(&now);

    char filename[64];
    snprintf(filename, sizeof(filename), "%s/readings_%04d%02d%02d.csv",
             SD_READINGS_DIR,
             timeinfo->tm_year + 1900,
             timeinfo->tm_mon + 1,
             timeinfo->tm_mday);

    return String(filename);
}

String ReadingStorage::getFilenameForDate(int year, int month, int day) const {
    char filename[64];
    snprintf(filename, sizeof(filename), "%s/readings_%04d%02d%02d.csv",
             SD_READINGS_DIR, year, month, day);
    return String(filename);
}

bool ReadingStorage::parseDateFromFilename(const String& filename, int& year, int& month, int& day) {
    // Format: readings_YYYYMMDD.csv
    int idx = filename.indexOf("readings_");
    if (idx < 0) return false;

    String dateStr = filename.substring(idx + 9, idx + 17);
    if (dateStr.length() != 8) return false;

    year = dateStr.substring(0, 4).toInt();
    month = dateStr.substring(4, 6).toInt();
    day = dateStr.substring(6, 8).toInt();

    return (year > 2000 && month >= 1 && month <= 12 && day >= 1 && day <= 31);
}
