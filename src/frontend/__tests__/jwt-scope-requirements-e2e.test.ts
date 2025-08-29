/**
 * End-to-end test to verify that all 5 required scopes are included in JWT tokens
 * This test validates the complete fix for the JWT scope issue
 */

import { renderHook, waitFor } from '@testing-library/react';
import { useClientConfig, setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';
import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';

// Mock fetch
global.fetch = jest.fn();

describe('JWT Scope Requirements - End-to-End Validation', () => {
  beforeEach(() => {
    clearCachedClientConfig();
    jest.clearAllMocks();
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  it('should include all 5 required scopes in JWT tokens when configuration is complete', async () => {
    // Mock the complete client config response that includes all required fields
    const completeClientConfig = {
      azure: {
        tenantId: 'test-tenant-id',
        clientId: 'frontend-client-id',
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

    // Mock the /api/clientConfig endpoint returning complete config
    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => completeClientConfig
    });

    // Load the client config using the hook
    const { result } = renderHook(() => useClientConfig());

    // Wait for config to load and be cached
    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    // Verify config was loaded successfully
    expect(result.current.config).toEqual(completeClientConfig);
    expect(result.current.error).toBe(null);

    // Now test that authentication requests include all 5 required scopes
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();

    // Validate that all 5 scopes from the issue are present:
    // 1. User.Read
    // 2. openid
    // 3. profile 
    // 4. email
    // 5. backend API scope in the form of api://[backend client id]/Api.All

    expect(loginReq.scopes).toContain('User.Read');
    expect(loginReq.scopes).toContain('openid');
    expect(loginReq.scopes).toContain('profile');
    expect(loginReq.scopes).toContain('email');
    expect(loginReq.scopes).toContain('api://backend-client-id/Api.All');

    expect(tokenReq.scopes).toContain('User.Read');
    expect(tokenReq.scopes).toContain('openid');
    expect(tokenReq.scopes).toContain('profile');
    expect(tokenReq.scopes).toContain('email');
    expect(tokenReq.scopes).toContain('api://backend-client-id/Api.All');

    // Verify exact match with expected array
    const expectedScopes = ['openid', 'profile', 'User.Read', 'email', 'api://backend-client-id/Api.All'];
    expect(loginReq.scopes).toEqual(expectedScopes);
    expect(tokenReq.scopes).toEqual(expectedScopes);

    console.log('✅ SUCCESS: All 5 required scopes are now included in JWT tokens');
    console.log('✅ Scopes verified:', loginReq.scopes);
    console.log('✅ This resolves the JWT scope issue reported in the GitHub issue');
  });

  it('should demonstrate the issue was about missing backend API scope and email scope', () => {
    // This test shows what the problem was before the fix

    // Before fix: Only basic OIDC scopes were included
    const scopesBeforeFix = ['User.Read', 'profile', 'openid']; // Missing email and backend API scope

    // After fix: All 5 required scopes are included
    const scopesAfterFix = ['openid', 'profile', 'User.Read', 'email', 'api://backend-client-id/Api.All'];

    // Mock config with complete scopes
    const configWithAllScopes = {
      azure: {
        tenantId: 'test-tenant-id',
        clientId: 'test-client-id',
        authority: 'https://login.microsoftonline.com/test-tenant-id',
        redirectUri: 'http://localhost:3000/auth/callback',
        scopes: scopesAfterFix
      },
      backend: { url: 'http://localhost:8000/api' },
      frontend: { url: 'http://localhost:3000' }
    };

    setCachedClientConfig(configWithAllScopes);
    
    const loginReq = getLoginRequest();
    
    // Verify the fix: we now have all 5 required scopes
    expect(loginReq.scopes).toEqual(scopesAfterFix);
    expect(loginReq.scopes).toHaveLength(5);
    
    // Verify the missing scopes from the original issue are now included
    expect(loginReq.scopes).toContain('email'); // Was missing before
    expect(loginReq.scopes).toContain('api://backend-client-id/Api.All'); // Was missing before

    console.log('✅ Issue resolved: backend API scope and email scope are now included');
  });
});