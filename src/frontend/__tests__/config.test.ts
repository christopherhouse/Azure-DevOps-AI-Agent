/**
 * Tests for the configuration module.
 */

import { loadConfig, resetConfig } from '@/lib/config';

// Mock environment variables
const mockEnv = {
  NEXT_PUBLIC_AZURE_TENANT_ID: 'test-tenant-id',
  NEXT_PUBLIC_AZURE_CLIENT_ID: 'test-client-id',
  NEXT_PUBLIC_AZURE_AUTHORITY: 'https://login.microsoftonline.com/test-tenant-id',
  NEXT_PUBLIC_AZURE_REDIRECT_URI: 'http://localhost:3000/auth/callback',
  NEXT_PUBLIC_AZURE_SCOPES: 'openid,profile,User.Read',
  NEXT_PUBLIC_FRONTEND_URL: 'http://localhost:3000',
  NEXT_PUBLIC_BACKEND_URL: 'http://localhost:8000',
  NEXT_PUBLIC_ENVIRONMENT: 'test',
  NEXT_PUBLIC_DEBUG: 'true',
  NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING: 'test-connection-string',
  NEXT_PUBLIC_ENABLE_TELEMETRY: 'true',
  NEXT_PUBLIC_SESSION_TIMEOUT: '3600',
  NEXT_PUBLIC_REQUIRE_HTTPS: 'false',
};

describe('Config', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    jest.resetModules();
    resetConfig(); // Reset the cached config instance
    process.env = { ...originalEnv, ...mockEnv };
  });

  afterEach(() => {
    process.env = originalEnv;
    resetConfig(); // Clean up after each test
  });

  it('should load configuration with all required environment variables', () => {
    const config = loadConfig();

    expect(config.environment).toBe('test');
    expect(config.debug).toBe(true);
    expect(config.frontendUrl).toBe('http://localhost:3000');
    expect(config.backendUrl).toBe('http://localhost:8000');
    expect(config.api.baseUrl).toBe('http://localhost:8000/api');

    expect(config.azure.tenantId).toBe('test-tenant-id');
    expect(config.azure.clientId).toBe('test-client-id');
    expect(config.azure.authority).toBe('https://login.microsoftonline.com/test-tenant-id');
    expect(config.azure.redirectUri).toBe('http://localhost:3000/auth/callback');
    expect(config.azure.scopes).toEqual(['openid', 'profile', 'User.Read']);

    expect(config.telemetry.connectionString).toBe('test-connection-string');
    expect(config.telemetry.enabled).toBe(true);

    expect(config.security.sessionTimeout).toBe(3600);
    expect(config.security.requireHttps).toBe(false);
  });

  it('should throw error when required environment variables are missing', () => {
    delete process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

    expect(() => loadConfig()).toThrow('Required environment variable NEXT_PUBLIC_AZURE_TENANT_ID is not set');
  });

  it('should use default values for optional environment variables', () => {
    delete process.env.NEXT_PUBLIC_ENVIRONMENT;
    delete process.env.NEXT_PUBLIC_DEBUG;
    delete process.env.NEXT_PUBLIC_FRONTEND_URL;
    delete process.env.NEXT_PUBLIC_BACKEND_URL;

    const config = loadConfig();

    expect(config.environment).toBe('development');
    expect(config.debug).toBe(false);
    expect(config.frontendUrl).toBe('http://localhost:3000');
    expect(config.backendUrl).toBe('http://localhost:8000');
    expect(config.api.baseUrl).toBe('http://localhost:8000/api');
  });

  it('should parse boolean environment variables correctly', () => {
    process.env.NEXT_PUBLIC_DEBUG = 'false';
    process.env.NEXT_PUBLIC_ENABLE_TELEMETRY = 'false';
    process.env.NEXT_PUBLIC_REQUIRE_HTTPS = 'true';

    const config = loadConfig();

    expect(config.debug).toBe(false);
    expect(config.telemetry.enabled).toBe(false);
    expect(config.security.requireHttps).toBe(true);
  });

  it('should parse scopes correctly', () => {
    process.env.NEXT_PUBLIC_AZURE_SCOPES = 'openid, profile , User.Read,  api://test  ';

    const config = loadConfig();

    expect(config.azure.scopes).toEqual(['openid', 'profile', 'User.Read', 'api://test']);
  });

  it('should use placeholder values during build time', () => {
    // Store original values for restoration
    const originalNodeEnv = process.env.NODE_ENV;
    const originalWindow = global.window;
    
    // Simulate build time environment
    delete process.env.NEXT_PUBLIC_AZURE_TENANT_ID;
    delete process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
    
    // Use Object.defineProperty to safely modify NODE_ENV
    Object.defineProperty(process.env, 'NODE_ENV', {
      value: 'production',
      writable: true,
      enumerable: true,
      configurable: true
    });
    
    // Mock window to be undefined (server-side)
    delete (global as any).window;

    const config = loadConfig();

    // Should get placeholder values instead of throwing an error
    expect(config.azure.tenantId).toBe('build-time-placeholder');
    expect(config.azure.clientId).toBe('build-time-placeholder');
    expect(config.azure.authority).toBe('https://login.microsoftonline.com/build-time-placeholder');

    // Restore original values
    if (originalNodeEnv !== undefined) {
      Object.defineProperty(process.env, 'NODE_ENV', {
        value: originalNodeEnv,
        writable: true,
        enumerable: true,
        configurable: true
      });
    }
    (global as any).window = originalWindow;
  });

  it('should not double-add /api suffix to backend URL', () => {
    process.env.NEXT_PUBLIC_BACKEND_URL = 'http://localhost:8000/api';

    const config = loadConfig();

    expect(config.api.baseUrl).toBe('http://localhost:8000/api');
  });

  it('should construct redirect URI from frontend URL when not explicitly set', () => {
    process.env.NEXT_PUBLIC_FRONTEND_URL = 'https://myapp.region.azurecontainerapps.io';
    delete process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI;

    const config = loadConfig();

    expect(config.azure.redirectUri).toBe('https://myapp.region.azurecontainerapps.io/auth/callback');
  });
});