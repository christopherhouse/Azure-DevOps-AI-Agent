/**
 * Tests for split token requests functionality (Issue #232)
 * 
 * This test validates the new approach where OIDC profile tokens and backend API tokens
 * are requested separately instead of being combined into one token request.
 */

import { 
  getOidcLoginRequest, 
  getOidcTokenRequest, 
  getBackendApiTokenRequest,
  getLoginRequest,
  getTokenRequest
} from '@/lib/auth-config';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';

describe('Split Token Requests (Issue #232)', () => {
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

  const mockClientConfigOidcOnly = {
    azure: {
      tenantId: 'test-tenant-id',
      clientId: 'test-client-id',
      authority: 'https://login.microsoftonline.com/test-tenant-id',
      redirectUri: 'http://localhost:3000/auth/callback',
      scopes: ['openid', 'profile', 'User.Read', 'email']
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

  describe('OIDC Token Requests', () => {
    it('should return only OIDC scopes for profile information', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);

      const oidcLoginReq = getOidcLoginRequest();
      const oidcTokenReq = getOidcTokenRequest();

      // OIDC requests should only contain profile-related scopes
      const expectedOidcScopes = ['openid', 'profile', 'User.Read', 'email'];
      
      expect(oidcLoginReq.scopes).toEqual(expectedOidcScopes);
      expect(oidcTokenReq.scopes).toEqual(expectedOidcScopes);
      
      // Should NOT contain backend API scope
      expect(oidcLoginReq.scopes).not.toContain('api://backend-client-id/Api.All');
      expect(oidcTokenReq.scopes).not.toContain('api://backend-client-id/Api.All');
    });

    it('should work regardless of client config content', () => {
      setCachedClientConfig(mockClientConfigOidcOnly);

      const oidcLoginReq = getOidcLoginRequest();
      const oidcTokenReq = getOidcTokenRequest();

      // OIDC requests should always return standard profile scopes
      const expectedOidcScopes = ['openid', 'profile', 'User.Read', 'email'];
      
      expect(oidcLoginReq.scopes).toEqual(expectedOidcScopes);
      expect(oidcTokenReq.scopes).toEqual(expectedOidcScopes);
    });
  });

  describe('Backend API Token Requests', () => {
    it('should return only backend API scopes when available', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);

      const backendTokenReq = getBackendApiTokenRequest();

      // Backend token request should only contain API scope
      expect(backendTokenReq.scopes).toEqual(['api://backend-client-id/Api.All']);
      
      // Should NOT contain OIDC scopes
      expect(backendTokenReq.scopes).not.toContain('openid');
      expect(backendTokenReq.scopes).not.toContain('profile');
      expect(backendTokenReq.scopes).not.toContain('User.Read');
      expect(backendTokenReq.scopes).not.toContain('email');
    });

    it('should return empty scopes when no backend API scope is configured', () => {
      setCachedClientConfig(mockClientConfigOidcOnly);

      const backendTokenReq = getBackendApiTokenRequest();

      // Should return empty array when no backend API scopes are available
      expect(backendTokenReq.scopes).toEqual([]);
    });

    it('should handle missing client config gracefully', () => {
      // Don't set any client config
      const backendTokenReq = getBackendApiTokenRequest();

      // Should return empty array when no client config is available
      expect(backendTokenReq.scopes).toEqual([]);
    });
  });

  describe('Backward Compatibility', () => {
    it('should maintain existing behavior for original token requests', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);

      const originalLoginReq = getLoginRequest();
      const originalTokenReq = getTokenRequest();

      // Original requests should still return all scopes as before
      expect(originalLoginReq.scopes).toEqual([
        'openid', 
        'profile', 
        'User.Read', 
        'email', 
        'api://backend-client-id/Api.All'
      ]);
      
      expect(originalTokenReq.scopes).toEqual([
        'openid', 
        'profile', 
        'User.Read', 
        'email', 
        'api://backend-client-id/Api.All'
      ]);
    });
  });

  describe('Token Request Properties', () => {
    it('should set account property correctly for token requests', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);

      const oidcTokenReq = getOidcTokenRequest();
      const backendTokenReq = getBackendApiTokenRequest();
      const originalTokenReq = getTokenRequest();

      // All token requests should have account property set to null initially
      expect(oidcTokenReq.account).toBe(null);
      expect(backendTokenReq.account).toBe(null);
      expect(originalTokenReq.account).toBe(null);
    });

    it('should not have account property for login requests', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);

      const oidcLoginReq = getOidcLoginRequest();
      const originalLoginReq = getLoginRequest();

      // Login requests should not have account property
      expect(oidcLoginReq).not.toHaveProperty('account');
      expect(originalLoginReq).not.toHaveProperty('account');
    });
  });

  describe('Split Token Use Case Validation', () => {
    it('should demonstrate the split: OIDC + Backend API = Original Combined', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);

      const oidcScopes = getOidcTokenRequest().scopes;
      const backendScopes = getBackendApiTokenRequest().scopes;
      const combinedScopes = getTokenRequest().scopes;

      // The combination of OIDC + Backend should equal the original combined scopes
      const splitCombination = [...oidcScopes, ...backendScopes];
      
      expect(splitCombination.sort()).toEqual(combinedScopes.sort());
    });

    it('should show that split requests cover all original scopes without overlap', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);

      const oidcScopes = getOidcTokenRequest().scopes;
      const backendScopes = getBackendApiTokenRequest().scopes;

      // No overlap between OIDC and backend scopes
      const overlap = oidcScopes.filter(scope => backendScopes.includes(scope));
      expect(overlap).toEqual([]);

      // All scopes are covered
      const allSplitScopes = [...oidcScopes, ...backendScopes];
      const originalScopes = getTokenRequest().scopes;
      
      expect(allSplitScopes.sort()).toEqual(originalScopes.sort());
    });
  });
});