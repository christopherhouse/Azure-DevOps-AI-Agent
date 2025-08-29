/**
 * Test that validates the fix for the exact issue described in the GitHub issue
 * where JWT tokens were missing the backend API scope
 */

import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';

describe('GitHub Issue JWT Scope Fix Validation', () => {
  beforeEach(() => {
    clearCachedClientConfig();
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  it('should fix the issue: JWT tokens now include all 5 required scopes', () => {
    // Setup: Simulate the environment where BACKEND_CLIENT_ID is available
    const originalEnv = process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID;
    process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID = 'ac2a2313-a766-420c-85a2-afbd65768239';
    
    try {
      // Clear cached config to simulate the scenario where client config loading fails
      // but environment variable is available as fallback
      clearCachedClientConfig();
      
      const loginReq = getLoginRequest();
      const tokenReq = getTokenRequest();
      
      // The issue stated JWT tokens should contain these 5 scopes:
      const requiredScopes = [
        'User.Read',
        'openid', 
        'email',
        'profile',
        'api://ac2a2313-a766-420c-85a2-afbd65768239/Api.All'
      ];
      
      // Verify all required scopes are present in login request
      requiredScopes.forEach(scope => {
        expect(loginReq.scopes).toContain(scope);
      });
      
      // Verify all required scopes are present in token request  
      requiredScopes.forEach(scope => {
        expect(tokenReq.scopes).toContain(scope);
      });
      
      // Verify exact match (order may differ)
      expect(loginReq.scopes).toHaveLength(5);
      expect(tokenReq.scopes).toHaveLength(5);
      
      console.log('✅ FIXED: JWT tokens will now include all 5 required scopes');
      console.log('✅ Before fix: "scp": "User.Read profile openid email"');
      console.log('✅ After fix: "scp": "User.Read profile openid email api://ac2a2313-a766-420c-85a2-afbd65768239/Api.All"');
      console.log('✅ Login request scopes:', loginReq.scopes);
      console.log('✅ Token request scopes:', tokenReq.scopes);
      
    } finally {
      // Restore environment
      if (originalEnv !== undefined) {
        process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID = originalEnv;
      } else {
        delete process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID;
      }
    }
  });

  it('should work with client config when available (primary path)', () => {
    // This tests the primary path where client config loads successfully
    const mockClientConfig = {
      azure: {
        tenantId: '76de2d2d-77f8-438d-9a87-01806f2345da',
        clientId: 'ac2a2313-a766-420c-85a2-afbd65768239',
        authority: 'https://login.microsoftonline.com/76de2d2d-77f8-438d-9a87-01806f2345da',
        redirectUri: 'http://localhost:3000/auth/callback',
        scopes: [
          'openid', 
          'profile', 
          'User.Read', 
          'email', 
          'api://backend-app-client-id/Api.All'
        ]
      },
      backend: {
        url: 'http://localhost:8000/api'
      },
      frontend: {
        url: 'http://localhost:3000'
      }
    };

    setCachedClientConfig(mockClientConfig);
    
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();
    
    // Should use scopes from client config
    expect(loginReq.scopes).toEqual(mockClientConfig.azure.scopes);
    expect(tokenReq.scopes).toEqual(mockClientConfig.azure.scopes);
    
    // Should include backend API scope
    expect(loginReq.scopes).toContain('api://backend-app-client-id/Api.All');
    expect(tokenReq.scopes).toContain('api://backend-app-client-id/Api.All');
  });

  it('should fallback gracefully when no backend client ID is available', () => {
    // Test edge case where neither config nor environment have backend client ID
    const originalEnv = process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID;
    delete process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID;
    delete process.env.BACKEND_CLIENT_ID;
    
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation();
    
    try {
      clearCachedClientConfig();
      
      const loginReq = getLoginRequest();
      
      // Should still work with basic scopes
      expect(loginReq.scopes).toEqual([
        'openid',
        'profile', 
        'User.Read',
        'email'
      ]);
      
      // Should warn about missing backend scope
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('Backend API scope not available')
      );
      
    } finally {
      consoleWarnSpy.mockRestore();
      if (originalEnv !== undefined) {
        process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID = originalEnv;
      }
    }
  });
});