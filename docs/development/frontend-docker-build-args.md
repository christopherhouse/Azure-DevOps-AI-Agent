# Building Frontend Container with Environment Variables

The frontend Dockerfile now supports build-time arguments for `NEXT_PUBLIC_*` environment variables. This allows you to "bake" environment-specific values directly into the built application at Docker build time.

## Available Build Arguments

The following build arguments are supported:

- `NEXT_PUBLIC_AZURE_TENANT_ID` - Azure tenant ID for authentication
- `NEXT_PUBLIC_AZURE_CLIENT_ID` - Azure client ID for authentication  
- `NEXT_PUBLIC_AZURE_AUTHORITY` - Azure authority URL (optional)
- `NEXT_PUBLIC_AZURE_REDIRECT_URI` - Azure redirect URI (optional)
- `NEXT_PUBLIC_AZURE_SCOPES` - Azure scopes (optional)
- `NEXT_PUBLIC_BACKEND_URL` - Backend API URL
- `NEXT_PUBLIC_FRONTEND_URL` - Frontend application URL
- `NEXT_PUBLIC_ENVIRONMENT` - Environment name (development, staging, production)
- `NEXT_PUBLIC_DEBUG` - Debug mode flag
- `NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING` - Application Insights connection string
- `NEXT_PUBLIC_ENABLE_TELEMETRY` - Enable telemetry flag
- `NEXT_PUBLIC_SESSION_TIMEOUT` - Session timeout in seconds
- `NEXT_PUBLIC_REQUIRE_HTTPS` - Require HTTPS flag

## Build Examples

### Basic Build (Uses Existing Runtime Config Approach)

```bash
docker build -t azure-devops-ai-agent-frontend .
```

When no build args are provided, the application uses the existing runtime configuration approach with build-time placeholders.

### Build with Environment Variables

```bash
docker build \
  --build-arg NEXT_PUBLIC_AZURE_TENANT_ID=your-tenant-id \
  --build-arg NEXT_PUBLIC_AZURE_CLIENT_ID=your-client-id \
  --build-arg NEXT_PUBLIC_BACKEND_URL=https://api.yourdomain.com \
  --build-arg NEXT_PUBLIC_FRONTEND_URL=https://app.yourdomain.com \
  --build-arg NEXT_PUBLIC_ENVIRONMENT=production \
  -t azure-devops-ai-agent-frontend:prod .
```

### Development Build

```bash
docker build \
  --build-arg NEXT_PUBLIC_AZURE_TENANT_ID=dev-tenant-id \
  --build-arg NEXT_PUBLIC_AZURE_CLIENT_ID=dev-client-id \
  --build-arg NEXT_PUBLIC_BACKEND_URL=http://localhost:8000 \
  --build-arg NEXT_PUBLIC_FRONTEND_URL=http://localhost:3000 \
  --build-arg NEXT_PUBLIC_ENVIRONMENT=development \
  --build-arg NEXT_PUBLIC_DEBUG=true \
  -t azure-devops-ai-agent-frontend:dev .
```

### Using Environment Variables for Build Args

You can also use environment variables to pass build args:

```bash
export NEXT_PUBLIC_AZURE_TENANT_ID=your-tenant-id
export NEXT_PUBLIC_AZURE_CLIENT_ID=your-client-id
export NEXT_PUBLIC_BACKEND_URL=https://api.yourdomain.com

docker build \
  --build-arg NEXT_PUBLIC_AZURE_TENANT_ID \
  --build-arg NEXT_PUBLIC_AZURE_CLIENT_ID \
  --build-arg NEXT_PUBLIC_BACKEND_URL \
  -t azure-devops-ai-agent-frontend .
```

## Running the Container

After building with environment variables, simply run the container:

```bash
docker run -p 3000:3000 azure-devops-ai-agent-frontend:prod
```

The environment variables are now baked into the application and don't need to be provided at runtime.

## CI/CD Integration

In CI/CD pipelines, you can pass secrets and environment-specific values as build arguments:

### GitHub Actions Example

```yaml
- name: Build Frontend Image
  run: |
    docker build \
      --build-arg NEXT_PUBLIC_AZURE_TENANT_ID=${{ secrets.AZURE_TENANT_ID }} \
      --build-arg NEXT_PUBLIC_AZURE_CLIENT_ID=${{ secrets.AZURE_CLIENT_ID }} \
      --build-arg NEXT_PUBLIC_BACKEND_URL=${{ vars.BACKEND_URL }} \
      --build-arg NEXT_PUBLIC_FRONTEND_URL=${{ vars.FRONTEND_URL }} \
      --build-arg NEXT_PUBLIC_ENVIRONMENT=production \
      -t ${{ env.REGISTRY }}/frontend:${{ github.sha }} \
      src/frontend
```

### Azure DevOps Pipeline Example

```yaml
- task: Docker@2
  displayName: 'Build Frontend Image'
  inputs:
    command: 'build'
    dockerfile: 'src/frontend/Dockerfile'
    buildContext: 'src/frontend'
    tags: '$(Build.BuildId)'
    arguments: |
      --build-arg NEXT_PUBLIC_AZURE_TENANT_ID=$(AZURE_TENANT_ID)
      --build-arg NEXT_PUBLIC_AZURE_CLIENT_ID=$(AZURE_CLIENT_ID)
      --build-arg NEXT_PUBLIC_BACKEND_URL=$(BACKEND_URL)
      --build-arg NEXT_PUBLIC_FRONTEND_URL=$(FRONTEND_URL)
      --build-arg NEXT_PUBLIC_ENVIRONMENT=production
```

## Benefits

1. **Build-Time Optimization**: Environment variables are inlined into the JavaScript bundle during build, improving runtime performance
2. **Single Image for Environment**: Build once with specific environment values, deploy anywhere
3. **Security**: No need to pass sensitive environment variables at runtime
4. **Consistency**: Ensures the same configuration is used across all instances of the same image
5. **Backward Compatibility**: Still supports the existing runtime configuration approach when no build args are provided

## Notes

- Build arguments take precedence over the runtime configuration approach
- When build args are not provided, the application falls back to the existing runtime config system
- NEXT_PUBLIC_ variables are inlined at build time and cannot be changed at runtime
- For maximum flexibility in multi-environment deployments, consider using the runtime config approach
- This approach is ideal for environment-specific builds in CI/CD pipelines