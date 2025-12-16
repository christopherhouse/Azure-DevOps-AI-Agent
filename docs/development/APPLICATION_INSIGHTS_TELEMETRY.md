# Application Insights Frontend Telemetry Implementation

## Overview

This document describes the Application Insights telemetry implementation for the Azure DevOps AI Agent frontend application. The implementation provides comprehensive tracking of user interactions, API calls, errors, and custom events.

## Configuration

### Environment Variables

The telemetry system requires the following environment variables to be configured:

- `NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING` - The Application Insights connection string
- `NEXT_PUBLIC_ENABLE_TELEMETRY` - Set to `'true'` to enable telemetry

Example:
```bash
NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=your-key;IngestionEndpoint=https://your-region.applicationinsights.azure.com/
NEXT_PUBLIC_ENABLE_TELEMETRY=true
```

### Runtime Configuration

The telemetry system reads configuration from `window.__CLIENT_CONFIG__` which is set by the `ClientLayout` component. The configuration includes:

```typescript
{
  telemetry: {
    connectionString: string;
    enabled: boolean;
  },
  debug: boolean;
}
```

## Features

### Automatic Tracking

The SDK is configured to automatically track:
- Page views and route changes (`enableAutoRouteTracking: true`)
- Page visit time (`autoTrackPageVisitTime: true`)
- AJAX/Fetch requests (`disableFetchTracking: false`, `disableAjaxTracking: false`)
- Unhandled promise rejections (`enableUnhandledPromiseRejectionTracking: true`)
- CORS correlation (`enableCorsCorrelation: true`)
- Request/Response headers (`enableRequestHeaderTracking: true`, `enableResponseHeaderTracking: true`)

### Manual Tracking Functions

#### `initializeTelemetry()`
Initializes the Application Insights SDK. This is automatically called when the application loads.

```typescript
import { initializeTelemetry } from '@/lib/telemetry';

initializeTelemetry();
```

#### `trackPageView(name?, uri?, properties?)`
Tracks a custom page view.

```typescript
import { trackPageView } from '@/lib/telemetry';

trackPageView('Dashboard', '/dashboard', { userId: '123' });
```

#### `trackEvent(eventName, properties?, measurements?)`
Tracks a custom event.

```typescript
import { trackEvent } from '@/lib/telemetry';

trackEvent('ButtonClicked', { buttonName: 'Submit' }, { clicks: 1 });
```

#### `trackAuthEvent(eventName, properties?)`
Tracks authentication-related events with the `Auth_` prefix.

```typescript
import { trackAuthEvent } from '@/lib/telemetry';

trackAuthEvent('login_success', { userId: '123', method: 'oauth' });
```

#### `trackApiCall(method, url, statusCode, duration, success?)`
Tracks API calls as dependencies.

```typescript
import { trackApiCall } from '@/lib/telemetry';

trackApiCall('GET', '/api/users', 200, 150, true);
```

#### `trackException(error, properties?, severityLevel?)`
Tracks exceptions and errors.

```typescript
import { trackException } from '@/lib/telemetry';

try {
  // some code
} catch (error) {
  trackException(error, { context: 'DataFetch' }, 3);
}
```

#### `trackTrace(message, severityLevel?, properties?)`
Tracks trace/log messages.

```typescript
import { trackTrace } from '@/lib/telemetry';

trackTrace('User action completed', 1, { actionType: 'submit' });
```

#### `trackChatMessage(messageType, messageLength?)`
Tracks chat messages.

```typescript
import { trackChatMessage } from '@/lib/telemetry';

trackChatMessage('user', 150);
```

#### `setAuthenticatedUserContext(userId, accountId?)`
Sets the authenticated user context for all telemetry.

```typescript
import { setAuthenticatedUserContext } from '@/lib/telemetry';

setAuthenticatedUserContext('user123', 'account456');
```

#### `clearAuthenticatedUserContext()`
Clears the authenticated user context.

```typescript
import { clearAuthenticatedUserContext } from '@/lib/telemetry';

clearAuthenticatedUserContext();
```

#### `flushTelemetry()`
Manually flushes telemetry data. Useful before page unload.

```typescript
import { flushTelemetry } from '@/lib/telemetry';

window.addEventListener('beforeunload', () => {
  flushTelemetry();
});
```

## Integration Points

### API Client
The `ApiClient` class automatically tracks all API calls using `trackApiCall()` in its response interceptors.

### Authentication Hooks
The `use-auth` hook tracks authentication events:
- `login_attempt`
- `login_success`
- `login_failure`
- `logout`

### MFA Handler
The `mfa-handler` service tracks MFA-related events:
- `mfa_challenge_started`
- `mfa_challenge_completed_silently`
- `mfa_challenge_requires_interaction`
- `mfa_challenge_completed_interactive`
- `mfa_challenge_failed`

### Chat Hook
The `use-chat` hook tracks:
- User messages with `trackChatMessage('user', messageLength)`
- Chat clearing with `trackEvent('ChatCleared')`
- Chat history loading with `trackEvent('ChatHistoryLoaded', { messageCount })`

## Testing

The telemetry implementation includes comprehensive unit tests covering:
- Initialization with various configurations
- All tracking functions
- Error handling and fallbacks
- Server-side rendering safety
- Authenticated user context management

Run tests with:
```bash
npm test -- telemetry.test.ts
```

## Severity Levels

Application Insights supports the following severity levels:

- `0` - Verbose
- `1` - Information
- `2` - Warning
- `3` - Error
- `4` - Critical

## Best Practices

1. **Initialize Early**: The telemetry is automatically initialized in the `page.tsx` component via `useEffect`.

2. **Track User Context**: Set authenticated user context after successful login to correlate telemetry with specific users.

3. **Custom Properties**: Add relevant custom properties to events to enable better filtering and analysis in Application Insights.

4. **Error Handling**: Always track exceptions in catch blocks to monitor application health.

5. **Performance**: API calls are automatically tracked with duration, making it easy to identify slow endpoints.

6. **Privacy**: Be mindful of PII (Personally Identifiable Information) in custom properties and event names.

## Troubleshooting

### Telemetry Not Sending

1. Check that `NEXT_PUBLIC_ENABLE_TELEMETRY` is set to `'true'`
2. Verify the connection string is valid
3. Check browser console for initialization errors
4. Ensure the Application Insights resource exists in Azure

### Missing Events

1. Check that telemetry is initialized before tracking calls
2. Verify the browser has network connectivity
3. Check browser developer tools for network requests to Application Insights endpoints

### Testing Telemetry

During development, enable debug mode by setting `debug: true` in the configuration. This will output telemetry information to the browser console.

## Azure Portal

To view telemetry data:

1. Navigate to your Application Insights resource in the Azure Portal
2. Use the following sections:
   - **Overview**: Key metrics and insights
   - **Live Metrics**: Real-time telemetry stream
   - **Search**: Query specific events
   - **Failures**: View exceptions and failed requests
   - **Performance**: Analyze API call durations
   - **Users**: Track user behavior and sessions
   - **Events**: View custom events

## Future Enhancements

Potential improvements to consider:

1. Add sampling to reduce telemetry volume in high-traffic scenarios
2. Implement custom telemetry initializers for global properties
3. Add distributed tracing with correlation IDs
4. Integrate with Azure Monitor alerts for critical errors
5. Add telemetry for specific user flows (e.g., onboarding, key features)
