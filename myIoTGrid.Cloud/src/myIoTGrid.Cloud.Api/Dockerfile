# =============================================================================
# myIoTGrid Cloud API - Multi-Stage Dockerfile
# =============================================================================
# Optimized for:
# - Azure Container Apps deployment
# - HTTP on Port 8080 (Azure Ingress handles HTTPS externally)
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
# Cloud projects
COPY ["myIoTGrid.Cloud/myIoTGrid.Cloud.sln", "myIoTGrid.Cloud/"]
COPY ["myIoTGrid.Cloud/src/myIoTGrid.Cloud.Api/myIoTGrid.Cloud.Api.csproj", "myIoTGrid.Cloud/src/myIoTGrid.Cloud.Api/"]
COPY ["myIoTGrid.Cloud/src/myIoTGrid.Cloud.Domain/myIoTGrid.Cloud.Domain.csproj", "myIoTGrid.Cloud/src/myIoTGrid.Cloud.Domain/"]
COPY ["myIoTGrid.Cloud/src/myIoTGrid.Cloud.Shared/myIoTGrid.Cloud.Shared.csproj", "myIoTGrid.Cloud/src/myIoTGrid.Cloud.Shared/"]
COPY ["myIoTGrid.Cloud/src/myIoTGrid.Cloud.Service/myIoTGrid.Cloud.Service.csproj", "myIoTGrid.Cloud/src/myIoTGrid.Cloud.Service/"]
COPY ["myIoTGrid.Cloud/src/myIoTGrid.Cloud.Infrastructure/myIoTGrid.Cloud.Infrastructure.csproj", "myIoTGrid.Cloud/src/myIoTGrid.Cloud.Infrastructure/"]
COPY ["myIoTGrid.Cloud/src/myIoTGrid.Cloud.Interface/myIoTGrid.Cloud.Interface.csproj", "myIoTGrid.Cloud/src/myIoTGrid.Cloud.Interface/"]

# Shared projects
COPY ["myIoTGrid.Shared/myIoTGrid.Shared.Common/myIoTGrid.Shared.Common.csproj", "myIoTGrid.Shared/myIoTGrid.Shared.Common/"]
COPY ["myIoTGrid.Shared/myIoTGrid.Shared.Contracts/myIoTGrid.Shared.Contracts.csproj", "myIoTGrid.Shared/myIoTGrid.Shared.Contracts/"]
COPY ["myIoTGrid.Shared/myIoTGrid.Shared.Utilities/myIoTGrid.Shared.Utilities.csproj", "myIoTGrid.Shared/myIoTGrid.Shared.Utilities/"]

# Restore dependencies (cached if .csproj files haven't changed)
RUN dotnet restore "myIoTGrid.Cloud/src/myIoTGrid.Cloud.Api/myIoTGrid.Cloud.Api.csproj"

# Copy everything else and build
COPY ["myIoTGrid.Cloud/", "myIoTGrid.Cloud/"]
COPY ["myIoTGrid.Shared/", "myIoTGrid.Shared/"]
WORKDIR "/src/myIoTGrid.Cloud/src/myIoTGrid.Cloud.Api"
RUN dotnet build "myIoTGrid.Cloud.Api.csproj" -c Release -o /app/build

# -----------------------------------------------------------------------------
# Stage 2: Publish
# -----------------------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "myIoTGrid.Cloud.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Stage 3: Runtime
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Create logs directory
# Note: .NET images already have an 'app' user (UID 1654)
RUN mkdir -p /app/logs && \
    chown -R app:app /app

# Copy published files
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R app:app /app

# Switch to non-root user (built-in .NET user)
USER app

# Expose HTTP ports (8080 for Azure Ingress, 80 for direct HTTP access)
EXPOSE 8080
EXPOSE 80

# Environment variables for Azure Container Apps
# No certificates needed - Azure handles HTTPS termination
ENV ASPNETCORE_URLS=http://+:8080;http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check (HTTP on port 8080)
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "myIoTGrid.Cloud.Api.dll"]
