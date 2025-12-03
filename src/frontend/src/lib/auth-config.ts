/**
 * Authentication configuration for Microsoft Authentication Library (MSAL)
 * This uses the new client config API instead of NEXT_PUBLIC_* environment variables
 */

import {
  Configuration,
  PopupRequest,
  SilentRequest,
} from '@azure/msal-browser';
import { ClientConfig, getCachedClientConfig } from '@/hooks/use-client-config';

/**
 * Get MSAL configuration from client config API
 */
async function getMsalConfigFromApi(): Promise<Configuration> {
  try {
    const response = await fetch('/api/clientConfig');
    if (!response.ok) {
      throw new Error(
        `Failed to load client configuration: ${response.status}`
      );
    }

    const config = await response.json();

    if (config.error) {
      throw new Error(config.message || 'Configuration error');
    }

    return {
      auth: {
        clientId: config.azure.clientId,
        authority: config.azure.authority,
        redirectUri: config.azure.redirectUri,
      },
      cache: {
        cacheLocation: 'sessionStorage',
        storeAuthStateInCookie: false,
      },
    };
  } catch (error) {
    console.error('Failed to load MSAL config from API:', error);
    throw error;
  }
}

/**
 * Create MSAL configuration from client config object
 */
export function createMsalConfigFromClientConfig(
  clientConfig: ClientConfig
): Configuration {
  return {
    auth: {
      clientId: clientConfig.azure.clientId,
      authority: clientConfig.azure.authority,
      redirectUri: clientConfig.azure.redirectUri,
    },
    cache: {
      cacheLocation: 'sessionStorage',
      storeAuthStateInCookie: false,
    },
  };
}

/**
 * Get MSAL configuration object - loads from runtime API
 */
export const getMsalConfig = async (): Promise<Configuration> => {
  // Check if we're in build time - use placeholder values only during static generation
  const isBuildTime =
    typeof window === 'undefined' && process.env.NODE_ENV === 'production';

  if (isBuildTime) {
    // Only return placeholder values during build/SSG phase
    return {
      auth: {
        clientId: 'build-time-placeholder',
        authority: 'https://login.microsoftonline.com/build-time-placeholder',
        redirectUri: '/',
      },
      cache: {
        cacheLocation: 'sessionStorage',
        storeAuthStateInCookie: false,
      },
    };
  }

  // Try to get from cache first
  const cachedConfig = getCachedClientConfig();
  if (cachedConfig) {
    return createMsalConfigFromClientConfig(cachedConfig);
  }

  // If not cached, load from API
  return await getMsalConfigFromApi();
};

/**
 * Synchronous version - uses cached config if available, otherwise throws
 */
export const getMsalConfigSync = (): Configuration => {
  // Check if we're in build time - use placeholder values only during static generation
  const isBuildTime =
    typeof window === 'undefined' && process.env.NODE_ENV === 'production';

  if (isBuildTime) {
    // Only return placeholder values during build/SSG phase
    return {
      auth: {
        clientId: 'build-time-placeholder',
        authority: 'https://login.microsoftonline.com/build-time-placeholder',
        redirectUri: '/',
      },
      cache: {
        cacheLocation: 'sessionStorage',
        storeAuthStateInCookie: false,
      },
    };
  }

  // Try to get from cache
  const cachedConfig = getCachedClientConfig();
  if (!cachedConfig) {
    throw new Error(
      'Client configuration not loaded yet. Use getMsalConfig() instead or ensure useClientConfig has completed loading.'
    );
  }

  return createMsalConfigFromClientConfig(cachedConfig);
};

/**
 * Legacy export for backward compatibility - will be lazy-loaded
 */
let msalConfigInstance: Configuration | null = null;
export const msalConfig = new Proxy({} as Configuration, {
  get(target, prop) {
    if (!msalConfigInstance) {
      msalConfigInstance = getMsalConfigSync();
    }
    return msalConfigInstance[prop as keyof Configuration];
  },
});

/**
 * Get scopes directly from client config as requested in issue #224
 * "Just use clientConfig.scopes as the source of the scopes when requesting a token"
 */
