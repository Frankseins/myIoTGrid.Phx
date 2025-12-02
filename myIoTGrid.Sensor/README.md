# myIoTGrid Sensor Firmware

C++ Firmware for ESP32-based IoT sensors with native simulation support.

## Features

- **Multi-Platform**: Runs on ESP32 hardware and as native Linux application
- **Hardware Abstraction Layer (HAL)**: Platform-independent code
- **Simulated Sensors**: Realistic sensor value simulation for testing
- **REST API**: HTTP connection to Hub backend
- **Persistent Configuration**: Stores device config locally

## Project Structure

```
myIoTGrid.Sensor/
├── include/
│   ├── config.h          # Configuration constants
│   └── hal/hal.h         # HAL interface definition
├── src/
│   └── main.cpp          # Entry point
├── lib/
│   ├── hal_esp32/        # ESP32 HAL implementation
│   ├── hal_native/       # Native/Linux HAL implementation
│   ├── sensor/           # Sensor abstraction layer
│   ├── connection/       # Connection implementations (HTTP, MQTT, LoRa)
│   ├── controller/       # Main controller and config management
│   └── data/             # Data structures and JSON serialization
├── test/                 # Unit tests
├── docker/               # Docker build files
└── platformio.ini        # PlatformIO configuration
```

## Build Targets

### Native (Linux/Docker Simulator)

```bash
# Build
pio run -e native

# Run
.pio/build/native/program

# Run tests
pio test -e native_test
```

### ESP32 Hardware

```bash
# Build for ESP32 with real sensors
pio run -e esp32

# Build for ESP32 with simulated sensors
pio run -e esp32_simulate

# Upload to ESP32
pio run -e esp32 --target upload

# Monitor serial output
pio device monitor
```

## Docker

### Build Image

```bash
cd docker
docker build -t myiotgrid-sensor-sim -f Dockerfile ..
```

### Run Container

```bash
# Run with Hub on localhost
docker run -e HUB_HOST=host.docker.internal -e HUB_PORT=5000 myiotgrid-sensor-sim

# Run with docker-compose (development)
docker-compose -f docker-compose.dev.yml up
```

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `HUB_HOST` | localhost | Hub API hostname |
| `HUB_PORT` | 5000 | Hub API port |
| `WIFI_SSID` | - | WiFi network name (ESP32 only) |
| `WIFI_PASSWORD` | - | WiFi password (ESP32 only) |

### Sensor Types

The following sensor types are supported:

| Type | Unit | Description |
|------|------|-------------|
| `temperature` | °C | Temperature |
| `humidity` | % | Relative humidity |
| `pressure` | hPa | Barometric pressure |
| `co2` | ppm | CO2 concentration |
| `pm25` | µg/m³ | Particulate matter 2.5 |
| `pm10` | µg/m³ | Particulate matter 10 |
| `soil_moisture` | % | Soil moisture |
| `light` | lux | Light intensity |
| `uv` | index | UV index |
| `wind_speed` | m/s | Wind speed |
| `rainfall` | mm | Rainfall |
| `water_level` | cm | Water level |
| `battery` | % | Battery level |
| `rssi` | dBm | Signal strength |

## API Endpoints

The sensor communicates with these Hub API endpoints:

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/devices/register` | Register sensor node |
| POST | `/api/readings` | Send sensor reading |

### Registration Payload

```json
{
  "serialNumber": "SIM-A1B2C3D4-0001",
  "capabilities": ["temperature", "humidity", "pressure"],
  "firmwareVersion": "1.0.0",
  "hardwareType": "SIM"
}
```

### Reading Payload

```json
{
  "deviceId": "wetterstation-sim-01",
  "type": "temperature",
  "value": 21.5,
  "unit": "°C",
  "timestamp": 1733150400
}
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     NodeController                           │
│  - setup(): Initialize, register, create sensors             │
│  - loop(): Execute readings at interval                      │
└─────────────────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  ConfigManager  │  │   IConnection   │  │    ISensor      │
│                 │  │                 │  │                 │
│ - loadConfig()  │  │ - registerNode()│  │ - read()        │
│ - saveConfig()  │  │ - sendReading() │  │ - getType()     │
└─────────────────┘  └─────────────────┘  └─────────────────┘
                              │                    │
                     ┌────────┴────────┐    ┌──────┴──────┐
                     │ HttpConnection  │    │ Simulated   │
                     │ (REST API)      │    │ Sensor      │
                     └────────┬────────┘    └─────────────┘
                              │
                              ▼
                     ┌─────────────────┐
                     │   HAL Layer     │
                     │                 │
                     │ hal_native.cpp  │
                     │ hal_esp32.cpp   │
                     └─────────────────┘
```

## Development

### Adding a New Sensor Type

1. Add sensor info to `lib/sensor/src/sensor_interface.h`:

```cpp
constexpr SensorTypeInfo MY_SENSOR = {
    "my_sensor", "My Sensor", "unit",
    0.0f, 100.0f,           // min, max
    50.0f, 10.0f, 2.0f      // base, amplitude, noise
};
```

2. Add to lookup table in `lib/sensor/src/sensor_types.cpp`

3. Add to `getSupportedTypes()` in `lib/sensor/src/sensor_factory.cpp`

### Adding a Hardware Sensor (ESP32)

1. Create sensor class in `lib/sensor/src/` implementing `ISensor`
2. Add creation logic in `SensorFactory::create()` for ESP32 platform

## Testing

```bash
# Run all tests
pio test -e native_test

# Run specific test
pio test -e native_test -f test_simulation
```

## License

MIT License - see LICENSE file for details.
