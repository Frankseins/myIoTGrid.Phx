# myIoTGrid Sensor - Native Simulator Docker Image
# Builds and runs the sensor firmware as a native Linux application
# Using Debian Bookworm with GCC 12 (GCC 10 has numeric_limits bug)

FROM debian:bookworm AS builder

# Install build dependencies with GCC 12 (with retry for transient network errors)
RUN for i in 1 2 3; do \
        apt-get update && apt-get install -y --fix-missing \
            python3 \
            python3-pip \
            python3-venv \
            git \
            curl \
            build-essential \
            libcurl4-openssl-dev \
            uuid-dev \
        && break || sleep 5; \
    done \
    && rm -rf /var/lib/apt/lists/*

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
FROM debian:bookworm-slim

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

# Create data directory (sensor uses ./data relative to working dir)
RUN mkdir -p /data

WORKDIR /app

# Create data dir in working directory and link to /data for persistence
RUN mkdir -p /app/data && chown sensor:sensor /app/data

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

# Data volume for persistence
VOLUME ["/app/data"]

# Health check - sensor should be running
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD pgrep -x sensor || exit 1

# Run the sensor
CMD ["/app/sensor"]
