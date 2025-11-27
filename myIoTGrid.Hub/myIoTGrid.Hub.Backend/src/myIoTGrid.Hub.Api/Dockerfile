# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first (for better caching)
COPY ["myIoTGrid.Hub.sln", "."]
COPY ["src/myIoTGrid.Hub.Api/myIoTGrid.Hub.Api.csproj", "src/myIoTGrid.Hub.Api/"]
COPY ["src/myIoTGrid.Hub.Domain/myIoTGrid.Hub.Domain.csproj", "src/myIoTGrid.Hub.Domain/"]
COPY ["src/myIoTGrid.Hub.Shared/myIoTGrid.Hub.Shared.csproj", "src/myIoTGrid.Hub.Shared/"]
COPY ["src/myIoTGrid.Hub.Service/myIoTGrid.Hub.Service.csproj", "src/myIoTGrid.Hub.Service/"]
COPY ["src/myIoTGrid.Hub.Infrastructure/myIoTGrid.Hub.Infrastructure.csproj", "src/myIoTGrid.Hub.Infrastructure/"]
COPY ["src/myIoTGrid.Hub.Interface/myIoTGrid.Hub.Interface.csproj", "src/myIoTGrid.Hub.Interface/"]

# Restore dependencies
RUN dotnet restore "src/myIoTGrid.Hub.Api/myIoTGrid.Hub.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/myIoTGrid.Hub.Api"
RUN dotnet build "myIoTGrid.Hub.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "myIoTGrid.Hub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Create data directory
RUN mkdir -p /app/data && chown -R appuser:appuser /app

# Copy published files
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "myIoTGrid.Hub.Api.dll"]
