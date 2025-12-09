# =============================================================================
# myIoTGrid Hub API - Multi-Stage Dockerfile
# =============================================================================
# Optimized for:
# - Multi-Architecture builds (amd64 + arm64)
# - Layer caching (dependencies first)
# - Security (non-root user)
# - Small image size (aspnet runtime only)
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (for better layer caching)
# Hub projects
COPY ["myIoTGrid.Hub/myIoTGrid.Hub.sln", "myIoTGrid.Hub/"]
COPY ["myIoTGrid.Hub/src/myIoTGrid.Hub.Api/myIoTGrid.Hub.Api.csproj", "myIoTGrid.Hub/src/myIoTGrid.Hub.Api/"]
COPY ["myIoTGrid.Hub/src/myIoTGrid.Hub.Domain/myIoTGrid.Hub.Domain.csproj", "myIoTGrid.Hub/src/myIoTGrid.Hub.Domain/"]
COPY ["myIoTGrid.Hub/src/myIoTGrid.Hub.Shared/myIoTGrid.Hub.Shared.csproj", "myIoTGrid.Hub/src/myIoTGrid.Hub.Shared/"]
COPY ["myIoTGrid.Hub/src/myIoTGrid.Hub.Service/myIoTGrid.Hub.Service.csproj", "myIoTGrid.Hub/src/myIoTGrid.Hub.Service/"]
COPY ["myIoTGrid.Hub/src/myIoTGrid.Hub.Infrastructure/myIoTGrid.Hub.Infrastructure.csproj", "myIoTGrid.Hub/src/myIoTGrid.Hub.Infrastructure/"]
COPY ["myIoTGrid.Hub/src/myIoTGrid.Hub.Interface/myIoTGrid.Hub.Interface.csproj", "myIoTGrid.Hub/src/myIoTGrid.Hub.Interface/"]

# Shared projects
COPY ["myIoTGrid.Shared/myIoTGrid.Shared.Common/myIoTGrid.Shared.Common.csproj", "myIoTGrid.Shared/myIoTGrid.Shared.Common/"]
COPY ["myIoTGrid.Shared/myIoTGrid.Shared.Contracts/myIoTGrid.Shared.Contracts.csproj", "myIoTGrid.Shared/myIoTGrid.Shared.Contracts/"]
COPY ["myIoTGrid.Shared/myIoTGrid.Shared.Utilities/myIoTGrid.Shared.Utilities.csproj", "myIoTGrid.Shared/myIoTGrid.Shared.Utilities/"]

# Restore dependencies (cached if .csproj files haven't changed)
RUN dotnet restore "myIoTGrid.Hub/src/myIoTGrid.Hub.Api/myIoTGrid.Hub.Api.csproj"

# Copy everything else and build
COPY ["myIoTGrid.Hub/", "myIoTGrid.Hub/"]
COPY ["myIoTGrid.Shared/", "myIoTGrid.Shared/"]
WORKDIR "/src/myIoTGrid.Hub/src/myIoTGrid.Hub.Api"
RUN dotnet build "myIoTGrid.Hub.Api.csproj" -c Release -o /app/build

# -----------------------------------------------------------------------------
# Stage 2: Publish
# -----------------------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "myIoTGrid.Hub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Stage 3: Runtime
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

# Install curl and openssl for health checks and certificate generation
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl openssl && \
    rm -rf /var/lib/apt/lists/*

# Create data, logs, and certs directories
# Note: .NET images already have an 'app' user (UID 1654)
RUN mkdir -p /app/data /app/logs /app/certs && \
    chown -R app:app /app

# Generate self-signed certificate for development (will be overwritten in production)
# Using RSA 2048 with SHA256 for ESP32 mbedTLS compatibility
# SAN includes localhost and common private IP ranges for IoT devices
RUN openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /app/certs/privkey.pem \
    -out /app/certs/fullchain.pem \
    -subj "/C=DE/ST=NRW/L=Cologne/O=myIoTGrid/OU=Hub/CN=myiotgrid-hub" \
    -addext "subjectAltName=DNS:localhost,DNS:hub-api,DNS:myiotgrid-hub,IP:127.0.0.1,IP:192.168.0.1,IP:192.168.1.1,IP:192.168.178.1,IP:192.168.188.1,IP:10.0.0.1" \
    -addext "keyUsage=digitalSignature,keyEncipherment" \
    -addext "extendedKeyUsage=serverAuth" && \
    chown -R app:app /app/certs

# Copy published files
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R app:app /app

# Switch to non-root user (built-in .NET user)
USER app

# Expose HTTPS port
EXPOSE 5001

# Environment variables for HTTPS
ENV ASPNETCORE_URLS=https://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/fullchain.pem
ENV ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/app/certs/privkey.pem
ENV ConnectionStrings__HubDb="Data Source=/app/data/hub.db"

# Health check (HTTPS with insecure flag for self-signed cert)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -fk https://localhost:5001/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "myIoTGrid.Hub.Api.dll"]
