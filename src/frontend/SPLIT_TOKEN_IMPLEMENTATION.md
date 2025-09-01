# Split Token Requests Implementation (Issue #232)

This document explains the new split token request functionality that separates OIDC profile tokens from backend API tokens.

## Overview

Previously, the frontend requested a single token that included both OIDC profile scopes and backend API scopes:
- `['openid', 'profile', 'User.Read', 'email', 'api://backend-client-id/Api.All']`

Now, you can request these tokens separately:
1. **OIDC Profile Token**: `['openid', 'profile', 'User.Read', 'email']`
2. **Backend API Token**: `['api://backend-client-id/Api.All']`

## New Functions

### Auth Configuration Functions

```typescript
import { 
  getOidcLoginRequest, 
  getOidcTokenRequest, 
  getBackendApiTokenRequest 
} from '@/lib/auth-config';

// OIDC profile requests (for user information)
const oidcLoginReq = getOidcLoginRequest();
const oidcTokenReq = getOidcTokenRequest();

// Backend API requests (for API access)
const backendTokenReq = getBackendApiTokenRequest();
```

### Auth Hook Methods

```typescript
import { useAuth } from '@/hooks/use-auth';

function MyComponent() {
  const auth = useAuth();

  const handleSeparateTokens = async () => {
    // Get OIDC profile token (for user info)
    const oidcToken = await auth.getOidcToken();
    
    // Get backend API token (for API calls)
    const backendToken = await auth.getBackendApiToken();
    
    console.log('OIDC Token:', oidcToken);
    console.log('Backend Token:', backendToken);
  };

  return (
    <button onClick={handleSeparateTokens}>
      Get Separate Tokens
    </button>
  );
}
```

## Usage Examples

### Example 1: Using OIDC Token for User Profile

```typescript
import { useAuth } from '@/hooks/use-auth';

function UserProfile() {
  const auth = useAuth();

  const loadUserProfile = async () => {
    // Get token with OIDC scopes only
    const oidcToken = await auth.getOidcToken();
    
    if (oidcToken) {
      // Use this token for Microsoft Graph API calls
      const response = await fetch('https://graph.microsoft.com/v1.0/me', {
        headers: {
          'Authorization': `Bearer ${oidcToken}`
        }
      });
      
      const userProfile = await response.json();
      console.log('User profile:', userProfile);
    }
  };

  return <button onClick={loadUserProfile}>Load Profile</button>;
}
```

### Example 2: Using Backend API Token for API Calls

```typescript
import { useAuth } from '@/hooks/use-auth';

function ChatInterface() {
  const auth = useAuth();

  const sendMessage = async (message: string) => {
    // Get token with backend API scope only
    const backendToken = await auth.getBackendApiToken();
    
    if (backendToken) {
      // Use this token for backend API calls
      const response = await fetch('/api/chat/message', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${backendToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ message })
      });
      
      const result = await response.json();
      return result;
    }
  };

  return (
    <button onClick={() => sendMessage('Hello')}>
      Send Message
    </button>
  );
}
```

### Example 3: Combined Usage

```typescript
import { useAuth } from '@/hooks/use-auth';

function CombinedExample() {
  const auth = useAuth();

  const handleBothTokens = async () => {
    // Get both tokens separately
    const [oidcToken, backendToken] = await Promise.all([
      auth.getOidcToken(),
      auth.getBackendApiToken()
    ]);

    // Use OIDC token for user profile
    if (oidcToken) {
      const userResponse = await fetch('https://graph.microsoft.com/v1.0/me', {
        headers: { 'Authorization': `Bearer ${oidcToken}` }
      });
      console.log('User:', await userResponse.json());
    }

    // Use backend token for API calls
    if (backendToken) {
      const apiResponse = await fetch('/api/projects', {
        headers: { 'Authorization': `Bearer ${backendToken}` }
      });
      console.log('Projects:', await apiResponse.json());
    }
  };

  return <button onClick={handleBothTokens}>Use Both Tokens</button>;
}
```

## Backward Compatibility

All existing code continues to work unchanged:

```typescript
import { useAuth } from '@/hooks/use-auth';

function ExistingComponent() {
  const auth = useAuth();

  const existingMethod = async () => {
    // This still works and returns the combined token
    const token = await auth.getAccessToken();
    
    // This token includes both OIDC and backend API scopes
    console.log('Combined token:', token);
  };

  return <button onClick={existingMethod}>Get Combined Token</button>;
}
```

## Token Scope Details

### OIDC Profile Token Scopes
- `openid`: Basic OpenID Connect identity
- `profile`: User profile information
- `User.Read`: Microsoft Graph user read permission
- `email`: User email address

### Backend API Token Scopes
- `api://{backend-client-id}/Api.All`: Full access to the backend API

### Combined Token Scopes (existing behavior)
- All of the above scopes in a single token

## Configuration Requirements

The split token functionality automatically works with your existing client configuration. The backend API scope is determined from the `BACKEND_CLIENT_ID` environment variable configured in your client config endpoint.

No additional configuration is required - the new methods will automatically filter the appropriate scopes from your existing configuration.

## Error Handling

```typescript
import { useAuth } from '@/hooks/use-auth';

function ErrorHandlingExample() {
  const auth = useAuth();

  const safeTokenUsage = async () => {
    try {
      const oidcToken = await auth.getOidcToken();
      const backendToken = await auth.getBackendApiToken();

      if (!oidcToken) {
        console.warn('OIDC token not available');
      }

      if (!backendToken) {
        console.warn('Backend API token not available (no backend scopes configured)');
      }

      // Proceed with available tokens
      if (oidcToken) {
        // Use OIDC token
      }

      if (backendToken) {
        // Use backend token
      }
    } catch (error) {
      console.error('Token acquisition failed:', error);
    }
  };

  return <button onClick={safeTokenUsage}>Safe Token Usage</button>;
}
```

## Benefits of Split Tokens

1. **Security**: Minimize scope exposure - each token only has the permissions it needs
2. **Performance**: Smaller tokens for specific use cases
3. **Clarity**: Clear separation between user profile access and API access
4. **Flexibility**: Easier to manage different token lifetimes and refresh strategies
5. **Standards Compliance**: Follows OAuth2 best practices for scope minimization

## Migration Guide

If you want to migrate from combined tokens to split tokens:

1. **Identify usage patterns**: Determine which parts of your code need user profile info vs API access
2. **Update gradually**: Replace `auth.getAccessToken()` calls with specific token methods
3. **Test thoroughly**: Ensure both token types work correctly in your environment
4. **No breaking changes**: Existing code continues to work during migration