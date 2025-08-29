/**
 * Test to validate the comprehensive JWT scope fix
 * This test ensures that all scenarios properly include the backend API scope
 */

import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';

describe('Comprehensive JWT Scope Fix', () => {
  beforeEach(() => {
    clearCachedClientConfig();
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  it('should include backend API scope when BACKEND_CLIENT_ID is available via environment', () => {
    // Test when BACKEND_CLIENT_ID is properly set in environment
    const originalEnv = process.env.BACKEND_CLIENT_ID;
    process.env.BACKEND_CLIENT_ID = 'test-backend-client-id';
    
    try {
      // Simulate no cached config (config load failed) but env var available
      clearCachedClientConfig();
      
      const loginReq = getLoginRequest();
      const tokenReq = getTokenRequest();
      
      // Should include backend API scope from environment fallback
      expect(loginReq.scopes).toContain('api://test-backend-client-id/Api.All');
      expect(tokenReq.scopes).toContain('api://test-backend-client-id/Api.All');
      
      // Should have all 5 required scopes
      expect(loginReq.scopes).toHaveLength(5);
      expect(tokenReq.scopes).toHaveLength(5);
    } finally {
      // Restore original environment
      if (originalEnv !== undefined) {
        process.env.BACKEND_CLIENT_ID = originalEnv;
      } else {
        delete process.env.BACKEND_CLIENT_ID;
      }
    }
  });

  it('should provide fallback scopes that include backend API scope when environment is available', () => {
    // This tests the improved fallback behavior
    const originalEnv = process.env.BACKEND_CLIENT_ID;
    process.env.BACKEND_CLIENT_ID = 'fallback-backend-client-id';
    
    try {
      // No cached config available
      clearCachedClientConfig();
      
      const loginReq = getLoginRequest();
      
      // Verify all required scopes are present including backend API scope
      expect(loginReq.scopes).toEqual([
        'openid',
        'profile', 
        'User.Read',
        'email',
        'api://fallback-backend-client-id/Api.All'
      ]);
    } finally {
      if (originalEnv !== undefined) {
        process.env.BACKEND_CLIENT_ID = originalEnv;
      } else {
        delete process.env.BACKEND_CLIENT_ID;
      }
    }
  });

  it('should warn when no backend client ID is available anywhere', () => {
    // Test the scenario where both config and environment are missing
    const originalEnv = process.env.BACKEND_CLIENT_ID;
    delete process.env.BACKEND_CLIENT_ID;
    
    // Mock console.warn to capture warning
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation();
    
    try {
      clearCachedClientConfig();
      
      const loginReq = getLoginRequest();
      
      // Should still work but with warning
      expect(loginReq.scopes).toEqual([
        'openid',
        'profile',
        'User.Read', 
        'email'
      ]);
      
      // Should have warned about missing backend scope
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('Backend API scope not available')
      );
    } finally {
      consoleWarnSpy.mockRestore();
      if (originalEnv !== undefined) {
        process.env.BACKEND_CLIENT_ID = originalEnv;
      }
    }
  });
});