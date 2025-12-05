# myIoTGrid Sensor - Native Simulator Docker Image
# Builds and runs the sensor firmware as a native Linux application
# Using Debian Bullseye for stable GCC 10 to avoid cmath/numeric_limits bug

FROM debian:bullseye AS builder

# Install build dependencies with GCC 10 (with retry for transient network errors)
RUN for i in 1 2 3; do \
        apt-get update && apt-get install -y --fix-missing \
            python3 \
            python3-pip \
            python3-venv \
            git \
            curl \
            build-essential \
            g++-10 \
            gcc-10 \
            libcurl4-openssl-dev \
            uuid-dev \
        && break || sleep 5; \
    done \
    && rm -rf /var/lib/apt/lists/* \
    && update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-10 100 \
    && update-alternatives --install /usr/bin/g++ g++ /usr/bin/g++-10 100

# Install PlatformIO
RUN python3 -m venv /opt/pio && \
    /opt/pio/bin/pip install platformio

ENV PATH="/opt/pio/bin:${PATH}"

# Set working directory
WORKDIR /app

# Copy project files
COPY platformio.ini ./
COPY include/ ./include/
COPY src/ ./src/
COPY lib/ ./lib/

# Build native target
RUN pio run -e native

# ============================================
# Runtime Image
# ============================================
FROM debian:bullseye-slim

# Install runtime dependencies (including ca-certificates for HTTPS) with retry
RUN for i in 1 2 3; do \
        apt-get update && apt-get install -y --fix-missing \
            libcurl4 \
            libuuid1 \
            ca-certificates \
        && break || sleep 5; \
    done \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -s /bin/bash sensor

# Create data directory
RUN mkdir -p /data && chown sensor:sensor /data

WORKDIR /app

# Copy built binary from builder
COPY --from=builder /app/.pio/build/native/program /app/sensor

# Set ownership
RUN chown sensor:sensor /app/sensor

# Switch to non-root user
USER sensor

# Environment variables for Hub connection (HTTPS)
ENV HUB_HOST=hub-api
ENV HUB_PORT=5001
ENV HUB_PROTOCOL=https
ENV HUB_INSECURE=true

# Data volume
VOLUME ["/data"]

# Health check - sensor should be running
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD pgrep -x sensor || exit 1

# Run the sensor
CMD ["/app/sensor"]