function getScopes(): string[] {
  const clientConfig = getCachedClientConfig();

  // Use scopes directly from client config as requested - no array manipulation
  if (clientConfig?.azure.scopes) {
    console.debug(
      'Using scopes from client config:',
      clientConfig.azure.scopes
    );
    return clientConfig.azure.scopes;
  }

  // If no client config available, this is the problematic scenario described in issue #224
  console.error(
    '🚨 SCOPE ISSUE #224: Client configuration not loaded when requesting authentication scopes! ' +
      'This will result in JWT tokens missing the backend API scope. ' +
      'Expected scopes from /api/clientConfig but falling back to basic scopes only.'
  );

  // Minimal fallback to prevent complete failure, but this is the problematic case
  const basicScopes = ['openid', 'profile', 'User.Read', 'email'];
  console.warn('Falling back to basic scopes only:', basicScopes);
  console.warn(
    'JWT tokens will NOT include backend API scope - authentication may fail!'
  );

  return basicScopes;
}

/**
 * Get OIDC profile scopes only (for profile information)
 * This separates profile scopes from backend API scopes as requested in issue #232
 */
function getOidcScopes(): string[] {
  const oidcScopes = ['openid', 'profile', 'User.Read', 'email'];
  console.debug('Using OIDC profile scopes:', oidcScopes);
  return oidcScopes;
}

/**
 * Get backend API scopes only (for backend API access)
 * This separates backend API scopes from OIDC profile scopes as requested in issue #232
 */
function getBackendApiScopes(): string[] {
  const clientConfig = getCachedClientConfig();

  if (clientConfig?.azure.scopes) {
    // Filter for backend API scopes - look for scopes that match api://*/pattern
    const backendScopes = clientConfig.azure.scopes.filter(
      (scope) => scope.startsWith('api://') && scope.includes('/Api.')
    );

    if (backendScopes.length > 0) {
      console.debug('Using backend API scopes:', backendScopes);
      return backendScopes;
    }
  }

  // Fallback: construct backend API scope from environment if available
  const clientConfig2 = getCachedClientConfig();
  if (clientConfig2?.backend?.url) {
    // Try to extract client ID from backend URL or use a default pattern
    // This is a fallback case, normally the scope should be in clientConfig.azure.scopes
    console.warn(
      'Backend API scope not found in client config, unable to construct fallback'
    );
  }

  console.warn(
    'No backend API scopes available - backend authentication may fail!'
  );
  return [];
}

/**
 * Get login request with dynamic scopes from client config
 */
export function getLoginRequest(): PopupRequest {
  return {
    scopes: getScopes(),
  };
}

/**
 * Get token request with dynamic scopes from client config
 */
export function getTokenRequest(): SilentRequest {
  return {
    scopes: getScopes(),
    account: null as any,
  };
}

/**
 * Get login request for OIDC profile information only
 * This is part of the split token approach requested in issue #232
 */
export function getOidcLoginRequest(): PopupRequest {
  return {
    scopes: getOidcScopes(),
  };
}

/**
 * Get token request for OIDC profile information only
 * This is part of the split token approach requested in issue #232
 */
export function getOidcTokenRequest(): SilentRequest {
  return {
    scopes: getOidcScopes(),
    account: null as any,
  };
}

/**
 * Get token request for backend API access only
 * This is part of the split token approach requested in issue #232
 */
export function getBackendApiTokenRequest(): SilentRequest {
  return {
    scopes: getBackendApiScopes(),
    account: null as any,
  };
}

/**
 * Scopes requested during login
 * @deprecated Use getLoginRequest() instead for dynamic scopes
 */
export const loginRequest: PopupRequest = {
  scopes: ['openid', 'profile', 'User.Read', 'email'],
};

/**
 * Scopes for token request
 * @deprecated Use getTokenRequest() instead for dynamic scopes
 */
export const tokenRequest: SilentRequest = {
  scopes: ['openid', 'profile', 'User.Read', 'email'],
  account: null as any,
};
