/**
 * Client configuration API endpoint
 *
 * This endpoint provides runtime configuration to client components by reading
 * from server-side environment variables (not NEXT_PUBLIC_*). This enables
 * building once and deploying multiple times with different runtime configurations.
 *
 * No authentication is required to call this endpoint as it only returns
 * non-sensitive configuration values needed for client initialization.
 */

import { NextResponse } from 'next/server';

export interface ClientConfigResponse {
  azure: {
    tenantId: string;
    clientId: string;
    authority: string;
    redirectUri: string;
    scopes: string[];
  };
  backend: {
    url: string;
  };
  frontend: {
    url: string;
  };
}

export async function GET() {
  try {
    // Read configuration from server-side environment variables
    const tenantId = process.env.AZURE_TENANT_ID;
    const clientId = process.env.AZURE_CLIENT_ID;
    const backendClientId = process.env.BACKEND_CLIENT_ID;
    const backendUrl = process.env.BACKEND_URL;
    const frontendUrl = process.env.FRONTEND_URL;
    const authority = process.env.AZURE_AUTHORITY;
    const redirectUri = process.env.AZURE_REDIRECT_URI;
    const scopes = process.env.AZURE_SCOPES;

    // Validate required environment variables
    if (!tenantId) {
      throw new Error('AZURE_TENANT_ID environment variable is not set');
    }
    if (!clientId) {
      throw new Error('AZURE_CLIENT_ID environment variable is not set');
    }
    if (!backendClientId) {
      throw new Error('BACKEND_CLIENT_ID environment variable is not set');
    }
    if (!backendUrl) {
      throw new Error('BACKEND_URL environment variable is not set');
    }
    if (!frontendUrl) {
      throw new Error('FRONTEND_URL environment variable is not set');
    }

    // Construct scopes array with OIDC scopes and backend client ID
    // Using just the backend client ID as scope to set correct audience claim
    // Per MSAL docs: audience (aud) should be the client ID, scope (scp) should be the permission
    const defaultScopes = ['openid', 'profile', 'User.Read', 'email'];
    const backendClientIdScope = backendClientId; // Use client ID directly for correct audience

    const scopesArray = scopes
      ? scopes.split(',').map((scope) => scope.trim())
      : defaultScopes;

    // Add backend client ID scope if not already present
    // This will set the audience to the backend client ID as required
    if (!scopesArray.includes(backendClientIdScope)) {
      scopesArray.push(backendClientIdScope);
    }

    // Build the response with defaults for optional values
    const config: ClientConfigResponse = {
      azure: {
        tenantId,
        clientId,
        authority: authority || `https://login.microsoftonline.com/${tenantId}`,
        redirectUri: redirectUri || `${frontendUrl}/auth/callback`,
        scopes: scopesArray,
      },
      backend: {
        url: backendUrl.endsWith('/api') ? backendUrl : `${backendUrl}/api`,
      },
      frontend: {
        url: frontendUrl,
      },
    };

    return NextResponse.json(config);
  } catch (error) {
    console.error('Failed to load client configuration:', error);

    // Return error with helpful information
    return NextResponse.json(
      {
        error: 'Configuration Error',
        message:
          error instanceof Error
            ? error.message
            : 'Unknown configuration error',
        details:
          'Please check that required environment variables (AZURE_TENANT_ID, AZURE_CLIENT_ID, BACKEND_CLIENT_ID, BACKEND_URL, FRONTEND_URL) are properly set on the server',
      },
      { status: 500 }
    );
  }
}
