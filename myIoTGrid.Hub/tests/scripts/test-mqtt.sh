#!/bin/bash

# ===========================================
# myIoTGrid Hub - MQTT Test Script
# ===========================================
# Dieses Script testet den MQTT-Datenfluss:
# Sensor -> MQTT Broker -> Hub API -> SignalR -> Browser
#
# Voraussetzungen:
# - mosquitto_pub installiert (mosquitto-clients)
# - MQTT Broker läuft auf localhost:1883
# - Hub API läuft auf localhost:5000
# ===========================================

# Konfiguration
MQTT_HOST="${MQTT_HOST:-localhost}"
MQTT_PORT="${MQTT_PORT:-1883}"
TENANT_ID="${TENANT_ID:-00000000-0000-0000-0000-000000000001}"

# Farben für die Ausgabe
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=========================================${NC}"
echo -e "${BLUE}  myIoTGrid Hub - MQTT Test${NC}"
echo -e "${BLUE}=========================================${NC}"
echo ""

# Prüfe ob mosquitto_pub verfügbar ist
if ! command -v mosquitto_pub &> /dev/null; then
    echo -e "${RED}Fehler: mosquitto_pub nicht gefunden!${NC}"
    echo "Bitte installieren Sie mosquitto-clients:"
    echo "  macOS: brew install mosquitto"
    echo "  Ubuntu: sudo apt install mosquitto-clients"
    exit 1
fi

# Funktion zum Senden einer Sensor-Nachricht
send_sensor_data() {
    local hub_id=$1
    local sensor_type=$2
    local value=$3

    local topic="myiotgrid/${TENANT_ID}/sensordata"
    local payload="{\"hubId\":\"${hub_id}\",\"sensorType\":\"${sensor_type}\",\"value\":${value}}"

    echo -e "${YELLOW}Sende: ${sensor_type}=${value} von ${hub_id}${NC}"
    mosquitto_pub -h "${MQTT_HOST}" -p "${MQTT_PORT}" -t "${topic}" -m "${payload}"

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}  -> Erfolgreich gesendet${NC}"
    else
        echo -e "${RED}  -> Fehler beim Senden${NC}"
    fi
}

# Funktion zum Senden eines Hub-Status
send_hub_status() {
    local hub_id=$1
    local status=$2

    local topic="myiotgrid/${TENANT_ID}/hubs/${hub_id}/status"

    echo -e "${YELLOW}Sende Hub-Status: ${hub_id} -> ${status}${NC}"
    mosquitto_pub -h "${MQTT_HOST}" -p "${MQTT_PORT}" -t "${topic}" -m "${status}"

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}  -> Erfolgreich gesendet${NC}"
    else
        echo -e "${RED}  -> Fehler beim Senden${NC}"
    fi
}

# Test 1: Einzelne Sensordaten senden
echo ""
echo -e "${BLUE}=== Test 1: Einzelne Sensordaten ===${NC}"
send_sensor_data "test-sensor-wohnzimmer" "temperature" "21.5"
sleep 0.5
send_sensor_data "test-sensor-wohnzimmer" "humidity" "45.2"
sleep 0.5
send_sensor_data "test-sensor-wohnzimmer" "co2" "650"

# Test 2: Multiple Sensoren simulieren
echo ""
echo -e "${BLUE}=== Test 2: Multiple Sensoren ===${NC}"
send_sensor_data "test-sensor-schlafzimmer" "temperature" "19.8"
sleep 0.5
send_sensor_data "test-sensor-kueche" "temperature" "23.1"
sleep 0.5
send_sensor_data "test-sensor-garten" "soil_moisture" "35.5"

# Test 3: Hub-Status testen
echo ""
echo -e "${BLUE}=== Test 3: Hub-Status ===${NC}"
send_hub_status "test-sensor-wohnzimmer" "online"
sleep 0.5
send_hub_status "test-sensor-garten" "offline"
sleep 1
send_hub_status "test-sensor-garten" "online"

# Test 4: Burst-Test (viele Nachrichten schnell)
echo ""
echo -e "${BLUE}=== Test 4: Burst-Test (10 Nachrichten) ===${NC}"
for i in {1..10}; do
    temp=$(echo "scale=1; 20 + $i * 0.5" | bc)
    send_sensor_data "test-sensor-burst" "temperature" "${temp}"
    sleep 0.1
done

echo ""
echo -e "${BLUE}=========================================${NC}"
echo -e "${GREEN}Tests abgeschlossen!${NC}"
echo ""
echo "Prüfen Sie die Ergebnisse:"
echo "  - API Logs: docker-compose logs -f hub-api"
echo "  - Swagger: http://localhost:5000/swagger"
echo "  - GET /api/sensordata?sensorTypeCode=temperature"
echo ""
echo "SignalR Test im Browser:"
echo "  Öffnen Sie die Browser-Konsole und führen Sie aus:"
echo ""
echo -e "${YELLOW}const connection = new signalR.HubConnectionBuilder()"
echo "  .withUrl('http://localhost:5000/hubs/sensors')"
echo "  .build();"
echo "connection.on('NewSensorData', (data) => {"
echo "  console.log('Neue Sensordaten:', data);"
echo "});"
echo -e "await connection.start();${NC}"
echo ""
