# myIoTGrid Sensor - Native Simulator Docker Image
# Builds and runs the sensor firmware as a native Linux application

FROM ubuntu:24.04 AS builder

# Install build dependencies
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    python3-venv \
    git \
    curl \
    build-essential \
    libcurl4-openssl-dev \
    uuid-dev \
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
FROM ubuntu:24.04

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    libcurl4 \
    libuuid1 \
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

# Environment variables for Hub connection
ENV HUB_HOST=hub-api
ENV HUB_PORT=5000

# Data volume
VOLUME ["/data"]

# Health check - sensor should be running
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD pgrep -x sensor || exit 1

# Run the sensor
CMD ["/app/sensor"]
