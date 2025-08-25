# Docker Build Fix - Environment Variables

## Issue Resolution: Frontend Build Failure in Container

### Problem
The frontend Docker build was failing with the error:
```
Error: Required environment variable NEXT_PUBLIC_AZURE_TENANT_ID is not set
```

This occurred during the static page generation phase of `npm run build` inside the Docker container.

### Root Cause
- Next.js static site generation (SSG) executes application code at build time
- The configuration validation was happening at module load time
- Docker build environment didn't have the required Azure environment variables
- Build process failed before reaching runtime where variables would be available

### Solution Implemented
The solution uses **deferred configuration loading** with **build-time detection**:

1. **Build-time Detection**: Detects when running in Docker build context vs. runtime
2. **Placeholder Values**: Uses safe placeholder values during build to allow static generation
3. **Runtime Validation**: Validates actual environment variables only at runtime
4. **Cached Loading**: Implements efficient caching for production performance

### Technical Details

#### Configuration Loading Strategy
```typescript
// Build-time detection logic
const isBuildTime = typeof window === 'undefined' && 
                   process.env.NODE_ENV === 'production' && 
                   !process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

if (isBuildTime) {
  // Use placeholder values during Docker build
  config.azure.tenantId = 'build-time-placeholder';
  config.azure.clientId = 'build-time-placeholder';
} else {
  // Runtime validation with actual values
  validateRequiredEnvironmentVariables();
}
```

#### Environment Variables
The following variables are required for **runtime** but not for **build**:

**Required at Runtime:**
- `NEXT_PUBLIC_AZURE_TENANT_ID` - Your Azure tenant ID
- `NEXT_PUBLIC_AZURE_CLIENT_ID` - Your Azure application client ID

**Optional (with defaults):**
- `NEXT_PUBLIC_AZURE_AUTHORITY` - Defaults to `https://login.microsoftonline.com/{tenantId}`
- `NEXT_PUBLIC_AZURE_REDIRECT_URI` - Defaults to `http://localhost:3000/auth/callback`
- `NEXT_PUBLIC_AZURE_SCOPES` - Defaults to `openid,profile,User.Read`

### Docker Build Process
1. **Build Stage**: Uses placeholder values, no environment variables required
2. **Runtime Stage**: Validates actual environment variables when application starts
3. **Deployment**: Environment variables injected via Container Apps environment configuration

### Development vs. Production

#### Local Development
```bash
# Copy environment template
cp .env.example .env.local

# Set actual values
NEXT_PUBLIC_AZURE_TENANT_ID=your-actual-tenant-id
NEXT_PUBLIC_AZURE_CLIENT_ID=your-actual-client-id

# Build and run
npm run build
npm start
```

#### Docker Build
```bash
# Build without environment variables (uses placeholders)
docker build -t frontend .

# Run with environment variables
docker run -e NEXT_PUBLIC_AZURE_TENANT_ID=your-tenant-id \
           -e NEXT_PUBLIC_AZURE_CLIENT_ID=your-client-id \
           frontend
```

#### Container Apps Deployment
Environment variables are injected at runtime via Azure Container Apps configuration:
- Key Vault references for secrets
- Environment-specific configuration
- Managed identity for secure access

### Testing
The solution includes comprehensive tests for:
- Normal configuration loading with environment variables
- Build-time behavior with placeholder values
- Error handling for missing variables at runtime
- Configuration caching and reset functionality

### Benefits
1. **Build Success**: Docker builds complete without requiring sensitive environment variables
2. **Security**: No secrets needed during build process
3. **Flexibility**: Same build artifact works across environments
4. **Performance**: Configuration caching for efficient runtime access
5. **Maintainability**: Clear separation between build-time and runtime requirements

### Migration Notes
- Existing environment variable configuration remains unchanged
- No changes required for local development workflows
- Docker builds now succeed without environment variables
- Runtime behavior preserved for all existing functionality