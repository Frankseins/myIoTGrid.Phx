#!/usr/bin/env node
/**
 * Rhine River Simulation Data Generator
 * Generates GPS readings along the Rhine from Erft mouth (Neuss) to Dutch border
 * For Node "Rhein-01" with sensor readings for December 17, 2025
 *
 * Real GPS coordinates from OpenStreetMap
 * Real water levels from official German gauges (17.12.2025)
 */

const https = require('https');
const http = require('http');

// Configuration
const API_BASE_URL = process.env.API_URL || 'https://phx.myiotgrid.cloud';
const NODE_ID = 'Rhein-02'; // Node identifier

// Rhine River Route (Erft mouth to Duisburg)
// Real GPS coordinates from OpenStreetMap
const ROUTE = {
  start: { lat: 51.186173, lon: 6.733080, name: 'Erftmündung/Neuss' },
  end: { lat: 51.430000, lon: 6.735000, name: 'Duisburg' }
};

// Simulation parameters
const INTERVAL_SECONDS = 60;
const SPEED_KMH = 12; // Motorboat speed on Rhine
const START_TIME = new Date('2025-12-17T08:00:00Z');

// Current Rhine conditions (17.12.2025) from official gauges
const RHINE_CONDITIONS = {
  waterTemperature: 7,        // °C (measured 15.12.2025)
  airTemperature: 4,          // °C
  humidity: 85,               // %
  pressure: 1015,             // hPa
  illuminance: 600,           // lux (winter, cloudy)
  // Water levels at different stations (cm)
  waterLevels: {
    duesseldorf: 247,
    duisburg: 389,
    wesel: 342,
    rees: 285,
    emmerich: 233
  }
};

// Waypoints along the Rhine - REAL coordinates from OpenStreetMap
// From Erft mouth (Neuss) to Duisburg
const WAYPOINTS = [
  // Neuss / Erftmündung
  { lat: 51.186173, lon: 6.733080, name: 'Erftmündung Neuss', waterLevel: 250 },
  { lat: 51.192756, lon: 6.730440, name: 'Neuss Hafen', waterLevel: 250 },
  { lat: 51.205000, lon: 6.745000, name: 'Düsseldorf-Hamm', waterLevel: 247 },

  // Düsseldorf
  { lat: 51.220000, lon: 6.765000, name: 'Düsseldorf Altstadt', waterLevel: 247 },
  { lat: 51.235000, lon: 6.770000, name: 'Düsseldorf-Oberkassel', waterLevel: 247 },
  { lat: 51.249974, lon: 6.753618, name: 'Düsseldorf-Lörick', waterLevel: 250 },
  { lat: 51.262748, lon: 6.709264, name: 'Düsseldorf-Kaiserswerth', waterLevel: 260 },

  // Duisburg
  { lat: 51.298462, lon: 6.727718, name: 'Duisburg-Süd', waterLevel: 320 },
  { lat: 51.333625, lon: 6.714873, name: 'Duisburg Hafen', waterLevel: 360 },
  { lat: 51.380000, lon: 6.720000, name: 'Duisburg-Ruhrort', waterLevel: 389 },
  { lat: 51.430000, lon: 6.735000, name: 'Duisburg-Walsum', waterLevel: 380 }
];

/**
 * Calculate distance between two GPS points (Haversine formula)
 */
