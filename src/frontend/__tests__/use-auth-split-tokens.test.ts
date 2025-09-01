/**
 * Tests for the enhanced auth hook with split token acquisition methods (Issue #232)
 */

import { renderHook, act } from '@testing-library/react';
import { useAuth } from '@/hooks/use-auth';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';

// Mock MSAL React
const mockInstance = {
  acquireTokenSilent: jest.fn(),
  loginPopup: jest.fn(),
  logoutPopup: jest.fn(),
};

const mockAccount = {
  homeAccountId: 'test-account-id',
  username: 'test@example.com',
  name: 'Test User',
  tenantId: 'test-tenant',
};

jest.mock('@azure/msal-react', () => ({
  useMsal: () => ({
    instance: mockInstance,
    accounts: [mockAccount],
    inProgress: 'None',
  }),
  useAccount: () => mockAccount,
}));

// Mock telemetry - create a manual mock
jest.mock('@/lib/telemetry', () => ({
  trackAuthEvent: jest.fn(),
  trackApiCall: jest.fn(),
  trackException: jest.fn(),
}), { virtual: true });

// Mock API client - create a manual mock
jest.mock('@/services/api-client', () => ({
  apiClient: {
    setAccessToken: jest.fn(),
  },
}), { virtual: true });

describe('useAuth Hook - Split Token Methods (Issue #232)', () => {
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
    jest.clearAllMocks();
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  describe('getOidcToken method', () => {
    it('should acquire OIDC token with profile scopes only', async () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      mockInstance.acquireTokenSilent.mockResolvedValue({
        accessToken: 'oidc-access-token'
      });

      const { result } = renderHook(() => useAuth());

      // Wait for initial auth state to settle
      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
      });

      let oidcToken;
      await act(async () => {
        oidcToken = await result.current.getOidcToken();
      });

      expect(oidcToken).toBe('oidc-access-token');
      expect(mockInstance.acquireTokenSilent).toHaveBeenCalledWith({
        scopes: ['openid', 'profile', 'User.Read', 'email'],
        account: mockAccount,
      });
    });

    it('should handle OIDC token acquisition failure gracefully', async () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      mockInstance.acquireTokenSilent.mockRejectedValue(new Error('Token acquisition failed'));

      const { result } = renderHook(() => useAuth());

      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
      });

      let oidcToken;
      await act(async () => {
        oidcToken = await result.current.getOidcToken();
      });

      expect(oidcToken).toBe(null);
    });
  });

  describe('getBackendApiToken method', () => {
    it('should acquire backend API token with API scopes only', async () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      mockInstance.acquireTokenSilent.mockResolvedValue({
        accessToken: 'backend-api-access-token'
      });

      const { result } = renderHook(() => useAuth());

      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
      });

      let backendToken;
      await act(async () => {
        backendToken = await result.current.getBackendApiToken();
      });

      expect(backendToken).toBe('backend-api-access-token');
      expect(mockInstance.acquireTokenSilent).toHaveBeenCalledWith({
        scopes: ['api://backend-client-id/Api.All'],
        account: mockAccount,
      });
    });

    it('should return null when no backend API scopes are available', async () => {
      setCachedClientConfig(mockClientConfigOidcOnly);

      const { result } = renderHook(() => useAuth());

      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
      });

      let backendToken;
      await act(async () => {
        backendToken = await result.current.getBackendApiToken();
      });

      expect(backendToken).toBe(null);
      // Note: acquireTokenSilent might still be called during auth initialization for the main token,
      // but the backend-specific token request should not be made
    });

    it('should handle backend API token acquisition failure gracefully', async () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      mockInstance.acquireTokenSilent.mockRejectedValue(new Error('API token acquisition failed'));

      const { result } = renderHook(() => useAuth());

      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
      });

      let backendToken;
      await act(async () => {
        backendToken = await result.current.getBackendApiToken();
      });

      expect(backendToken).toBe(null);
    });
  });

  describe('Backward compatibility', () => {
    it('should maintain existing getAccessToken method behavior', async () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      mockInstance.acquireTokenSilent.mockResolvedValue({
        accessToken: 'combined-access-token'
      });

      const { result } = renderHook(() => useAuth());

      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
      });

      let accessToken;
      await act(async () => {
        accessToken = await result.current.getAccessToken();
      });

      expect(accessToken).toBe('combined-access-token');
      // Should be called with combined scopes as before
      expect(mockInstance.acquireTokenSilent).toHaveBeenCalledWith({
        scopes: ['openid', 'profile', 'User.Read', 'email', 'api://backend-client-id/Api.All'],
        account: mockAccount,
      });
    });

    it('should maintain all existing auth hook properties', () => {
      const { result } = renderHook(() => useAuth());

      // Check that all original properties still exist
      expect(result.current).toHaveProperty('isAuthenticated');
      expect(result.current).toHaveProperty('user');
      expect(result.current).toHaveProperty('accessToken');
      expect(result.current).toHaveProperty('error');
      expect(result.current).toHaveProperty('isLoading');
      expect(result.current).toHaveProperty('login');
      expect(result.current).toHaveProperty('logout');
      expect(result.current).toHaveProperty('getAccessToken');

      // Check that new methods are added
      expect(result.current).toHaveProperty('getOidcToken');
      expect(result.current).toHaveProperty('getBackendApiToken');
    });
  });

  describe('Integration scenarios', () => {
    it('should allow acquiring both OIDC and backend tokens separately', async () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      // Clear any previous calls
      mockInstance.acquireTokenSilent.mockClear();
      
      // Setup different responses for different token requests
      mockInstance.acquireTokenSilent
        .mockResolvedValueOnce({ accessToken: 'main-token' }) // For initialization
        .mockResolvedValueOnce({ accessToken: 'oidc-token' })  // For OIDC request
        .mockResolvedValueOnce({ accessToken: 'backend-api-token' }); // For backend request

      const { result } = renderHook(() => useAuth());

      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
      });

      let oidcToken, backendToken;
      await act(async () => {
        oidcToken = await result.current.getOidcToken();
        backendToken = await result.current.getBackendApiToken();
      });

      expect(oidcToken).toBe('oidc-token');
      expect(backendToken).toBe('backend-api-token');
      // Should be called at least 2 times (for oidc and backend tokens)
      expect(mockInstance.acquireTokenSilent).toHaveBeenCalledTimes(3); // initialization + oidc + backend
    });
  });
});