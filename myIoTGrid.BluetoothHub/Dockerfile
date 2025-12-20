FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy AS base
WORKDIR /app

# Install Bluetooth dependencies
RUN apt-get update && apt-get install -y \
    bluetooth \
    bluez \
    libbluetooth-dev \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/myIoTGrid.BluetoothHub/myIoTGrid.BluetoothHub.csproj", "src/myIoTGrid.BluetoothHub/"]
RUN dotnet restore "src/myIoTGrid.BluetoothHub/myIoTGrid.BluetoothHub.csproj"
COPY . .
WORKDIR "/src/src/myIoTGrid.BluetoothHub"
RUN dotnet build "myIoTGrid.BluetoothHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "myIoTGrid.BluetoothHub.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "myIoTGrid.BluetoothHub.dll"]
