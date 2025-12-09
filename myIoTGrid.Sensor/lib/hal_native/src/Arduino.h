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
#include <cstdarg>
#include <string>
#include <functional>
#include <iostream>
#include <chrono>
#include <thread>
#include <cctype>

// Arduino type definitions
typedef bool boolean;
typedef uint8_t byte;

// Arduino constants
#define HIGH 1
#define LOW 0
#define INPUT 0
#define OUTPUT 1
#define INPUT_PULLUP 2
#define INPUT_PULLDOWN 3

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

    void reserve(size_t size) { _str.reserve(size); }
    void remove(size_t index, size_t count = 1) {
        if (index < _str.length()) {
            _str.erase(index, count);
        }
    }
    void clear() { _str.clear(); _readPos = 0; }

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
    bool operator<(const String& other) const { return _str < other._str; }
    bool operator>(const String& other) const { return _str > other._str; }
    bool operator<=(const String& other) const { return _str <= other._str; }
    bool operator>=(const String& other) const { return _str >= other._str; }

    char operator[](size_t index) const { return _str[index]; }
    char& operator[](size_t index) { return _str[index]; }

    int indexOf(char c) const {
        size_t pos = _str.find(c);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int indexOf(char c, size_t fromIndex) const {
        if (fromIndex >= _str.length()) return -1;
        size_t pos = _str.find(c, fromIndex);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int indexOf(const String& str) const {
        size_t pos = _str.find(str._str);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int indexOf(const String& str, size_t fromIndex) const {
        if (fromIndex >= _str.length()) return -1;
        size_t pos = _str.find(str._str, fromIndex);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int indexOf(const char* str) const {
        if (!str) return -1;
        size_t pos = _str.find(str);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int indexOf(const char* str, size_t fromIndex) const {
        if (!str || fromIndex >= _str.length()) return -1;
        size_t pos = _str.find(str, fromIndex);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int lastIndexOf(char c) const {
        size_t pos = _str.rfind(c);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int lastIndexOf(char c, size_t fromIndex) const {
        if (fromIndex >= _str.length()) fromIndex = _str.length() - 1;
        size_t pos = _str.rfind(c, fromIndex);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int lastIndexOf(const String& str) const {
        size_t pos = _str.rfind(str._str);
        return pos == std::string::npos ? -1 : static_cast<int>(pos);
    }

    int lastIndexOf(const char* str) const {
        if (!str) return -1;
        size_t pos = _str.rfind(str);
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

    bool equalsIgnoreCase(const String& other) const {
        if (_str.length() != other._str.length()) return false;
        for (size_t i = 0; i < _str.length(); i++) {
            if (std::tolower(_str[i]) != std::tolower(other._str[i])) return false;
        }
        return true;
    }

    bool equalsIgnoreCase(const char* other) const {
        if (!other) return _str.empty();
        size_t otherLen = strlen(other);
        if (_str.length() != otherLen) return false;
        for (size_t i = 0; i < _str.length(); i++) {
            if (std::tolower(_str[i]) != std::tolower(other[i])) return false;
        }
        return true;
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

    size_t write(uint8_t c) { std::cout << static_cast<char>(c); return 1; }
    size_t write(const uint8_t* buffer, size_t size) {
        for (size_t i = 0; i < size; i++) {
            std::cout << static_cast<char>(buffer[i]);
        }
        return size;
    }
    size_t write(const char* str) {
        if (!str) return 0;
        std::cout << str;
        return strlen(str);
    }
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

// File stub class for native builds (SD card operations are no-ops)
class File {
public:
    File() : _open(false), _size(0) {}

    operator bool() const { return _open; }
    bool isOpen() const { return _open; }
    void close() { _open = false; }
    size_t size() const { return _size; }
    size_t position() const { return 0; }
    bool seek(size_t pos) { (void)pos; return false; }
    int read() { return -1; }
    size_t read(uint8_t* buf, size_t size) { (void)buf; (void)size; return 0; }
    size_t write(uint8_t c) { (void)c; return 0; }
    size_t write(const uint8_t* buf, size_t size) { (void)buf; (void)size; return 0; }
    size_t print(const char* str) { (void)str; return 0; }
    size_t print(const String& str) { (void)str; return 0; }
    size_t println(const char* str) { (void)str; return 0; }
    size_t println(const String& str) { (void)str; return 0; }
    size_t println() { return 0; }
    void flush() {}
    String name() const { return ""; }
    bool isDirectory() const { return false; }
    File openNextFile() { return File(); }

private:
    bool _open;
    size_t _size;
};

// Print base class (for SerialCapture and other classes that need Print interface)
class Print {
public:
    virtual ~Print() = default;
    virtual size_t write(uint8_t c) = 0;
    virtual size_t write(const uint8_t* buffer, size_t size) {
        size_t count = 0;
        for (size_t i = 0; i < size; i++) {
            count += write(buffer[i]);
        }
        return count;
    }

    size_t write(const char* str) {
        if (!str) return 0;
        return write(reinterpret_cast<const uint8_t*>(str), strlen(str));
    }

    size_t print(const char* str) { return write(str); }
    size_t print(char c) { return write(static_cast<uint8_t>(c)); }
    size_t print(int value) {
        char buf[16];
        snprintf(buf, sizeof(buf), "%d", value);
        return write(buf);
    }
    size_t print(unsigned int value) {
        char buf[16];
        snprintf(buf, sizeof(buf), "%u", value);
        return write(buf);
    }
    size_t print(long value) {
        char buf[32];
        snprintf(buf, sizeof(buf), "%ld", value);
        return write(buf);
    }
    size_t print(unsigned long value) {
        char buf[32];
        snprintf(buf, sizeof(buf), "%lu", value);
        return write(buf);
    }
    size_t print(double value, int decimalPlaces = 2) {
        char buf[32];
        snprintf(buf, sizeof(buf), "%.*f", decimalPlaces, value);
        return write(buf);
    }
    size_t print(const String& str) { return write(str.c_str()); }

    size_t println() { return write("\n"); }
    size_t println(const char* str) { size_t n = print(str); n += println(); return n; }
    size_t println(char c) { size_t n = print(c); n += println(); return n; }
    size_t println(int value) { size_t n = print(value); n += println(); return n; }
    size_t println(unsigned int value) { size_t n = print(value); n += println(); return n; }
    size_t println(long value) { size_t n = print(value); n += println(); return n; }
    size_t println(unsigned long value) { size_t n = print(value); n += println(); return n; }
    size_t println(double value, int decimalPlaces = 2) { size_t n = print(value, decimalPlaces); n += println(); return n; }
    size_t println(const String& str) { size_t n = print(str); n += println(); return n; }
};

#endif // ARDUINO_H
