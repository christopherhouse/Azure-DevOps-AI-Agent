/**
 * Test to validate the comprehensive JWT scope fix
 * This test ensures that all scenarios properly include the backend API scope
 */

import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';
import { createMockClientConfig } from './test-helpers';

describe('Comprehensive JWT Scope Fix', () => {
  beforeEach(() => {
    clearCachedClientConfig();
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  it('should use client config scopes when available (primary scenario)', () => {
    // Test the primary path: client config is loaded and cached
    const clientConfig = createMockClientConfig({
      azure: {
        tenantId: 'test-tenant-id',
        clientId: 'test-client-id',
        authority: 'https://login.microsoftonline.com/test-tenant-id',
        redirectUri: 'http://localhost:3000/auth/callback',
        scopes: [
          'openid',
          'profile',
          'User.Read',
          'email',
          'api://test-backend-client-id/Api.All'
        ]
      },
      backend: {
        url: 'http://localhost:8000/api'
      },
      frontend: {
        url: 'http://localhost:3000'
      }
    });
    
    setCachedClientConfig(clientConfig);
    
    const loginReq = getLoginRequest();
    const tokenReq = getTokenRequest();
    
    // Should use scopes directly from client config
    expect(loginReq.scopes).toEqual(clientConfig.azure.scopes);
    expect(tokenReq.scopes).toEqual(clientConfig.azure.scopes);
    
    // Should have all 5 required scopes
    expect(loginReq.scopes).toHaveLength(5);
    expect(tokenReq.scopes).toHaveLength(5);
    
    // Should include the backend API scope
    expect(loginReq.scopes).toContain('api://test-backend-client-id/Api.All');
    expect(tokenReq.scopes).toContain('api://test-backend-client-id/Api.All');
  });

  it('should fall back to basic scopes when no client config is available', () => {
    // Test the problematic scenario: no client config cached
    clearCachedClientConfig();
    
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation();
    
    try {
      const loginReq = getLoginRequest();
      const tokenReq = getTokenRequest();
      
      // Should fall back to basic scopes only (no backend API scope)
      expect(loginReq.scopes).toEqual([
        'openid',
        'profile', 
        'User.Read',
        'email'
      ]);
      expect(tokenReq.scopes).toEqual([
        'openid',
        'profile', 
        'User.Read',
        'email'
      ]);
      
      // Should have logged the issue clearly
      expect(consoleErrorSpy).toHaveBeenCalledWith(
        expect.stringContaining('SCOPE ISSUE #224')
      );
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('JWT tokens will NOT include backend API scope')
      );
    } finally {
      consoleErrorSpy.mockRestore();
      consoleWarnSpy.mockRestore();
    }
  });

  it('should clearly indicate when client config is not available', () => {
    // Test the scenario where client config is not loaded
    clearCachedClientConfig();
    
    // Mock console methods to capture logging
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation();
    
    try {
      const loginReq = getLoginRequest();
      
      // Should fall back to basic scopes
      expect(loginReq.scopes).toEqual([
        'openid',
        'profile',
        'User.Read', 
        'email'
      ]);
      
      // Should have clearly logged the issue
      expect(consoleErrorSpy).toHaveBeenCalledWith(
        expect.stringContaining('SCOPE ISSUE #224')
      );
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('JWT tokens will NOT include backend API scope')
      );
    } finally {
      consoleErrorSpy.mockRestore();
      consoleWarnSpy.mockRestore();
    }
  });
});