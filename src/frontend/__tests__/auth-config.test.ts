/**
 * Tests for MSAL configuration fix - ensuring client_id is properly passed during authentication
 */

import { getMsalConfig } from '@/lib/auth-config';

// Test environment variables
const mockEnvVars = {
  NEXT_PUBLIC_AZURE_TENANT_ID: 'test-tenant-id-12345',
  NEXT_PUBLIC_AZURE_CLIENT_ID: 'test-client-id-67890',
  NEXT_PUBLIC_AZURE_AUTHORITY: 'https://login.microsoftonline.com/test-tenant-id-12345',
  NEXT_PUBLIC_AZURE_REDIRECT_URI: 'http://localhost:3000/auth/callback',
};

describe('MSAL Configuration Fix', () => {
  const originalEnv = process.env;
  const originalWindow = global.window;

  beforeEach(() => {
    jest.resetModules();
    // Set up test environment variables
    process.env = { ...originalEnv, ...mockEnvVars };
    // Mock browser environment
    (global as any).window = { location: { origin: 'http://localhost:3000' } };
  });

  afterEach(() => {
    process.env = originalEnv;
    global.window = originalWindow;
  });

  it('should not use placeholder values in browser environment', () => {
    const config = getMsalConfig();

    // Ensure real values are used, not placeholders
    expect(config.auth.clientId).toBe('test-client-id-67890');
    expect(config.auth.clientId).not.toBe('build-time-placeholder');
    expect(config.auth.authority).toBe('https://login.microsoftonline.com/test-tenant-id-12345');
    expect(config.auth.authority).not.toBe('https://login.microsoftonline.com/build-time-placeholder');
  });

  it('should throw error when NEXT_PUBLIC_AZURE_TENANT_ID is missing in browser', () => {
    delete process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

    expect(() => getMsalConfig()).toThrow('Required environment variable NEXT_PUBLIC_AZURE_TENANT_ID is not set');
  });

  it('should throw error when NEXT_PUBLIC_AZURE_CLIENT_ID is missing in browser', () => {
    delete process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;

    expect(() => getMsalConfig()).toThrow('Required environment variable NEXT_PUBLIC_AZURE_CLIENT_ID is not set');
  });

  it('should use placeholder values only during build time', () => {
    // Simulate build time environment
    delete process.env.NEXT_PUBLIC_AZURE_TENANT_ID;
    delete process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
    
    // Mock server-side production environment
    delete (global as any).window;
    Object.defineProperty(process.env, 'NODE_ENV', {
      value: 'production',
      writable: true,
      enumerable: true,
      configurable: true
    });

    const config = getMsalConfig();

    // Should use placeholder values during build
    expect(config.auth.clientId).toBe('build-time-placeholder');
    expect(config.auth.authority).toBe('https://login.microsoftonline.com/build-time-placeholder');
  });

  it('should construct correct redirect URI with client origin', () => {
    delete process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI; // Test default behavior

    const config = getMsalConfig();

    expect(config.auth.redirectUri).toBe('http://localhost:3000/auth/callback');
  });

  it('should use explicit redirect URI when provided', () => {
    process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI = 'https://custom.domain.com/auth/callback';

    const config = getMsalConfig();

    expect(config.auth.redirectUri).toBe('https://custom.domain.com/auth/callback');
  });
});