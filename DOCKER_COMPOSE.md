# Docker Compose Setup Guide

This guide explains how to run the Azure DevOps AI Agent using Docker Compose for local development and testing.

## Overview

Docker Compose orchestrates both the backend (.NET API) and frontend (Next.js) containers, allowing you to run the entire application stack locally with minimal configuration.

## Prerequisites

- Docker Desktop or Docker Engine (20.10.0+)
- Docker Compose (2.0.0+)
- Azure subscription with:
  - Azure OpenAI service
  - Azure DevOps organization
  - Microsoft Entra ID tenant with app registrations

## Quick Start

### 1. Create Environment Files

Copy the example environment files and configure them with your Azure credentials:

```bash
# Copy backend environment file
cp .env.backend.example .env.backend

# Copy frontend environment file
cp .env.frontend.example .env.frontend

# Edit the files with your actual values
```

**Alternatively**, you can use the consolidated Docker example:

```bash
# Use the consolidated example for both
cp .env.docker.example .env.backend
cp .env.docker.example .env.frontend

# Edit both files with your actual values
```

### 2. Configure Required Variables

At minimum, you need to set these variables in both `.env.backend` and `.env.frontend`:

**Backend (.env.backend):**
```bash
# Azure OpenAI (Required)
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4

# Azure DevOps (Required)
AZURE_DEVOPS_ORGANIZATION=https://dev.azure.com/your-org
AZURE_DEVOPS_PAT=your-pat-token

# Entra ID (Required)
AZURE_TENANT_ID=your-tenant-id
BACKEND_CLIENT_ID=your-backend-client-id
AZURE_CLIENT_SECRET=your-client-secret

# Security (Required)
JWT_SECRET_KEY=your-secret-key-min-32-chars
DISABLE_AUTH=true
```

**Frontend (.env.frontend):**
```bash
# Entra ID (Required)
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-frontend-client-id
BACKEND_CLIENT_ID=your-backend-client-id
```

### 3. Start the Services

```bash
# Build and start all services
docker-compose up --build

# Or run in detached mode (background)
docker-compose up --build -d

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f backend
docker-compose logs -f frontend
```

### 4. Access the Application

Once the containers are running:

- **Frontend UI:** http://localhost:3000
- **Backend API:** http://localhost:8000
- **API Documentation:** http://localhost:8000/docs (when `DISABLE_AUTH=true`)
- **Backend Health:** http://localhost:8000/health
- **Frontend Health:** http://localhost:3000/api/health

### 5. Stop the Services

```bash
# Stop and remove containers
docker-compose down

# Stop, remove containers, and remove volumes
docker-compose down -v

# Stop, remove containers, volumes, and images
docker-compose down -v --rmi all
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Docker Host                          │
│                                                              │
│  ┌────────────────────┐         ┌─────────────────────┐    │
│  │  Frontend Container │         │  Backend Container  │    │
│  │                     │         │                     │    │
│  │  Next.js (Node 20)  │────────▶│  .NET 8.0 API      │    │
│  │  Port: 3000         │  HTTP   │  Port: 8000         │    │
│  └────────────────────┘         └─────────────────────┘    │
│           │                              │                  │
│           │                              │                  │
└───────────┼──────────────────────────────┼──────────────────┘
            │                              │
            ▼                              ▼
    Browser Access                 External Services
    localhost:3000                 (Azure OpenAI,
                                    Azure DevOps,
                                    Entra ID)
```

## Service Configuration

### Backend Service

- **Container Name:** `azure-devops-ai-backend`
- **Port Mapping:** `8000:8000`
- **Health Check:** `http://localhost:8000/health`
- **Environment:** Loaded from `.env.backend`
- **Startup:** Runs first, frontend depends on it

### Frontend Service

- **Container Name:** `azure-devops-ai-frontend`
- **Port Mapping:** `3000:3000`
- **Health Check:** `http://0.0.0.0:3000/api/health`
- **Environment:** Loaded from `.env.frontend` + build args
- **Startup:** Waits for backend to be healthy
- **Dependencies:** Requires backend service

### Networking

Both services run on a shared Docker bridge network (`azure-devops-ai-network`), allowing them to communicate using service names:

- Frontend can reach backend at `http://backend:8000` (internal)
- Browser accesses frontend at `http://localhost:3000` (external)
- Browser accesses backend at `http://localhost:8000` (external)

## Environment Variables

### Backend Variables

See `.env.backend.example` for a complete list. Key variables include:

