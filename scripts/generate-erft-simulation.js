#!/usr/bin/env node
/**
 * Erft River Simulation Data Generator
 * Generates GPS readings along the Erft river from Grevenbroich to Rhine
 * For Node "PHX" with sensor readings for December 16, 2024
 */

const https = require('https');
const http = require('http');

// Configuration
const API_BASE_URL = process.env.API_URL || 'https://phx.myiotgrid.cloud';
const NODE_ID = 'Erft-02'; // Node identifier

// Erft River Route (Grevenbroich to Rhine/Neuss)
// Real GPS coordinates from OpenStreetMap
const ROUTE = {
  start: { lat: 51.086887, lon: 6.588180, name: 'Grevenbroich' },
  end: { lat: 51.186173, lon: 6.733080, name: 'Neuss/Rhein' }
};

// Simulation parameters
const INTERVAL_SECONDS = 60;
const SPEED_KMH = 5; // Kayak/boat speed
const START_TIME = new Date('2025-12-17T08:00:00Z');

// Weather data for December 16 (realistic values)
const WEATHER_BASE = {
  temperature: 4,      // °C
  waterTemperature: 7, // °C
  humidity: 80,        // %
  pressure: 1018,      // hPa
  illuminance: 800     // lux (winter daylight)
};

// Waypoints along the Erft - REAL coordinates from OpenStreetMap
const WAYPOINTS = [
  { lat: 51.086887, lon: 6.588180, name: 'Grevenbroich Start' },
  { lat: 51.087856, lon: 6.586550, name: 'Grevenbroich Erftaue' },
  { lat: 51.089925, lon: 6.587119, name: 'Grevenbroich Nord' },
  { lat: 51.092905, lon: 6.587940, name: 'Erft bei Kapellen' },
  { lat: 51.096145, lon: 6.590488, name: 'Kapellen' },
  { lat: 51.099543, lon: 6.607198, name: 'Wevelinghoven Süd' },
  { lat: 51.100268, lon: 6.608224, name: 'Wevelinghoven' },
  { lat: 51.103941, lon: 6.611570, name: 'Wevelinghoven Nord' },
  { lat: 51.110614, lon: 6.624524, name: 'Erft bei Gustorf' },
  { lat: 51.126871, lon: 6.635057, name: 'Gustorf' },
  { lat: 51.133036, lon: 6.637143, name: 'Fürth' },
  { lat: 51.144054, lon: 6.661300, name: 'Erft bei Grimlinghausen' },
  { lat: 51.149926, lon: 6.672649, name: 'Uedesheim' },
  { lat: 51.160755, lon: 6.688861, name: 'Neuss-Grimlinghausen Süd' },
  { lat: 51.161957, lon: 6.690436, name: 'Neuss-Grimlinghausen' },
  { lat: 51.166116, lon: 6.700815, name: 'Erftmündung Süd' },
  { lat: 51.170515, lon: 6.706726, name: 'Erftkanal' },
  { lat: 51.174377, lon: 6.719678, name: 'Hafenbecken' },
  { lat: 51.175699, lon: 6.725797, name: 'Erft kurz vor Mündung' },
  { lat: 51.178204, lon: 6.726263, name: 'Erftmündung' },
  { lat: 51.186173, lon: 6.733080, name: 'Rhein bei Neuss' }
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
  let currentWaypoint = 0;
  let distanceTraveled = 0;
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

    // Add GPS noise (realistic ~3m accuracy)
    const latNoise = (Math.random() - 0.5) * 0.00006; // ~6m variation
    const lonNoise = (Math.random() - 0.5) * 0.00006;

    const timestamp = new Date(START_TIME.getTime() + i * INTERVAL_SECONDS * 1000);

    points.push({
      lat: parseFloat((lat + latNoise).toFixed(6)),
      lon: parseFloat((lon + lonNoise).toFixed(6)),
      timestamp: timestamp.toISOString(),
      progress: i / numPoints
    });
  }

  return points;
}

/**
 * Generate realistic sensor values with daily variation
 */
function generateSensorValues(progress, timestamp) {
  const hour = new Date(timestamp).getUTCHours();

  // Temperature variation (colder in morning, warmer midday)
  const tempVariation = Math.sin((hour - 6) * Math.PI / 12) * 2;
  const temperature = WEATHER_BASE.temperature + tempVariation + (Math.random() - 0.5) * 0.5;

  // Water temperature is more stable
  const waterTemperature = WEATHER_BASE.waterTemperature + (Math.random() - 0.5) * 0.3;

  // Humidity (higher in morning/evening)
  const humidityVariation = -Math.sin((hour - 6) * Math.PI / 12) * 5;
  const humidity = WEATHER_BASE.humidity + humidityVariation + (Math.random() - 0.5) * 2;

  // Pressure (slight random walk)
  const pressure = WEATHER_BASE.pressure + (Math.random() - 0.5) * 2;

  // Illuminance (daylight curve, winter in Germany ~8am-4pm)
  let illuminance = 0;
  if (hour >= 8 && hour <= 16) {
    const dayProgress = (hour - 8) / 8;
    illuminance = Math.sin(dayProgress * Math.PI) * 2000 + 200;
    illuminance += (Math.random() - 0.5) * 200; // Cloud variation
  }
  illuminance = Math.max(0, illuminance);

  // Flow speed (m/s) - Erft is slow, ~0.3-0.8 m/s
  const speed = 0.5 + (Math.random() - 0.5) * 0.3;

  // Water level/depth (cm) - Erft varies 50-200cm depending on location
  // Deeper near Neuss (end), shallower near Grevenbroich (start)
  const baseDepth = 80 + progress * 60; // 80cm start -> 140cm end
  const water_level = baseDepth + (Math.random() - 0.5) * 20; // ±10cm variation

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
  console.log('Erft River Simulation Data Generator');
  console.log('='.repeat(60));
  console.log(`API URL: ${API_BASE_URL}`);
  console.log(`Node ID: ${NODE_ID}`);
  console.log(`Start Time: ${START_TIME.toISOString()}`);
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
    const sensorValues = generateSensorValues(point.progress, point.timestamp);

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