function haversineDistance(lat1, lon1, lat2, lon2) {
  const R = 6371; // Earth's radius in km
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLon = (lon2 - lon1) * Math.PI / 180;
  const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
    Math.sin(dLon / 2) * Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

/**
 * Interpolate between waypoints to get GPS points
 */
function generateGpsPoints() {
  const points = [];
  let totalDistance = 0;

  // Calculate total distance
  for (let i = 0; i < WAYPOINTS.length - 1; i++) {
    totalDistance += haversineDistance(
      WAYPOINTS[i].lat, WAYPOINTS[i].lon,
      WAYPOINTS[i + 1].lat, WAYPOINTS[i + 1].lon
    );
  }

  console.log(`Total route distance: ${totalDistance.toFixed(2)} km`);

  // Calculate total time and number of points
  const totalTimeHours = totalDistance / SPEED_KMH;
  const totalTimeSeconds = totalTimeHours * 3600;
  const numPoints = Math.floor(totalTimeSeconds / INTERVAL_SECONDS);

  console.log(`Total time: ${totalTimeHours.toFixed(2)} hours`);
  console.log(`Number of points: ${numPoints}`);

  // Generate points by interpolating between waypoints
  const distancePerPoint = totalDistance / numPoints;

  for (let i = 0; i <= numPoints; i++) {
    const targetDistance = i * distancePerPoint;

    // Find which segment we're on
    let segmentStart = 0;
    let segmentEnd = 0;
    let accumulatedDistance = 0;

    for (let j = 0; j < WAYPOINTS.length - 1; j++) {
      const segmentDistance = haversineDistance(
        WAYPOINTS[j].lat, WAYPOINTS[j].lon,
        WAYPOINTS[j + 1].lat, WAYPOINTS[j + 1].lon
      );

      if (accumulatedDistance + segmentDistance >= targetDistance) {
        segmentStart = j;
        segmentEnd = j + 1;
        break;
      }
      accumulatedDistance += segmentDistance;
    }

    // Interpolate within segment
    const segmentDistance = haversineDistance(
      WAYPOINTS[segmentStart].lat, WAYPOINTS[segmentStart].lon,
      WAYPOINTS[segmentEnd].lat, WAYPOINTS[segmentEnd].lon
    );
    const distanceIntoSegment = targetDistance - accumulatedDistance;
    const t = Math.min(1, Math.max(0, distanceIntoSegment / segmentDistance));

    const lat = WAYPOINTS[segmentStart].lat + t * (WAYPOINTS[segmentEnd].lat - WAYPOINTS[segmentStart].lat);
    const lon = WAYPOINTS[segmentStart].lon + t * (WAYPOINTS[segmentEnd].lon - WAYPOINTS[segmentStart].lon);

    // Interpolate water level between waypoints
    const waterLevel = WAYPOINTS[segmentStart].waterLevel + t * (WAYPOINTS[segmentEnd].waterLevel - WAYPOINTS[segmentStart].waterLevel);

    // Add GPS noise (realistic ~5m accuracy on water)
    const latNoise = (Math.random() - 0.5) * 0.0001; // ~10m variation
    const lonNoise = (Math.random() - 0.5) * 0.0001;

    const timestamp = new Date(START_TIME.getTime() + i * INTERVAL_SECONDS * 1000);

    points.push({
      lat: parseFloat((lat + latNoise).toFixed(6)),
      lon: parseFloat((lon + lonNoise).toFixed(6)),
      timestamp: timestamp.toISOString(),
      progress: i / numPoints,
      waterLevel: Math.round(waterLevel)
    });
  }

  return points;
}

/**
 * Generate realistic sensor values for Rhine
 */
function generateSensorValues(progress, timestamp, waterLevel) {
  const hour = new Date(timestamp).getUTCHours();

  // Temperature variation (colder in morning, slightly warmer midday)
  const tempVariation = Math.sin((hour - 6) * Math.PI / 12) * 1.5;
  const temperature = RHINE_CONDITIONS.airTemperature + tempVariation + (Math.random() - 0.5) * 0.5;

  // Water temperature is very stable in large rivers
  const waterTemperature = RHINE_CONDITIONS.waterTemperature + (Math.random() - 0.5) * 0.2;

  // Humidity (higher near water, especially in morning)
  const humidityVariation = -Math.sin((hour - 6) * Math.PI / 12) * 3;
  const humidity = RHINE_CONDITIONS.humidity + humidityVariation + (Math.random() - 0.5) * 2;

  // Pressure (slight random walk)
  const pressure = RHINE_CONDITIONS.pressure + (Math.random() - 0.5) * 2;

  // Illuminance (daylight curve, winter in Germany ~8am-4pm)
  let illuminance = 0;
  if (hour >= 8 && hour <= 16) {
    const dayProgress = (hour - 8) / 8;
    illuminance = Math.sin(dayProgress * Math.PI) * 1500 + 100;
    illuminance += (Math.random() - 0.5) * 300; // Cloud variation
  }
  illuminance = Math.max(0, illuminance);

  // Flow speed (m/s) - Rhine is faster than Erft, ~1.5-2.5 m/s
  const speed = 2.0 + (Math.random() - 0.5) * 0.8;

  // Water level from interpolated waypoint data with slight variation
  const water_level = waterLevel + (Math.random() - 0.5) * 10;

  return {
    temperature: parseFloat(temperature.toFixed(1)),
    water_temperature: parseFloat(waterTemperature.toFixed(1)),
    humidity: parseFloat(Math.min(100, Math.max(0, humidity)).toFixed(1)),
    pressure: parseFloat(pressure.toFixed(1)),
    illuminance: Math.round(illuminance),
    speed: parseFloat(speed.toFixed(2)),
    water_level: Math.round(water_level)
  };
}

/**
 * Send readings to API
 */
async function sendReading(deviceId, type, value, timestamp, endpointId = null) {
  const payload = {
    deviceId,
    type,
    value,
    timestamp: Math.floor(new Date(timestamp).getTime() / 1000),
    endpointId
  };

  return new Promise((resolve, reject) => {
    const url = new URL(`${API_BASE_URL}/api/readings`);
    const isHttps = url.protocol === 'https:';
    const client = isHttps ? https : http;

    const options = {
      hostname: url.hostname,
      port: url.port || (isHttps ? 443 : 80),
      path: url.pathname,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      rejectUnauthorized: false // Allow self-signed certs
    };

    const req = client.request(options, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve(JSON.parse(data || '{}'));
        } else {
          reject(new Error(`HTTP ${res.statusCode}: ${data}`));
        }
      });
    });

    req.on('error', reject);
    req.write(JSON.stringify(payload));
    req.end();
  });
}

