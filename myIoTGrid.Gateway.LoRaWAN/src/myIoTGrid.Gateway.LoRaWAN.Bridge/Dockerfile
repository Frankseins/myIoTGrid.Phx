# myIoTGrid.Gateway.LoRaWAN.Bridge Dockerfile
# Multi-stage build for ARM64 + AMD64
# Stand: 10.12.2025

# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore
COPY ["myIoTGrid.Gateway.LoRaWAN.Bridge.csproj", "./"]
RUN dotnet restore "myIoTGrid.Gateway.LoRaWAN.Bridge.csproj"

# Copy source and build
COPY . .
RUN dotnet build "myIoTGrid.Gateway.LoRaWAN.Bridge.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ============================================
# Stage 2: Publish
# ============================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "myIoTGrid.Gateway.LoRaWAN.Bridge.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ============================================
# Stage 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install wget for health checks
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Create non-root user (use UID 1001 to avoid conflicts)
RUN useradd -r -u 1001 -g root appuser && \
    chown -R appuser:root /app
USER appuser

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5100
ENV DOTNET_EnableDiagnostics=0

# Expose port
EXPOSE 5100

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD wget -q --spider http://localhost:5100/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "myIoTGrid.Gateway.LoRaWAN.Bridge.dll"]
