/**
 * Test specifically for the JWT scope fix
 * This test validates that the backend API scope is included in authentication requests
 */

import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';

describe('JWT Scope Fix Validation', () => {
  const mockClientConfigWithBackendScope = {
    azure: {
      tenantId: 'test-tenant-id',
      clientId: 'test-client-id',
      authority: 'https://login.microsoftonline.com/test-tenant-id',
      redirectUri: 'http://localhost:3000/auth/callback',
      scopes: ['openid', 'profile', 'User.Read', 'email', 'api://backend-client-id/Api.All']
    },
    backend: {
      url: 'http://localhost:8000/api'
    },
    frontend: {
      url: 'http://localhost:3000'
    }
  };

  beforeEach(() => {
    clearCachedClientConfig();
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  it('should include backend API scope in login request when client config is loaded', () => {
    // Set up the client config as if it was loaded from /api/clientConfig
    setCachedClientConfig(mockClientConfigWithBackendScope);

    // Get the login request that would be used for authentication
    const loginReq = getLoginRequest();

    // Verify the backend API scope is included
    expect(loginReq.scopes).toContain('api://backend-client-id/Api.All');
    
    // Verify all expected scopes are present
    expect(loginReq.scopes).toEqual([
      'openid', 
      'profile', 
      'User.Read',
      'email',
      'api://backend-client-id/Api.All'
    ]);
  });

  it('should include backend API scope in token request when client config is loaded', () => {
    // Set up the client config as if it was loaded from /api/clientConfig
    setCachedClientConfig(mockClientConfigWithBackendScope);

    // Get the token request that would be used for silent token acquisition
    const tokenReq = getTokenRequest();

    // Verify the backend API scope is included
    expect(tokenReq.scopes).toContain('api://backend-client-id/Api.All');
    
    // Verify all expected scopes are present (all 5 required scopes)
    expect(tokenReq.scopes).toEqual([
      'openid', 
      'profile', 
      'User.Read',
      'email',
      'api://backend-client-id/Api.All'
    ]);
  });

  it('should demonstrate the fix: backend API scope is now included in JWT token scopes', () => {
    // Before the fix: decoded JWT would only contain "scp": "User.Read profile openid email"
    // After the fix: decoded JWT should contain the backend API scope as well
    
    setCachedClientConfig(mockClientConfigWithBackendScope);
    
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();

    // Both requests should now include the backend API scope
    expect(loginReq.scopes).toContain('api://backend-client-id/Api.All');
    expect(tokenReq.scopes).toContain('api://backend-client-id/Api.All');

    // This means the JWT token obtained using these requests will include 
    // the backend API scope in its "scp" claim
    console.log('✓ Backend API scope will now be included in JWT tokens');
    console.log('✓ Login request scopes:', loginReq.scopes);
    console.log('✓ Token request scopes:', tokenReq.scopes);
  });

  it('should fall back to default scopes when no client config is available', () => {
    // When no client config is cached (e.g., during app initialization)
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();

    // Should use default scopes including email
    expect(loginReq.scopes).toEqual(['openid', 'profile', 'User.Read', 'email']);
    expect(tokenReq.scopes).toEqual(['openid', 'profile', 'User.Read', 'email']);
  });
});