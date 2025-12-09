/**
 * myIoTGrid.Sensor - Unity Test Configuration
 *
 * Minimal configuration for Unity test framework on native platform.
 */

#ifndef UNITY_CONFIG_H
#define UNITY_CONFIG_H

// Use standard 64-bit integer support on native platform
#define UNITY_INCLUDE_64

// Include double precision floating point support
#define UNITY_INCLUDE_DOUBLE

// Use standard print functions
#include <stdio.h>
#define UNITY_OUTPUT_CHAR(a) putchar(a)
#define UNITY_OUTPUT_FLUSH() fflush(stdout)

#endif // UNITY_CONFIG_H
