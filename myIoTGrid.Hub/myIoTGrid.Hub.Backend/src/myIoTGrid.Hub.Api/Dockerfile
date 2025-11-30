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
COPY ["myIoTGrid.Hub.sln", "."]
COPY ["src/myIoTGrid.Hub.Api/myIoTGrid.Hub.Api.csproj", "src/myIoTGrid.Hub.Api/"]
COPY ["src/myIoTGrid.Hub.Domain/myIoTGrid.Hub.Domain.csproj", "src/myIoTGrid.Hub.Domain/"]
COPY ["src/myIoTGrid.Hub.Shared/myIoTGrid.Hub.Shared.csproj", "src/myIoTGrid.Hub.Shared/"]
COPY ["src/myIoTGrid.Hub.Service/myIoTGrid.Hub.Service.csproj", "src/myIoTGrid.Hub.Service/"]
COPY ["src/myIoTGrid.Hub.Infrastructure/myIoTGrid.Hub.Infrastructure.csproj", "src/myIoTGrid.Hub.Infrastructure/"]
COPY ["src/myIoTGrid.Hub.Interface/myIoTGrid.Hub.Interface.csproj", "src/myIoTGrid.Hub.Interface/"]

# Restore dependencies (cached if .csproj files haven't changed)
RUN dotnet restore "src/myIoTGrid.Hub.Api/myIoTGrid.Hub.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/myIoTGrid.Hub.Api"
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

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Create data and logs directories
# Note: .NET images already have an 'app' user (UID 1654)
RUN mkdir -p /app/data /app/logs && \
    chown -R app:app /app

# Copy published files
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R app:app /app

# Switch to non-root user (built-in .NET user)
USER app

# Expose port (8080 is the default for .NET in containers)
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__HubDb="Data Source=/app/data/hub.db"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "myIoTGrid.Hub.Api.dll"]
