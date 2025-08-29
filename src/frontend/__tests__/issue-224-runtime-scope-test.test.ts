/**
 * Test that reproduces the exact runtime issue described in GitHub Issue #224
 * 
 * Issue: When the frontend requests a token, it's only including 
 * openid, profile, User.Read and email instead of all scopes from /api/clientConfig
 */

import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';

describe('Issue #224: Runtime Scope Problem Reproduction', () => {
  beforeEach(() => {
    clearCachedClientConfig();
    // Clear any environment variables that might interfere
    delete process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID;
    delete process.env.BACKEND_CLIENT_ID;
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  it('should reproduce the issue: scopes from /api/clientConfig are not being used', () => {
    // This reproduces the exact scenario from the issue
    // The /api/clientConfig returns the correct JSON with all 5 scopes
    const correctClientConfigFromApi = {
      "azure": {
        "tenantId": "76de2d2d-77f8-438d-9a87-01806f2345da",
        "clientId": "ac2a2313-a766-420c-85a2-afbd65768239",
        "authority": "https://login.microsoftonline.com/76de2d2d-77f8-438d-9a87-01806f2345da",
        "redirectUri": "https://azdo-ai-agent-dev-frontend.kindflower-ab69ae39.eastus2.azurecontainerapps.io/auth/callback",
        "scopes": [
          "openid",
          "profile",
          "User.Read",
          "email", 
          "api://083a95bb-15c4-4547-89ba-41ea1fbcc64f/Api.All"
        ]
      },
      "backend": {
        "url": "https://azdo-ai-agent-dev-backend.kindflower-ab69ae39.eastus2.azurecontainerapps.io/api"
      },
      "frontend": {
        "url": "https://azdo-ai-agent-dev-frontend.kindflower-ab69ae39.eastus2.azurecontainerapps.io"
      }
    };

    // Simulate the client config being loaded and cached
    setCachedClientConfig(correctClientConfigFromApi);

    // Now when requesting tokens, they should include ALL scopes from the config
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();

    console.log('🔍 Testing issue #224 reproduction:');
    console.log('✅ Client config loaded with scopes:', correctClientConfigFromApi.azure.scopes);
    console.log('❓ Login request scopes:', loginReq.scopes);
    console.log('❓ Token request scopes:', tokenReq.scopes);

    // The issue claims that only 4 scopes are being requested instead of 5
    // If this test fails, it means the issue is not reproduced in tests
    // But the user says it happens at runtime

    // This SHOULD use all 5 scopes from the client config directly
    expect(loginReq.scopes).toEqual(correctClientConfigFromApi.azure.scopes);
    expect(tokenReq.scopes).toEqual(correctClientConfigFromApi.azure.scopes);
    
    // Verify all 5 scopes are present
    expect(loginReq.scopes).toHaveLength(5);
    expect(tokenReq.scopes).toHaveLength(5);
    
    // Specifically check for the backend API scope that's allegedly missing
    expect(loginReq.scopes).toContain('api://083a95bb-15c4-4547-89ba-41ea1fbcc64f/Api.All');
    expect(tokenReq.scopes).toContain('api://083a95bb-15c4-4547-89ba-41ea1fbcc64f/Api.All');
  });

  it('should demonstrate the actual problem: fallback behavior when config is not cached yet', () => {
    // This might be the real issue - what happens when getScopes() is called
    // BEFORE the client config is cached?
    
    // Clear the cache to simulate the timing issue
    clearCachedClientConfig();
    
    // Now try to get token requests without cached config
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();
    
    console.log('🔍 Testing timing issue scenario:');
    console.log('❌ No cached client config available');
    console.log('❓ Login request scopes:', loginReq.scopes);
    console.log('❓ Token request scopes:', tokenReq.scopes);
    
    // This will likely fall back to default scopes without backend API scope
    // because there's no BACKEND_CLIENT_ID environment variable in the test
    expect(loginReq.scopes).toEqual(['openid', 'profile', 'User.Read', 'email']);
    expect(tokenReq.scopes).toEqual(['openid', 'profile', 'User.Read', 'email']);
    
    // This is probably what's happening at runtime - a timing issue where
    // authentication happens before client config is cached
    console.log('💡 This might be the actual issue: authentication before config caching');
  });

  it('should test the fix: ensure config is used even with environment fallback', () => {
    // Test the scenario where we have both environment variable AND client config
    // The client config should take priority
    
    // Set up environment variable (fallback)
    process.env.NEXT_PUBLIC_BACKEND_CLIENT_ID = 'fallback-client-id';
    
    // But also have client config (should take priority)
    const clientConfig = {
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
          'api://083a95bb-15c4-4547-89ba-41ea1fbcc64f/Api.All'  // This should be used, not the fallback
        ]
      },
      backend: {
        url: 'http://localhost:8000/api'
      },
      frontend: {
        url: 'http://localhost:3000'
      }
    };
    
    setCachedClientConfig(clientConfig);
    
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();
    
    // Should use client config scopes, not construct from environment variable
    expect(loginReq.scopes).toEqual(clientConfig.azure.scopes);
    expect(tokenReq.scopes).toEqual(clientConfig.azure.scopes);
    
    // Should NOT contain the fallback scope
    expect(loginReq.scopes).not.toContain('api://fallback-client-id/Api.All');
    expect(tokenReq.scopes).not.toContain('api://fallback-client-id/Api.All');
    
    // Should contain the correct scope from config
    expect(loginReq.scopes).toContain('api://083a95bb-15c4-4547-89ba-41ea1fbcc64f/Api.All');
    expect(tokenReq.scopes).toContain('api://083a95bb-15c4-4547-89ba-41ea1fbcc64f/Api.All');
  });
});