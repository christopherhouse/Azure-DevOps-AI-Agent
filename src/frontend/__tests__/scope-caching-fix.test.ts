/**
 * Test to verify that client config caching happens immediately 
 * and scopes are available for authentication requests
 */

import { renderHook, waitFor } from '@testing-library/react';
import { useClientConfig, getCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';
import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';
import { createMockClientConfigWithBackendScope } from './test-helpers';

// Mock fetch
global.fetch = jest.fn();

const mockClientConfigWithBackendScope = createMockClientConfigWithBackendScope();

describe('Scope Caching Fix', () => {
  beforeEach(() => {
    clearCachedClientConfig();
    jest.clearAllMocks();
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  it('should cache client config immediately when useClientConfig loads config', async () => {
    // Mock successful config fetch
    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockClientConfigWithBackendScope
    });

    // Initially, no cached config should exist
    expect(getCachedClientConfig()).toBe(null);

    // Use the hook to load config
    const { result } = renderHook(() => useClientConfig());

    // Wait for config to load
    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    // Verify config was loaded successfully
    expect(result.current.config).toEqual(mockClientConfigWithBackendScope);
    expect(result.current.error).toBe(null);

    // Most importantly: verify config was cached immediately
    expect(getCachedClientConfig()).toEqual(mockClientConfigWithBackendScope);
  });

  it('should make scopes available for authentication requests immediately after config loads', async () => {
    // Mock successful config fetch
    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockClientConfigWithBackendScope
    });

    // Use the hook to load config
    const { result } = renderHook(() => useClientConfig());

    // Wait for config to load
    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    // Now authentication requests should include the backend API scope
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();

    expect(loginReq.scopes).toContain('api://backend-client-id/Api.All');
    expect(tokenReq.scopes).toContain('api://backend-client-id/Api.All');
    
    // Verify all 5 expected scopes are present (all required scopes from the issue)
    expect(loginReq.scopes).toEqual(['openid', 'profile', 'User.Read', 'email', 'api://backend-client-id/Api.All']);
    expect(tokenReq.scopes).toEqual(['openid', 'profile', 'User.Read', 'email', 'api://backend-client-id/Api.All']);
  });

  it('should demonstrate the fix: scopes are available immediately after config load without waiting for MSAL initialization', async () => {
    // This test simulates the real-world scenario where authentication might happen
    // before the ClientLayout component finishes initializing MSAL
    
    // Mock successful config fetch
    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockClientConfigWithBackendScope
    });

    // Load config with useClientConfig hook
    const { result } = renderHook(() => useClientConfig());
    
    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    // At this point, cached config should be available and auth requests should work
    expect(getCachedClientConfig()).not.toBe(null);
    
    const loginReq = getLoginRequest();
    expect(loginReq.scopes).toEqual([
      'openid', 
      'profile', 
      'User.Read',
      'email',
      'api://backend-client-id/Api.All'
    ]);

    console.log('✓ Fix verified: Backend API scope is available immediately after config load');
    console.log('✓ Auth request scopes:', loginReq.scopes);
  });
});