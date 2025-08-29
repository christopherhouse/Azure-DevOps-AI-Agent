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
 * Get scopes from client config or fallback to defaults
 */
function getScopes(): string[] {
  const clientConfig = getCachedClientConfig();
  return clientConfig?.azure.scopes || ['openid', 'profile', 'User.Read'];
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
 * Scopes requested during login
 * @deprecated Use getLoginRequest() instead for dynamic scopes
 */
export const loginRequest: PopupRequest = {
  scopes: ['openid', 'profile', 'User.Read'],
};

/**
 * Scopes for token request
 * @deprecated Use getTokenRequest() instead for dynamic scopes
 */
export const tokenRequest: SilentRequest = {
  scopes: ['openid', 'profile', 'User.Read'],
  account: null as any,
};
