# Volleyer Digital Ocean Deployment

This directory contains deployment configurations for deploying the Volleyer microservices to Digital Ocean.

## Architecture

- **Auth Service**: Authentication microservice
- **Events Service**: Events management microservice  
- **Nginx**: Reverse proxy for routing requests
- **Digital Ocean Managed Database**: PostgreSQL database service

## Prerequisites

### Digital Ocean Setup

1. **Digital Ocean Droplet**: Ubuntu server to host the containers
2. **Digital Ocean Container Registry**: To store Docker images
3. **Digital Ocean Managed Database**: PostgreSQL database instances for each service

### GitHub Secrets

Configure these secrets in each repository (auth-service, events-service):

```
DIGITALOCEAN_ACCESS_TOKEN=your_do_access_token
DO_REGISTRY_NAME=your_registry_name
DO_HOST=your_droplet_ip
DO_USERNAME=root
DO_SSH_KEY=your_private_ssh_key
DO_PORT=22
AUTH_DB_CONNECTION_STRING=Host=your-auth-db;Port=25060;Database=volleyer_auth;Username=doadmin;Password=xxx;SslMode=Require
EVENTS_DB_CONNECTION_STRING=Host=your-events-db;Port=25060;Database=volleyer_events;Username=doadmin;Password=xxx;SslMode=Require
JWT_SECRET_KEY=your_jwt_secret_minimum_32_characters
```

## Deployment Process

### 1. Initial Droplet Setup

SSH into your droplet and run:

```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Create Docker network
docker network create volleyer-network

# Create deployment directory
mkdir -p /opt/volleyer
cd /opt/volleyer

# Clone shared repository and navigate to deployment
git clone https://github.com/your-org/shared.git
cd shared/deployment

# Create environment file
cp .env.example .env
# Edit .env with your actual values
```

### 2. Deploy Services

Each service deploys independently via GitHub Actions:

- **Auth Service**: Push to `main` branch in auth-service repository
- **Events Service**: Push to `main` branch in events-service repository

The workflows will:
1. Build and test the .NET application
2. Build Docker image and push to DO Registry
3. SSH to droplet and deploy container
4. Run database migrations
5. Perform health checks

### 3. Deploy Nginx Proxy

After both services are deployed, start the nginx proxy:

```bash
cd /opt/volleyer/shared/deployment

# Start nginx proxy
docker-compose up -d nginx
```

## Service URLs

Once deployed, services are available at:

- **Auth API**: `http://your-domain/api/auth/`
- **Events API**: `http://your-domain/api/events/`
- **Health Checks**: 
  - Auth: `http://your-domain/api/auth/health`
  - Events: `http://your-domain/api/events/health`

## Manual Deployment Commands

### Deploy Individual Service

```bash
# Auth service
docker pull registry.digitalocean.com/your-registry/volleyer-auth:latest
docker stop volleyer-auth || true
docker rm volleyer-auth || true
docker run -d --name volleyer-auth --network volleyer-network \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e "ConnectionStrings__DefaultConnection=$AUTH_DB_CONNECTION_STRING" \
  -e "JWT__SecretKey=$JWT_SECRET_KEY" \
  registry.digitalocean.com/your-registry/volleyer-auth:latest

# Events service  
docker pull registry.digitalocean.com/your-registry/volleyer-events:latest
docker stop volleyer-events || true
docker rm volleyer-events || true
docker run -d --name volleyer-events --network volleyer-network \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e "ConnectionStrings__DefaultConnection=$EVENTS_DB_CONNECTION_STRING" \
  -e "JWT__SecretKey=$JWT_SECRET_KEY" \
  registry.digitalocean.com/your-registry/volleyer-events:latest
```

### Update Nginx

```bash
cd /opt/volleyer/shared/deployment
docker-compose restart nginx
```

### View Logs

```bash
# Service logs
docker logs volleyer-auth
docker logs volleyer-events
docker logs volleyer-nginx

# Combined logs
docker-compose logs -f
```

## Database Migrations

Migrations run automatically during deployment. To run manually:

```bash
# Auth service migrations
docker exec volleyer-auth dotnet ef database update

# Events service migrations  
docker exec volleyer-events dotnet ef database update
```

## Troubleshooting

### Service Not Starting

1. Check container logs: `docker logs volleyer-auth`
2. Verify environment variables are set correctly
3. Check database connectivity
4. Ensure network exists: `docker network ls`

### Health Check Failures

1. Wait 30-60 seconds for services to fully start
2. Check if containers are running: `docker ps`
3. Test health endpoints directly: `docker exec volleyer-auth curl http://localhost:5150/health`

### Database Connection Issues

1. Verify connection string format
2. Check Digital Ocean database firewall rules
3. Ensure SSL mode is set to 'Require' for DO managed databases

## Scaling

To scale services:

```bash
# Run multiple instances (update ports accordingly)
docker run -d --name volleyer-auth-2 --network volleyer-network \
  -e ASPNETCORE_URLS=http://+:5151 \
  registry.digitalocean.com/your-registry/volleyer-auth:latest

# Update nginx upstream configuration to include new instances
```