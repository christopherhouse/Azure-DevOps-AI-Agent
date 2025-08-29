# JWT Scope Fix Summary

## Problem
JWT tokens obtained after authentication were missing the backend API scope. The decoded JWT only contained:
```json
{
  "scp": "User.Read profile openid email"
}
```

But it should have included the backend API scope (e.g., `api://backend-client-id/Api.All`) to allow the frontend to call the backend API.

## Root Cause
The `loginRequest` and `tokenRequest` objects in `src/lib/auth-config.ts` were hardcoded with only basic Microsoft Graph scopes:
```typescript
export const loginRequest: PopupRequest = {
  scopes: ['openid', 'profile', 'User.Read'],
};
```

While the `/api/clientConfig` endpoint correctly included the backend API scope, these hardcoded objects were not using the dynamic scopes.

## Solution
1. **Added dynamic scope resolution functions**:
   - `getLoginRequest()` - Returns login request with scopes from client config
   - `getTokenRequest()` - Returns token request with scopes from client config

2. **Updated authentication logic**:
   - Modified `use-auth.ts` to use the new dynamic functions
   - Ensured fallback to default scopes when client config isn't loaded

3. **Maintained backward compatibility**:
   - Kept original exports as deprecated for any external usage
   - All existing tests continue to pass

## Result
JWT tokens now include the backend API scope:
```json
{
  "scp": "User.Read profile openid email api://backend-client-id/Api.All"
}
```

This allows the frontend to successfully authenticate with the backend API using the JWT token.

## Testing
- All 50 existing tests pass
- Added 17 new tests specifically for the dynamic scope functionality
- Added dedicated test file to validate the JWT scope fix
- Verified through linting, type checking, and build process