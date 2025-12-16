# Application Insights Frontend Fix

## Problem

Application Insights telemetry was not working in the frontend container because `window.__CLIENT_CONFIG__` was undefined. This prevented the telemetry library from accessing the Application Insights connection string.

## Root Cause

There was a race condition in the initialization sequence:

1. The `Home` component (in `src/frontend/src/app/page.tsx`) rendered
2. The component's `useEffect` hook immediately called `initializeTelemetry()`
3. At this point, `ClientLayout` hadn't finished fetching the configuration yet
4. `initializeTelemetry()` called `getAppInsights()` which checked for `window.__CLIENT_CONFIG__`
5. Since the config wasn't set yet, it returned `null` and logged a warning
6. The Application Insights instance was cached as `null`
7. Later, when `ClientLayout` finished loading and set `window.__CLIENT_CONFIG__`, it was too late - telemetry was already initialized as `null`

## Solution

The fix involves two changes:

### 1. Move Telemetry Initialization to ClientLayout

Modified `src/frontend/src/components/ClientLayout.tsx` to initialize telemetry AFTER setting `window.__CLIENT_CONFIG__`:

```typescript
// Set telemetry configuration on window object for telemetry library
if (typeof window !== 'undefined') {
  window.__CLIENT_CONFIG__ = {
    telemetry: {
      connectionString: clientConfig.telemetry.connectionString,
      enabled: clientConfig.telemetry.enabled,
    },
    debug: clientConfig.debug,
  };

  // Initialize telemetry after config is set
  // Dynamic import to avoid circular dependencies
  import('@/lib/telemetry').then(({ initializeTelemetry }) => {
    initializeTelemetry();
  });
}
```

### 2. Remove Premature Initialization from Home Component

Removed the `useEffect` hook from `src/frontend/src/app/page.tsx` that was calling `initializeTelemetry()` too early:

```typescript
// Before (INCORRECT)
export default function Home() {
  useEffect(() => {
    // Initialize Application Insights on client side
    initializeTelemetry();
  }, []);

  return (
    <ClientLayout>
      <Layout />
    </ClientLayout>
  );
}

// After (CORRECT)
export default function Home() {
  return (
    <ClientLayout>
      <Layout />
    </ClientLayout>
  );
}
```

## How It Works Now

The correct initialization sequence is:

1. `Home` component renders
2. `ClientLayout` starts rendering
3. `ClientLayout`'s `useEffect` hook runs
4. Config is fetched from `/api/clientConfig` endpoint
5. `window.__CLIENT_CONFIG__` is set with the telemetry configuration
6. Telemetry is initialized with the correct configuration
7. Application Insights is now properly configured and tracking events

## Verification

The fix has been verified to:

1. ✅ Pass all 133 frontend tests
2. ✅ Pass ESLint validation
3. ✅ Build successfully in Docker container
4. ✅ Correctly expose the Application Insights connection string via `/api/clientConfig`
5. ✅ Initialize telemetry after configuration is loaded

## Environment Variables Required

For Application Insights to work in production, the following environment variables must be set in the Azure Container App:

- `APPLICATIONINSIGHTS_CONNECTION_STRING` - The full Application Insights connection string
- `ENABLE_TELEMETRY=true` - Enable telemetry collection
- `DEBUG=false` - Set to false in production (true for development)

These are already configured in the deployment workflow (`.github/workflows/deploy.yml` lines 389-391).

## Testing

To test locally with Docker:

```bash
# Build the frontend container
cd src/frontend
docker build -t frontend-test .

# Run with environment variables
docker run -p 3000:3000 \
  -e AZURE_TENANT_ID=your-tenant-id \
  -e AZURE_CLIENT_ID=your-client-id \
  -e BACKEND_CLIENT_ID=your-backend-client-id \
  -e BACKEND_URL=http://backend:8000 \
  -e FRONTEND_URL=http://localhost:3000 \
  -e APPLICATIONINSIGHTS_CONNECTION_STRING="your-connection-string" \
  -e ENABLE_TELEMETRY=true \
  -e DEBUG=true \
  frontend-test

# Verify the config endpoint returns telemetry configuration
curl http://localhost:3000/api/clientConfig | jq .telemetry
```

## Related Files

- `src/frontend/src/app/page.tsx` - Removed premature telemetry initialization
- `src/frontend/src/components/ClientLayout.tsx` - Added telemetry initialization after config is set
- `src/frontend/src/lib/telemetry.ts` - Telemetry library (unchanged)
- `src/frontend/src/app/api/clientConfig/route.ts` - API endpoint that provides config (unchanged)
- `.github/workflows/deploy.yml` - Deployment workflow that sets environment variables (unchanged)
