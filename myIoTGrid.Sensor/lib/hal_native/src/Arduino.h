/**
 * Arduino.h Stub for Native Platform
 * Provides Arduino-compatible API for native/simulator builds
 */

#ifndef ARDUINO_H
#define ARDUINO_H

// IMPORTANT: Include <cmath> and <limits> FIRST, before any min/max/abs definitions
// This prevents macro conflicts with std::numeric_limits<T>::min() etc.
#include <cmath>
#include <limits>
#include <cstdint>
#include <cstddef>
#include <cstring>
#include <cstdlib>
#include <string>
#include <functional>
#include <iostream>
#include <chrono>
#include <thread>

// Arduino type definitions
typedef bool boolean;
typedef uint8_t byte;

// Arduino constants
#define HIGH 1
#define LOW 0
#define INPUT 0
#define OUTPUT 1
#define INPUT_PULLUP 2

// Timing functions
inline unsigned long millis() {
    static auto start = std::chrono::steady_clock::now();
    auto now = std::chrono::steady_clock::now();
    return std::chrono::duration_cast<std::chrono::milliseconds>(now - start).count();
}

inline unsigned long micros() {
    static auto start = std::chrono::steady_clock::now();
    auto now = std::chrono::steady_clock::now();
    return std::chrono::duration_cast<std::chrono::microseconds>(now - start).count();
}

inline void delay(unsigned long ms) {
    std::this_thread::sleep_for(std::chrono::milliseconds(ms));
}

inline void delayMicroseconds(unsigned int us) {
    std::this_thread::sleep_for(std::chrono::microseconds(us));
}

// Random functions
inline long random(long max) {
    return rand() % max;
}

inline long random(long min, long max) {
    return min + (rand() % (max - min));
}

inline void randomSeed(unsigned long seed) {
    srand(seed);
}

// String class (Arduino-compatible with ArduinoJson support)
class String {
private:
    std::string _str;
    mutable size_t _readPos = 0;

public:
    String() : _str(), _readPos(0) {}
    String(const char* str) : _str(str ? str : ""), _readPos(0) {}
    String(const std::string& str) : _str(str), _readPos(0) {}
    String(int value) : _str(std::to_string(value)), _readPos(0) {}
    String(unsigned int value) : _str(std::to_string(value)), _readPos(0) {}
    String(long value) : _str(std::to_string(value)), _readPos(0) {}
    String(unsigned long value) : _str(std::to_string(value)), _readPos(0) {}
    String(float value, int decimalPlaces = 2) : _readPos(0) {
        char buf[32];
        snprintf(buf, sizeof(buf), "%.*f", decimalPlaces, value);
        _str = buf;
    }
    String(double value, int decimalPlaces = 2) : _readPos(0) {
        char buf[32];
        snprintf(buf, sizeof(buf), "%.*f", decimalPlaces, value);
        _str = buf;
    }

    const char* c_str() const { return _str.c_str(); }
    size_t length() const { return _str.length(); }
    bool isEmpty() const { return _str.empty(); }

    // Stream-like interface for ArduinoJson
    int read() const {
        if (_readPos >= _str.length()) return -1;
        return static_cast<unsigned char>(_str[_readPos++]);
    }

    int peek() const {
        if (_readPos >= _str.length()) return -1;
        return static_cast<unsigned char>(_str[_readPos]);
    }

    size_t readBytes(char* buffer, size_t length) const {
        size_t count = 0;
        while (count < length && _readPos < _str.length()) {
            buffer[count++] = _str[_readPos++];
        }
        return count;
    }

    String& operator=(const String& other) { _str = other._str; _readPos = 0; return *this; }
    String& operator=(const char* str) { _str = str ? str : ""; _readPos = 0; return *this; }
    String operator+(const String& other) const { return String(_str + other._str); }
    String operator+(const char* str) const { return String(_str + (str ? str : "")); }
    String& operator+=(const String& other) { _str += other._str; return *this; }
    String& operator+=(const char* str) { if (str) _str += str; return *this; }
    String& operator+=(char c) { _str += c; return *this; }

    bool operator==(const String& other) const { return _str == other._str; }
    bool operator==(const char* str) const { return _str == (str ? str : ""); }
    bool operator!=(const String& other) const { return _str != other._str; }
    bool operator!=(const char* str) const { return _str != (str ? str : ""); }

    char operator[](size_t index) const { return _str[index]; }
    char& operator[](size_t index) { return _str[index]; }

