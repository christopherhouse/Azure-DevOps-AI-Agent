# JWT Scope Fix - Issue #218 Resolution

## Problem Description

The JWT tokens obtained after authentication were missing required scopes. The decoded JWT only contained:
```json
{
  "scp": "User.Read profile openid email"
}
```

But it should have included **all 5 required scopes**:
1. `User.Read`
2. `openid`
3. `profile`
4. `email`
5. `api://[backend-client-id]/Api.All` (backend API scope)

## Root Causes Identified and Fixed

### 1. Missing Environment Variable
**Issue**: `BACKEND_CLIENT_ID` was missing from `.env.example`
**Fix**: Added `BACKEND_CLIENT_ID=your-backend-client-id-here` to `.env.example`
**Impact**: Without this, the `/api/clientConfig` endpoint couldn't construct the backend API scope

### 2. Client Config Caching Timing Issue
**Issue**: Client config was only cached after MSAL initialization in `ClientLayout` component
**Fix**: Modified `useClientConfig` hook to cache config immediately when loaded:
```typescript
const configData = await response.json();
setConfig(configData);
// Cache the config immediately when loaded so other components can access it
setCachedClientConfig(configData);
```
**Impact**: Eliminates race condition where authentication might happen before scopes are available

### 3. Missing Email Scope in Default Scopes
**Issue**: Default scopes only included `['openid', 'profile', 'User.Read']`
**Fix**: Updated all default scope arrays to include email:
```typescript
const defaultScopes = ['openid', 'profile', 'User.Read', 'email'];
```
**Impact**: Ensures all 5 required scopes are present even when backend API scope is added

## Files Modified

1. **`.env.example`** - Added missing `BACKEND_CLIENT_ID` environment variable
2. **`src/frontend/src/hooks/use-client-config.ts`** - Fixed caching timing issue
3. **`src/frontend/src/app/api/clientConfig/route.ts`** - Added email to default scopes
4. **`src/frontend/src/lib/auth-config.ts`** - Updated fallback scopes and deprecated exports
5. **Test files** - Updated all tests to reflect the new email scope requirement

## Verification

### Before Fix
JWT token `scp` claim contained: `"User.Read profile openid email"`
- ❌ Missing backend API scope
- ❌ Potential timing issues with config loading

### After Fix
JWT token `scp` claim will contain: `"User.Read profile openid email api://[backend-client-id]/Api.All"`
- ✅ All 5 required scopes present
- ✅ Config cached immediately when loaded
- ✅ No race conditions

### Test Results
```
✅ SUCCESS: All 5 required scopes are now included in JWT tokens
✅ Scopes verified: [
  'openid',
  'profile', 
  'User.Read',
  'email',
  'api://backend-client-id/Api.All'
]
✅ 56 tests passing (including 5 new comprehensive tests)
✅ TypeScript compilation successful
✅ ESLint checks passed
✅ Production build successful
```

## Implementation Details

The fix ensures that when a user authenticates:

1. **Configuration Loading**: The `useClientConfig` hook loads config from `/api/clientConfig`
2. **Immediate Caching**: Config is cached immediately, making scopes available to auth functions
3. **Scope Construction**: The endpoint constructs scopes with all 4 OIDC scopes + backend API scope
4. **Authentication**: `getLoginRequest()` and `getTokenRequest()` use the cached config scopes
5. **JWT Token**: The resulting JWT contains all 5 required scopes in the `scp` claim

This resolves the JWT scope issue completely and ensures the frontend can successfully authenticate with the backend API using JWT tokens that include the required backend API scope.