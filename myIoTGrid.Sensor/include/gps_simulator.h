/**
 * myIoTGrid.Sensor - GPS Simulator
 *
 * Provides simulated GPS values similar to SensorSimulator but focused on
 * GNSS-related data: satellites, fix type, HDOP, latitude, longitude,
 * altitude, and ground speed. Designed to replace ad-hoc GPS simulation
 * code previously embedded in main.cpp.
 */

#ifndef GPS_SIMULATOR_H
#define GPS_SIMULATOR_H

#include <Arduino.h>

class GPSSimulator {
public:
    GPSSimulator();

    void init();
    void update();            // call periodically (e.g., every 1s)

    // Accessors
    int getSatellites() const;   // 0-12 typical
    int getFixType() const;      // 0 = none, 2 = 2D, 3 = 3D
    double getHdop() const;      // ~0.5 .. 5.0 typical

    double getLatitude() const;  // degrees
    double getLongitude() const; // degrees
    double getAltitude() const;  // meters
    double getSpeedKmh() const;  // km/h

private:
    // State
    int _satellites;
    int _fixType;
    double _hdop;

    double _lat;
    double _lon;
    double _alt;
    double _headingRad;
    double _speedKmh;

    // Timers
    unsigned long _lastSatUpdate;
    unsigned long _lastFixUpdate;
    unsigned long _lastHdopUpdate;
    unsigned long _lastPosUpdate;
    unsigned long _lastSpeedTweak;
};

#endif // GPS_SIMULATOR_H
