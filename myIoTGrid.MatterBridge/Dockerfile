# myIoTGrid Matter Bridge
# Multi-arch Dockerfile for ARM64 (Raspberry Pi) and AMD64

FROM node:20-slim AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./
COPY tsconfig.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY src/ ./src/

# Build TypeScript
RUN npm run build

# Production stage
FROM node:20-slim AS production

WORKDIR /app

# Install dbus for Matter mDNS (required for discovery)
RUN apt-get update && apt-get install -y \
    dbus \
    avahi-daemon \
    libavahi-compat-libdnssd-dev \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy package files and install production dependencies only
COPY package*.json ./
RUN npm ci --omit=dev

# Copy built files from builder
COPY --from=builder /app/dist ./dist

# Create data directory for credentials
RUN mkdir -p /app/data

# Environment variables
ENV NODE_ENV=production
ENV PORT=3000
ENV MATTER_PORT=5540
ENV STORAGE_PATH=/app/data
ENV LOG_LEVEL=info

# Vendor configuration (can be overridden)
ENV VENDOR_ID=0xFFF1
ENV VENDOR_NAME=myIoTGrid
ENV PRODUCT_NAME="myIoTGrid Hub"
ENV PRODUCT_ID=0x8001
ENV DISCRIMINATOR=3840
ENV PASSCODE=20202021

# Expose API port and Matter port
EXPOSE 3000
EXPOSE 5540/udp
EXPOSE 5540/tcp

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:3000/health || exit 1

# Run the application
CMD ["node", "dist/index.js"]