    int indexOf(char c) const {
        size_t pos = _str.find(c);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int indexOf(const String& str) const {
        size_t pos = _str.find(str._str);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    String substring(size_t beginIndex) const {
        return String(_str.substr(beginIndex));
    }

    String substring(size_t beginIndex, size_t endIndex) const {
        return String(_str.substr(beginIndex, endIndex - beginIndex));
    }

    void trim() {
        size_t start = _str.find_first_not_of(" \t\n\r");
        size_t end = _str.find_last_not_of(" \t\n\r");
        if (start == std::string::npos) {
            _str.clear();
        } else {
            _str = _str.substr(start, end - start + 1);
        }
    }

    void toLowerCase() {
        for (char& c : _str) {
            c = std::tolower(c);
        }
    }

    void toUpperCase() {
        for (char& c : _str) {
            c = std::toupper(c);
        }
    }

    int toInt() const {
        return std::atoi(_str.c_str());
    }

    float toFloat() const {
        return std::atof(_str.c_str());
    }

    double toDouble() const {
        return std::stod(_str);
    }

    bool startsWith(const String& prefix) const {
        return _str.find(prefix._str) == 0;
    }

    bool endsWith(const String& suffix) const {
        if (suffix._str.length() > _str.length()) return false;
        return _str.compare(_str.length() - suffix._str.length(), suffix._str.length(), suffix._str) == 0;
    }

    void replace(const String& find, const String& replace) {
        size_t pos = 0;
        while ((pos = _str.find(find._str, pos)) != std::string::npos) {
            _str.replace(pos, find._str.length(), replace._str);
            pos += replace._str.length();
        }
    }

    // Conversion operators
    operator std::string() const { return _str; }

    // Friend operators for const char* + String
    friend String operator+(const char* lhs, const String& rhs) {
        return String(std::string(lhs ? lhs : "") + rhs._str);
    }

    // ArduinoJson write support (for serializeJson)
    size_t write(uint8_t c) {
        _str += static_cast<char>(c);
        return 1;
    }

    size_t write(const uint8_t* buffer, size_t size) {
        _str.append(reinterpret_cast<const char*>(buffer), size);
        return size;
    }

    // Get internal string for direct access
    const std::string& str() const { return _str; }
    std::string& str() { return _str; }
};

// Serial class (minimal implementation for native)
class SerialClass {
public:
    void begin(unsigned long baud) {
        (void)baud;
        std::cout << "[Serial] Initialized at " << baud << " baud" << std::endl;
    }

    void print(const char* str) { std::cout << str; }
    void print(const String& str) { std::cout << str.c_str(); }
    void print(int value) { std::cout << value; }
    void print(unsigned int value) { std::cout << value; }
    void print(long value) { std::cout << value; }
    void print(unsigned long value) { std::cout << value; }
    void print(double value) { std::cout << value; }
    void print(float value) { std::cout << value; }
    void print(char c) { std::cout << c; }

    void println() { std::cout << std::endl; }
    void println(const char* str) { std::cout << str << std::endl; }
    void println(const String& str) { std::cout << str.c_str() << std::endl; }
    void println(int value) { std::cout << value << std::endl; }
    void println(unsigned int value) { std::cout << value << std::endl; }
    void println(long value) { std::cout << value << std::endl; }
    void println(unsigned long value) { std::cout << value << std::endl; }
    void println(double value) { std::cout << value << std::endl; }
    void println(float value) { std::cout << value << std::endl; }
    void println(char c) { std::cout << c << std::endl; }

    template<typename... Args>
    void printf(const char* format, Args... args) {
        char buffer[512];
        snprintf(buffer, sizeof(buffer), format, args...);
        std::cout << buffer << std::flush;
    }

    int available() { return 0; }
    int read() { return -1; }
    void flush() { std::cout.flush(); }
};

extern SerialClass Serial;

// Placeholder for GPIO functions (no-op in native)
inline void pinMode(uint8_t pin, uint8_t mode) {
    (void)pin;
    (void)mode;
}

inline void digitalWrite(uint8_t pin, uint8_t value) {
    (void)pin;
    (void)value;
}

inline int digitalRead(uint8_t pin) {
    (void)pin;
    return LOW;
}

inline int analogRead(uint8_t pin) {
    (void)pin;
    return 0;
}

inline void analogWrite(uint8_t pin, int value) {
    (void)pin;
    (void)value;
}

// Math functions - use inline templates to avoid conflicts with <cmath> and std::numeric_limits
// DO NOT use macros for min/max/abs as they break std::numeric_limits<T>::min() etc.

// Undefine any existing macros that might have been defined elsewhere
#ifdef min
#undef min
#endif
#ifdef max
#undef max
#endif
#ifdef abs
#undef abs
#endif

// Use std::min, std::max, std::abs from <algorithm>/<cmath> - included above
// For Arduino compatibility, provide these in global namespace
#include <algorithm>
using std::min;
using std::max;
using std::abs;
using std::fabs;

template<typename T>
inline T constrain(T amt, T low, T high) {
    return (amt < low) ? low : ((amt > high) ? high : amt);
}

inline long map(long x, long in_min, long in_max, long out_min, long out_max) {
    return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}

// Arduino code can call min(), max(), abs() directly using std:: versions from <cmath>
// We do NOT define macros here to avoid conflicts with std::numeric_limits<T>::min() etc.

// yield function (no-op in native)
inline void yield() {}

#endif // ARDUINO_H
