# =============================================================================
# myIoTGrid Cloud API Dockerfile
# Multi-stage build for .NET 10 with PostgreSQL
# =============================================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY myIoTGrid.Cloud/myIoTGrid.Cloud.sln ./myIoTGrid.Cloud/
COPY myIoTGrid.Cloud/src/myIoTGrid.Cloud.Api/myIoTGrid.Cloud.Api.csproj ./myIoTGrid.Cloud/src/myIoTGrid.Cloud.Api/
COPY myIoTGrid.Cloud/src/myIoTGrid.Cloud.Domain/myIoTGrid.Cloud.Domain.csproj ./myIoTGrid.Cloud/src/myIoTGrid.Cloud.Domain/
COPY myIoTGrid.Cloud/src/myIoTGrid.Cloud.Shared/myIoTGrid.Cloud.Shared.csproj ./myIoTGrid.Cloud/src/myIoTGrid.Cloud.Shared/
COPY myIoTGrid.Cloud/src/myIoTGrid.Cloud.Service/myIoTGrid.Cloud.Service.csproj ./myIoTGrid.Cloud/src/myIoTGrid.Cloud.Service/
COPY myIoTGrid.Cloud/src/myIoTGrid.Cloud.Infrastructure/myIoTGrid.Cloud.Infrastructure.csproj ./myIoTGrid.Cloud/src/myIoTGrid.Cloud.Infrastructure/
COPY myIoTGrid.Cloud/src/myIoTGrid.Cloud.Interface/myIoTGrid.Cloud.Interface.csproj ./myIoTGrid.Cloud/src/myIoTGrid.Cloud.Interface/

# Copy shared project files
COPY myIoTGrid.Shared/myIoTGrid.Shared.Common/myIoTGrid.Shared.Common.csproj ./myIoTGrid.Shared/myIoTGrid.Shared.Common/
COPY myIoTGrid.Shared/myIoTGrid.Shared.Contracts/myIoTGrid.Shared.Contracts.csproj ./myIoTGrid.Shared/myIoTGrid.Shared.Contracts/
COPY myIoTGrid.Shared/myIoTGrid.Shared.Utilities/myIoTGrid.Shared.Utilities.csproj ./myIoTGrid.Shared/myIoTGrid.Shared.Utilities/

# Restore dependencies
WORKDIR /src/myIoTGrid.Cloud
RUN dotnet restore

# Copy all source files
WORKDIR /src
COPY myIoTGrid.Cloud/ ./myIoTGrid.Cloud/
COPY myIoTGrid.Shared/ ./myIoTGrid.Shared/

# Build
WORKDIR /src/myIoTGrid.Cloud/src/myIoTGrid.Cloud.Api
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install useful tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Create directories
RUN mkdir -p /app/logs /app/data && \
    chown -R appuser:appgroup /app

# Copy published app
COPY --from=publish /app/publish .

# Switch to non-root user
USER appuser

# Environment variables
ENV ASPNETCORE_URLS=http://+:5002
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5002/health || exit 1

# Expose port
EXPOSE 5002

# Entry point
ENTRYPOINT ["dotnet", "myIoTGrid.Cloud.Api.dll"]
