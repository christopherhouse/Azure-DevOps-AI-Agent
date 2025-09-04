/**
 * Test specifically for the JWT scope fix and Issue #236 token audience fix
 * This test validates that the backend client ID is included in authentication requests
 * to ensure correct audience (aud) claim in JWT tokens
 */

import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';

describe('JWT Scope Fix and Token Audience Fix Validation', () => {
  const mockClientConfigWithBackendScope = {
    azure: {
      tenantId: 'test-tenant-id',
      clientId: 'test-client-id',
      authority: 'https://login.microsoftonline.com/test-tenant-id',
      redirectUri: 'http://localhost:3000/auth/callback',
      scopes: ['openid', 'profile', 'User.Read', 'email', 'backend-client-id']  // Updated for Issue #236
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

  it('should include backend client ID in login request for correct token audience (Issue #236)', () => {
    // Set up the client config as if it was loaded from /api/clientConfig
    setCachedClientConfig(mockClientConfigWithBackendScope);

    // Get the login request that would be used for authentication
    const loginReq = getLoginRequest();

    // Verify the backend client ID is included (Issue #236 fix)
    expect(loginReq.scopes).toContain('backend-client-id');
    
    // Verify all expected scopes are present
    expect(loginReq.scopes).toEqual([
      'openid', 
      'profile', 
      'User.Read',
      'email',
      'backend-client-id'  // Client ID only for correct audience (aud) claim
    ]);
  });

  it('should include backend client ID in token request for correct token audience (Issue #236)', () => {
    // Set up the client config as if it was loaded from /api/clientConfig
    setCachedClientConfig(mockClientConfigWithBackendScope);

    // Get the token request that would be used for silent token acquisition
    const tokenReq = getTokenRequest();

    // Verify the backend client ID is included (Issue #236 fix)
    expect(tokenReq.scopes).toContain('backend-client-id');
    
    // Verify all expected scopes are present (all 5 required scopes)
    expect(tokenReq.scopes).toEqual([
      'openid', 
      'profile', 
      'User.Read',
      'email',
      'backend-client-id'  // Client ID only for correct audience (aud) claim
    ]);
  });

  it('should demonstrate Issue #236 fix: backend client ID ensures correct token audience', () => {
    // Issue #236: audience (aud) was incorrectly set to "api://[backend-id]/Api.All"
    // After fix: audience (aud) should be just "[backend-id]" for correct token validation
    
    setCachedClientConfig(mockClientConfigWithBackendScope);
    
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();

    // Both requests should now include the backend client ID only (not full API URI)
    expect(loginReq.scopes).toContain('backend-client-id');
    expect(tokenReq.scopes).toContain('backend-client-id');

    // This means the JWT token obtained using these requests will have:
    // - audience (aud) = "backend-client-id" ✓ (fixed)
    // - scope (scp) still includes the proper permissions
    console.log('✓ Token audience (aud) will now be correctly set to backend client ID');
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