#!/bin/bash
set -e

echo "🏐 Volleyer Stack Deployment"
echo "=========================="

# Ensure we're in the right directory
cd /opt/volleyer/shared/deployment

# Login to registry
echo "$DIGITALOCEAN_ACCESS_TOKEN" | docker login registry.digitalocean.com -u "$DIGITALOCEAN_ACCESS_TOKEN" --password-stdin

# Pull latest images
echo "📦 Pulling latest images..."
docker-compose -f docker-compose.prod.yml pull auth-service events-service

# Update services
echo "🚀 Updating services..."
docker-compose -f docker-compose.prod.yml up -d

# Show status
echo "📊 Stack Status:"
docker-compose -f docker-compose.prod.yml ps

echo "✅ Stack deployment completed!"