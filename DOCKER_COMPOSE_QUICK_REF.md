# Docker Compose Quick Reference

## One-Command Setup

```bash
# 1. Copy environment files
cp .env.backend.example .env.backend
cp .env.frontend.example .env.frontend

# 2. Edit .env.backend with your credentials:
#    - AZURE_OPENAI_ENDPOINT
#    - AZURE_OPENAI_API_KEY
#    - AZURE_DEVOPS_ORGANIZATION
#    - AZURE_DEVOPS_PAT
#    - AZURE_TENANT_ID
#    - BACKEND_CLIENT_ID
#    - JWT_SECRET_KEY (generate: openssl rand -hex 32)

# 3. Edit .env.frontend with your credentials:
#    - AZURE_TENANT_ID
#    - AZURE_CLIENT_ID
#    - BACKEND_CLIENT_ID

# 4. Start everything
docker compose up --build
```

## Common Commands

```bash
# Start (build if needed)
docker compose up --build

# Start in background
docker compose up -d

# View logs
docker compose logs -f

# View logs for specific service
docker compose logs -f backend
docker compose logs -f frontend

# Stop services
docker compose down

# Stop and remove volumes
docker compose down -v

# Restart a service
docker compose restart backend
docker compose restart frontend

# Execute command in container
docker compose exec backend bash
docker compose exec frontend sh

# Check service status
docker compose ps

# Validate configuration
docker compose config
```

## Access Points

- Frontend: http://localhost:3000
- Backend API: http://localhost:8000
- Backend Docs: http://localhost:8000/docs (when DISABLE_AUTH=true)
- Backend Health: http://localhost:8000/health
- Frontend Health: http://localhost:3000/api/health

## Minimum Required Configuration

### Backend (.env.backend)
```bash
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4
AZURE_DEVOPS_ORGANIZATION=https://dev.azure.com/your-org
AZURE_DEVOPS_PAT=your-pat
AZURE_TENANT_ID=your-tenant-id
BACKEND_CLIENT_ID=your-backend-client-id
AZURE_CLIENT_SECRET=your-secret
JWT_SECRET_KEY=generate-with-openssl-rand-hex-32
DISABLE_AUTH=true
```

### Frontend (.env.frontend)
```bash
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-frontend-client-id
BACKEND_CLIENT_ID=your-backend-client-id
```

## Troubleshooting

### Port Already in Use
```bash
# Check what's using the port
lsof -i :3000
lsof -i :8000

# Or change ports in docker-compose.yml
ports:
  - "3001:3000"  # Use 3001 instead
```

### Container Won't Start
```bash
# Check logs
docker compose logs backend
docker compose logs frontend

# Rebuild from scratch
docker compose down -v
docker compose build --no-cache
docker compose up
```

### Environment Variables Not Loading
```bash
# Verify files exist
ls -la .env.backend .env.frontend

# Check values are loaded
docker compose config | grep AZURE_TENANT_ID
```

## See Full Documentation

For complete documentation, see [DOCKER_COMPOSE.md](DOCKER_COMPOSE.md)