| Variable | Required | Description |
|----------|----------|-------------|
| `AZURE_OPENAI_ENDPOINT` | Yes | Azure OpenAI service endpoint |
| `AZURE_OPENAI_API_KEY` | Yes* | API key (*or use managed identity) |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | Yes | GPT model deployment name |
| `AZURE_DEVOPS_ORGANIZATION` | Yes | Azure DevOps org URL |
| `AZURE_DEVOPS_PAT` | Yes | Personal Access Token |
| `AZURE_TENANT_ID` | Yes | Entra ID tenant ID |
| `BACKEND_CLIENT_ID` | Yes | Backend app registration ID |
| `JWT_SECRET_KEY` | Yes | Secret key (32+ chars) |
| `DISABLE_AUTH` | No | Set `true` for local dev |

### Frontend Variables

See `.env.frontend.example` for a complete list. Key variables include:

| Variable | Required | Description |
|----------|----------|-------------|
| `NEXT_PUBLIC_AZURE_TENANT_ID` | Yes | Entra ID tenant ID |
| `NEXT_PUBLIC_AZURE_CLIENT_ID` | Yes | Frontend app registration ID |
| `NEXT_PUBLIC_BACKEND_CLIENT_ID` | Yes | Backend app registration ID |
| `NEXT_PUBLIC_BACKEND_URL` | No | Backend URL (default: localhost:8000) |
| `NEXT_PUBLIC_AZURE_REDIRECT_URI` | No | OAuth callback (default: localhost:3000/auth/callback) |

**Note:** All `NEXT_PUBLIC_*` variables are exposed to the browser, so never include secrets.

## Development Workflow

### Making Code Changes

When you modify source code:

```bash
# Rebuild only the changed service
docker-compose up --build backend  # For backend changes
docker-compose up --build frontend # For frontend changes

# Or rebuild everything
docker-compose up --build
```

### Debugging

View logs in real-time:

```bash
# All services
docker-compose logs -f

# Backend only
docker-compose logs -f backend

# Frontend only
docker-compose logs -f frontend

# Last 100 lines
docker-compose logs --tail=100 backend
```

Execute commands inside containers:

```bash
# Backend - .NET CLI
docker-compose exec backend bash
docker-compose exec backend dotnet --version

# Frontend - Node/npm
docker-compose exec frontend sh
docker-compose exec frontend node --version
```

### Troubleshooting

**Backend not starting:**
```bash
# Check backend logs
docker-compose logs backend

# Common issues:
# - Missing required environment variables
# - Invalid Azure OpenAI credentials
# - Invalid Azure DevOps PAT
```

**Frontend not starting:**
```bash
# Check frontend logs
docker-compose logs frontend

# Common issues:
# - Backend not healthy (frontend waits for backend)
# - Missing NEXT_PUBLIC_* environment variables
# - Build errors (check if all dependencies installed)
```

**Port already in use:**
```bash
# If port 3000 or 8000 is already in use, modify docker-compose.yml:
# Change port mapping from "3000:3000" to "3001:3000" (or any available port)
```

**Cannot reach backend from frontend:**
```bash
# Verify both services are on the same network
docker-compose ps

# Check network connectivity
docker-compose exec frontend ping backend
```

### Rebuilding from Scratch

```bash
# Stop and remove everything
docker-compose down -v

# Remove Docker images
docker-compose down -v --rmi all

# Clean Docker build cache
docker builder prune -a

# Rebuild and start
docker-compose up --build
```

## Authentication Setup

### Local Development (Simplified)

For local development without authentication:

1. Set `DISABLE_AUTH=true` in `.env.backend`
2. Authentication is bypassed for easier testing
3. Access API docs at http://localhost:8000/docs

### Production Authentication

For production-like authentication setup:

1. Set `DISABLE_AUTH=false` in `.env.backend`
2. Configure Entra ID app registrations:
   - Frontend app: Add redirect URI `http://localhost:3000/auth/callback`
   - Backend app: Expose an API with appropriate scopes
   - Frontend app: Add API permissions to access backend
3. Set all required Entra ID variables in both `.env.backend` and `.env.frontend`

## Performance Optimization

### Build Cache

Docker caches build layers. To ensure faster rebuilds:

```bash
# Use BuildKit for better caching
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1

docker-compose build --parallel
```

### Resource Limits

Add resource limits to `docker-compose.yml` if needed:

```yaml
services:
  backend:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
```

## CI/CD Integration

This Docker Compose setup is intended for local development. For production deployments:

- Use GitHub Actions workflows in `.github/workflows/`
- Deploy to Azure Container Apps
- Use managed identities for Azure service authentication
- Store secrets in Azure Key Vault

## Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [Node.js Docker Images](https://hub.docker.com/_/node)
- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure DevOps REST API](https://learn.microsoft.com/rest/api/azure/devops/)
- [Microsoft Entra ID](https://learn.microsoft.com/entra/identity/)

## Support

For issues and questions:

- **GitHub Issues:** https://github.com/christopherhouse/Azure-DevOps-AI-Agent/issues
- **Documentation:** See `docs/` directory
- **Main README:** See root `README.md`
