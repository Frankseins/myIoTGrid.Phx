# =============================================================================
# myIoTGrid Hub Frontend - Multi-Stage Dockerfile
# =============================================================================
# Optimized for:
# - Multi-Architecture builds (amd64 + arm64)
# - HTTPS with SSL/TLS
# - Layer caching
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build Angular Application
# -----------------------------------------------------------------------------
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files first (for better layer caching)
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the application
RUN npm run build -- --configuration=production

# -----------------------------------------------------------------------------
# Stage 2: Serve with nginx (HTTPS)
# -----------------------------------------------------------------------------
FROM nginx:alpine

# Install openssl for self-signed cert generation (development)
RUN apk add --no-cache openssl

# Copy nginx configuration (HTTPS enabled)
COPY docker/nginx.conf /etc/nginx/nginx.conf

# Copy built application from build stage
COPY --from=build /app/dist/myIoTGrid.Hub.Frontend/browser /usr/share/nginx/html

# Create certs directory
RUN mkdir -p /etc/nginx/certs

# Generate self-signed certificate for development (will be overwritten in production)
RUN openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/nginx/certs/privkey.pem \
    -out /etc/nginx/certs/fullchain.pem \
    -subj "/C=DE/ST=NRW/L=Cologne/O=myIoTGrid/OU=Development/CN=localhost"

# Expose ports (HTTP for redirect, HTTPS for traffic)
EXPOSE 80 443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
