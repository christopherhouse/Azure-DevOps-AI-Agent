/**
 * Application configuration
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

export const loadConfig = (): Config => {
  // Validate required Azure environment variables
  const requiredAzureVars = [
    'NEXT_PUBLIC_AZURE_TENANT_ID',
    'NEXT_PUBLIC_AZURE_CLIENT_ID',
  ];

  for (const varName of requiredAzureVars) {
    if (!process.env[varName]) {
      throw new Error(`Required environment variable ${varName} is not set`);
    }
  }

  return {
    backendUrl: process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:8000',
    frontendUrl:
      process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000',
    environment: process.env.NEXT_PUBLIC_ENVIRONMENT || 'development',
    debug: process.env.NEXT_PUBLIC_DEBUG === 'true',
    api: {
      baseUrl: process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:8000',
    },
    azure: {
      tenantId: process.env.NEXT_PUBLIC_AZURE_TENANT_ID || '',
      clientId: process.env.NEXT_PUBLIC_AZURE_CLIENT_ID || '',
      authority:
        process.env.NEXT_PUBLIC_AZURE_AUTHORITY ||
        `https://login.microsoftonline.com/${process.env.NEXT_PUBLIC_AZURE_TENANT_ID}`,
      redirectUri:
        process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI ||
        'http://localhost:3000/auth/callback',
      scopes: (
        process.env.NEXT_PUBLIC_AZURE_SCOPES || 'openid,profile,User.Read'
      ).split(',').map(scope => scope.trim()),
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
};

export const config = loadConfig();
