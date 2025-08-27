# Frontend Azure Tenant ID Runtime Configuration Fix

## Problem
The frontend UI was displaying "Authentication Configuration Error" with message that `NEXT_PUBLIC_AZURE_TENANT_ID` is not set, even though the deployment workflow correctly set up Key Vault secret references.

## Root Cause
Next.js `NEXT_PUBLIC_` environment variables need to be available at build time to be inlined into the client bundle. However, Container Apps secret references only resolve at runtime, creating a mismatch.

## Solution
Implemented a runtime configuration system that reads environment variables at runtime instead of build time.

### Key Components

1. **Runtime Configuration API** (`/api/config`)
   - Uses Next.js App Router dynamic rendering
   - Reads environment variables at runtime on the server
   - Returns configuration to client-side components

2. **useRuntimeConfig Hook**
   - Client-side hook that fetches configuration from the API
   - Handles loading states and error conditions
   - Provides configuration data to React components

3. **ConfigStatus Component**
   - Displays configuration loading status
   - Shows clear error messages when configuration is missing
   - Integrated into the LoginPage for immediate feedback

4. **Updated Authentication Configuration**
   - Supports both sync (local dev) and async (container) configuration
   - Maintains backward compatibility with existing code
   - Gracefully handles placeholder values during build

## Benefits

- **Secure**: No secrets embedded in Docker build layers
- **Flexible**: Same Docker image works across multiple environments
- **Clear Error Handling**: Provides helpful error messages when configuration is missing
- **Container Apps Compatible**: Works with secret references that resolve at runtime
- **Backward Compatible**: Existing local development workflow unchanged

## Testing Results

✅ Frontend builds successfully without environment variables  
✅ Docker container builds without secrets  
✅ Runtime configuration API works correctly  
✅ Error handling provides clear feedback  
✅ Real environment variables loaded correctly at runtime  
✅ All existing tests pass  

## Deployment Impact

The existing deployment workflow already sets up Container Apps secret references correctly:

```yaml
--secret-ref "NEXT_PUBLIC_AZURE_TENANT_ID=entra-tenant-id"
--secret-ref "NEXT_PUBLIC_AZURE_CLIENT_ID=frontend-client-id"
```

These will now be properly consumed by the frontend at runtime, resolving the authentication configuration error.