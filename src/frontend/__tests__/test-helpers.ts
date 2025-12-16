/**
 * Shared test helper utilities and mock data
 */

import { ClientConfig } from '@/hooks/use-client-config';

/**
 * Default telemetry configuration for tests
 */
export const defaultTelemetryConfig = {
  connectionString: '',
  enabled: false
};

/**
 * Default debug configuration for tests
 */
export const defaultDebug = false;

/**
 * Create a mock client config with backend scope
 */
export function createMockClientConfigWithBackendScope(
  overrides?: Partial<ClientConfig>
): ClientConfig {
  return {
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
    },
    telemetry: defaultTelemetryConfig,
    debug: defaultDebug,
    ...overrides
  };
}

/**
 * Create a mock client config with OIDC scopes only (no backend scope)
 */
export function createMockClientConfigOidcOnly(
  overrides?: Partial<ClientConfig>
): ClientConfig {
  return {
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
    },
    telemetry: defaultTelemetryConfig,
    debug: defaultDebug,
    ...overrides
  };
}

/**
 * Create a mock client config with custom values
 */
export function createMockClientConfig(
  config: Partial<ClientConfig>
): ClientConfig {
  return {
    azure: {
      tenantId: 'test-tenant-id',
      clientId: 'test-client-id',
      authority: 'https://login.microsoftonline.com/test-tenant-id',
      redirectUri: 'http://localhost:3000/auth/callback',
      scopes: ['openid', 'profile', 'User.Read']
    },
    backend: {
      url: 'http://localhost:8000'
    },
    frontend: {
      url: 'http://localhost:3000'
    },
    telemetry: defaultTelemetryConfig,
    debug: defaultDebug,
    ...config
  };
}
