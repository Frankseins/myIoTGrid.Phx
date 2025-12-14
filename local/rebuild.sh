#!/usr/bin/env sh

# Usage: call from project root with ./local/rebuild.sh [service-name]
# Examples:
#   ./local/rebuild.sh              # Rebuild and start all services
#   ./local/rebuild.sh hub-api      # Rebuild and start only hub-api
#   ./local/rebuild.sh sensor-sim   # Rebuild and start only sensor-sim

SERVICE_NAME=$1
if [ -z "$SERVICE_NAME" ]; then
  echo "Starting all services..."
  docker compose -f docker-compose.yml -f local/compose-extension.yml up -d --build
else
  echo "Starting service: $SERVICE_NAME"
  docker compose -f ./docker-compose.yml -f local/compose-extension.yml up -d --build "$SERVICE_NAME"
fi
