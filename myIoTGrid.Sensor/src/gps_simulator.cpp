#include "gps_simulator.h"
#include <math.h>

GPSSimulator::GPSSimulator()
    : _satellites(0), _fixType(0), _hdop(1.5),
      _lat(51.091234), _lon(6.582567), _alt(34.0),
      _headingRad(0.7), _speedKmh(15.0),
      _lastSatUpdate(0), _lastFixUpdate(0), _lastHdopUpdate(0),
      _lastPosUpdate(0), _lastSpeedTweak(0) {}

void GPSSimulator::init() {
    _satellites = 0;
    _fixType = 0;
    _hdop = 1.5;
    _lat = 51.091234;
    _lon = 6.582567;
    _alt = 34.0;
    _headingRad = 0.7; // ~40°
    _speedKmh = 15.0;  // km/h
    _lastSatUpdate = _lastFixUpdate = _lastHdopUpdate = 0;
    _lastPosUpdate = _lastSpeedTweak = 0;
    Serial.printf("[GPS] init: lat=%.6f lon=%.6f alt=%.2f fix=%d sats=%d hdop=%.2f speed=%.2f km/h\n",
                     _lat, _lon, _alt, _fixType, _satellites, _hdop, _speedKmh);
}

void GPSSimulator::update() {
    unsigned long now = millis();

    // Satellites: update every 5s
    if (now - _lastSatUpdate > 5000) {
        _lastSatUpdate = now;
        if (random(100) < 10) {
            _satellites = 0; // cold start
        } else {
            _satellites = random(4, 13); // 4..12
        }
        Serial.printf("[GPS] Satellites update: %d\n", _satellites);
    }

    // Fix type: update every 5s based on probability
    if (now - _lastFixUpdate > 5000) {
        _lastFixUpdate = now;
        int r = random(100);
        if (r < 10) _fixType = 0;      // 10% none
        else if (r < 30) _fixType = 2; // 20% 2D
        else _fixType = 3;             // 70% 3D
        Serial.printf("[GPS] Fix type update: %d\n", _fixType);
    }

    // HDOP: update every 5s, mostly good
    if (now - _lastHdopUpdate > 5000) {
        _lastHdopUpdate = now;
        _hdop = 0.5 + (random(450) / 100.0); // 0.5 .. 5.0
        Serial.printf("[GPS] HDOP update: %.2f\n", _hdop);
    }

    // Position update ~10 Hz max
    if (_lastPosUpdate == 0) {
        _lastPosUpdate = now;
        _lastSpeedTweak = now;
        return;
    }

    double dtSec = (now - _lastPosUpdate) / 1000.0;
    if (dtSec <= 0.1) return; // cap at ~10 Hz
    _lastPosUpdate = now;

    // Occasionally vary speed slightly (~4-5s)
    if (now - _lastSpeedTweak > 4000) {
        _lastSpeedTweak = now;
        _speedKmh += (random(-10, 11)) / 10.0; // +/-1.0
        if (_speedKmh < 5.0) _speedKmh = 5.0;
        if (_speedKmh > 25.0) _speedKmh = 25.0;
        Serial.printf("[GPS] Speed tweak: %.2f km/h\n", _speedKmh);
    }

    // Small heading drift for a natural slightly curved path
    double headingJitter = (random(-2, 3) * (M_PI / 180.0)) * 0.03;
    _headingRad += headingJitter;

    // Distance advanced since last update
    double speedMs = _speedKmh * 1000.0 / 3600.0;
    double distM = speedMs * dtSec;

    // Convert meters to degrees
    double latRad = _lat * (M_PI / 180.0);
    double dLat = (distM * cos(_headingRad)) / 111111.0; // ~111.111 km/deg
    double denom = 111111.0 * max(0.2, cos(latRad));
    double dLon = (distM * sin(_headingRad)) / denom;    // avoid /0 near poles

    // GPS noise simulation (realistic ~3-5m accuracy variation)
    // 1 degree latitude ≈ 111km, so 0.00001° ≈ 1.1m
    dLat += (random(-50, 51)) / 1e6;  // +/- 5m noise
    dLon += (random(-50, 51)) / 1e6;  // +/- 5m noise

    _lat += dLat;
    _lon += dLon;

    // Smooth altitude variation (gentle wobble + small noise)
    _alt += sin(now / 5000.0) * 0.02; // +/- few cm over time
    _alt += (random(-2, 3)) / 100.0;  // +/- 2 cm noise

    // Throttled pose logging (every ~5s)
    static unsigned long lastPoseLog = 0;
    if (now - lastPoseLog > 5000) {
        lastPoseLog = now;
        Serial.printf("[GPS] Pose: lat=%.6f lon=%.6f alt=%.2f hdop=%.2f fix=%d speed=%.2f km/h\n",
                         _lat, _lon, _alt, _hdop, _fixType, _speedKmh);
    }
}

int GPSSimulator::getSatellites() const { return _satellites; }
int GPSSimulator::getFixType() const { return _fixType; }
double GPSSimulator::getHdop() const { return _hdop; }
double GPSSimulator::getLatitude() const { return _lat; }
double GPSSimulator::getLongitude() const { return _lon; }
double GPSSimulator::getAltitude() const { return _alt; }
double GPSSimulator::getSpeedKmh() const { return _speedKmh; }