/**
 * Main function
 */
async function main() {
  console.log('='.repeat(60));
  console.log('Rhine River Simulation Data Generator');
  console.log('Route: Erftmündung (Neuss) → Dutch Border (Lobith)');
  console.log('='.repeat(60));
  console.log(`API URL: ${API_BASE_URL}`);
  console.log(`Node ID: ${NODE_ID}`);
  console.log(`Start Time: ${START_TIME.toISOString()}`);
  console.log('');
  console.log('Current Rhine Conditions (17.12.2025):');
  console.log(`  Water Temperature: ${RHINE_CONDITIONS.waterTemperature}°C`);
  console.log(`  Water Level Düsseldorf: ${RHINE_CONDITIONS.waterLevels.duesseldorf} cm`);
  console.log(`  Water Level Emmerich: ${RHINE_CONDITIONS.waterLevels.emmerich} cm`);
  console.log('');

  // Generate GPS points
  console.log('Generating GPS route...');
  const gpsPoints = generateGpsPoints();
  console.log(`Generated ${gpsPoints.length} GPS points`);
  console.log('');

  // Sensor types to generate (with endpoint IDs matching node assignments)
  // EndpointId 1: neo-6m (GPS) - latitude, longitude, speed
  // EndpointId 2: bme280 - temperature, humidity, pressure
  // EndpointId 3: ds18b20 - water_temperature
  // EndpointId 4: bh1750 - illuminance
  // EndpointId 5: jsn-sr04t - water_level (depth)
  const sensorTypes = [
    { type: 'latitude', endpointId: 1 },
    { type: 'longitude', endpointId: 1 },
    { type: 'speed', endpointId: 1 },
    { type: 'temperature', endpointId: 2 },
    { type: 'humidity', endpointId: 2 },
    { type: 'pressure', endpointId: 2 },
    { type: 'water_temperature', endpointId: 3 },
    { type: 'illuminance', endpointId: 4 },
    { type: 'water_level', endpointId: 5 }
  ];

  console.log('Sending readings to API...');
  console.log('This will take a while for', gpsPoints.length, 'points x', sensorTypes.length, 'sensors =', gpsPoints.length * sensorTypes.length, 'readings');
  console.log('');

  let successCount = 0;
  let errorCount = 0;

  for (let i = 0; i < gpsPoints.length; i++) {
    const point = gpsPoints[i];
    const sensorValues = generateSensorValues(point.progress, point.timestamp, point.waterLevel);

    // Add GPS coordinates to sensor values
    sensorValues.latitude = point.lat;
    sensorValues.longitude = point.lon;

    // Send each sensor reading
    for (const sensor of sensorTypes) {
      const value = sensorValues[sensor.type];
      if (value !== undefined) {
        try {
          await sendReading(NODE_ID, sensor.type, value, point.timestamp, sensor.endpointId);
          successCount++;
        } catch (error) {
          errorCount++;
          if (errorCount <= 5) {
            console.error(`Error at point ${i}, ${sensor.type}:`, error.message);
          }
        }
      }
    }

    // Progress indicator
    if (i % 100 === 0 || i === gpsPoints.length - 1) {
      const percent = ((i + 1) / gpsPoints.length * 100).toFixed(1);
      process.stdout.write(`\rProgress: ${percent}% (${i + 1}/${gpsPoints.length} points, ${successCount} readings sent, ${errorCount} errors)`);
    }

    // Small delay to avoid overwhelming the API
    if (i % 10 === 0) {
      await new Promise(resolve => setTimeout(resolve, 50));
    }
  }

  console.log('\n');
  console.log('='.repeat(60));
  console.log('Simulation Complete!');
  console.log(`Total readings sent: ${successCount}`);
  console.log(`Errors: ${errorCount}`);
  console.log('='.repeat(60));
}

// Run
main().catch(console.error);
