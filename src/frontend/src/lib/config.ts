/**
 * Application configuration
 *
 * This module handles both build-time and runtime configuration loading.
 * For containerized deployments with secret references, use the runtime config API.
 * For local development with environment files, use the build-time config.
 */

export interface Config {
  backendUrl: string;
  frontendUrl: string;
  environment: string;
  debug: boolean;
  api: {
    baseUrl: string;
  };
  azure: {
    tenantId: string;
    clientId: string;
    authority: string;
    redirectUri: string;
    scopes: string[];
  };
  telemetry: {
    connectionString: string;
    enabled: boolean;
  };
  security: {
    sessionTimeout: number;
    requireHttps: boolean;
  };
}

// Cached config instance
let configInstance: Config | null = null;

/**
 * Check if we're using build-time placeholders (indicates container deployment)
 */
function isUsingPlaceholders(): boolean {
  return (
    process.env.NEXT_PUBLIC_AZURE_TENANT_ID === 'build-time-placeholder' ||
    process.env.NEXT_PUBLIC_AZURE_CLIENT_ID === 'build-time-placeholder'
  );
}

export const loadConfig = (): Config => {
  // Return cached instance if available
  if (configInstance) {
    return configInstance;
  }

  // Check if we're in build time (SSG phase) - use placeholder values
  const isBuildTime =
    typeof window === 'undefined' &&
    process.env.NODE_ENV === 'production' &&
    !process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

  if (isBuildTime) {
    // Provide placeholder values during build to allow static generation
    configInstance = {
      backendUrl:
        process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:8000',
      frontendUrl:
        process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000',
      environment: process.env.NEXT_PUBLIC_ENVIRONMENT || 'development',
      debug: process.env.NEXT_PUBLIC_DEBUG === 'true',
      api: {
        baseUrl: (() => {
          const url =
            process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:8000';
          return url.endsWith('/api') ? url : `${url}/api`;
        })(),
      },
      azure: {
        tenantId: 'build-time-placeholder',
        clientId: 'build-time-placeholder',
        authority: 'https://login.microsoftonline.com/build-time-placeholder',
        redirectUri:
          process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI ||
          `${process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000'}/auth/callback`,
        scopes: (
          process.env.NEXT_PUBLIC_AZURE_SCOPES || 'openid,profile,User.Read'
        )
          .split(',')
          .map((scope) => scope.trim()),
      },
      telemetry: {
        connectionString:
          process.env.NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING || '',
        enabled: process.env.NEXT_PUBLIC_ENABLE_TELEMETRY === 'true',
      },
      security: {
        sessionTimeout: parseInt(
          process.env.NEXT_PUBLIC_SESSION_TIMEOUT || '3600',
          10
        ),
        requireHttps: process.env.NEXT_PUBLIC_REQUIRE_HTTPS === 'true',
      },
    };
    return configInstance;
  }

  // Check if we're in a container with placeholders (runtime config needed)
  if (isUsingPlaceholders()) {
    throw new Error(
      'Configuration uses build-time placeholders. Please use the runtime configuration API (/api/config) or useRuntimeConfig hook for client-side access.'
    );
  }

  // Runtime validation - only validate required Azure environment variables at runtime
  const requiredAzureVars = [
    'NEXT_PUBLIC_AZURE_TENANT_ID',
    'NEXT_PUBLIC_AZURE_CLIENT_ID',
  ];

  for (const varName of requiredAzureVars) {
    if (!process.env[varName]) {
      throw new Error(`Required environment variable ${varName} is not set`);
    }
  }

  configInstance = {
    backendUrl: process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:8000',
    frontendUrl:
      process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000',
    environment: process.env.NEXT_PUBLIC_ENVIRONMENT || 'development',
    debug: process.env.NEXT_PUBLIC_DEBUG === 'true',
    api: {
      baseUrl: (() => {
        const url =
          process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:8000';
        return url.endsWith('/api') ? url : `${url}/api`;
      })(),
    },
    azure: {
      tenantId: process.env.NEXT_PUBLIC_AZURE_TENANT_ID || '',
      clientId: process.env.NEXT_PUBLIC_AZURE_CLIENT_ID || '',
      authority:
        process.env.NEXT_PUBLIC_AZURE_AUTHORITY ||
        `https://login.microsoftonline.com/${process.env.NEXT_PUBLIC_AZURE_TENANT_ID}`,
      redirectUri:
        process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI ||
        `${process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000'}/auth/callback`,
      scopes: (
        process.env.NEXT_PUBLIC_AZURE_SCOPES || 'openid,profile,User.Read'
      )
        .split(',')
        .map((scope) => scope.trim()),
    },
    telemetry: {
      connectionString:
        process.env.NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING || '',
      enabled: process.env.NEXT_PUBLIC_ENABLE_TELEMETRY === 'true',
    },
    security: {
      sessionTimeout: parseInt(
        process.env.NEXT_PUBLIC_SESSION_TIMEOUT || '3600',
        10
      ),
      requireHttps: process.env.NEXT_PUBLIC_REQUIRE_HTTPS === 'true',
    },
  };

  return configInstance;
};

// Export a getter function instead of direct config instance
export const getConfig = () => loadConfig();

// Reset function for testing purposes
export const resetConfig = () => {
  configInstance = null;
};
