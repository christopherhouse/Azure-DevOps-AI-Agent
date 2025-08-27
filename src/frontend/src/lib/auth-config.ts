/**
 * Authentication configuration for Microsoft Authentication Library (MSAL)
 * This works with both build-time and runtime configuration loading
 */

import {
  Configuration,
  PopupRequest,
  SilentRequest,
} from '@azure/msal-browser';

/**
 * Check if we're using build-time placeholders (indicates container deployment)
 */
function isUsingPlaceholders(): boolean {
  return process.env.NEXT_PUBLIC_AZURE_TENANT_ID === 'build-time-placeholder' ||
         process.env.NEXT_PUBLIC_AZURE_CLIENT_ID === 'build-time-placeholder';
}

/**
 * Get MSAL configuration from runtime config API
 */
async function getMsalConfigFromApi(): Promise<Configuration> {
  try {
    const response = await fetch('/api/config');
    if (!response.ok) {
      throw new Error(`Failed to load configuration: ${response.status}`);
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
 * Get MSAL configuration object - deferred to avoid build-time issues
 */
export const getMsalConfig = async (): Promise<Configuration> => {
  // Check if we're in build time - use placeholder values only during static generation
  const isBuildTime = typeof window === 'undefined' && 
                     process.env.NODE_ENV === 'production' && 
                     !process.env.NEXT_PUBLIC_AZURE_TENANT_ID;
  
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

  // If using placeholders, load from runtime API
  if (isUsingPlaceholders()) {
    return await getMsalConfigFromApi();
  }

  // Standard configuration - validate environment variables are present
  const tenantId = process.env.NEXT_PUBLIC_AZURE_TENANT_ID;
  const clientId = process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;

  if (!tenantId) {
    throw new Error('Required environment variable NEXT_PUBLIC_AZURE_TENANT_ID is not set');
  }
  if (!clientId) {
    throw new Error('Required environment variable NEXT_PUBLIC_AZURE_CLIENT_ID is not set');
  }

  return {
    auth: {
      clientId: clientId,
      authority: process.env.NEXT_PUBLIC_AZURE_AUTHORITY || 
                `https://login.microsoftonline.com/${tenantId}`,
      redirectUri: process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI || 
                  (typeof window !== 'undefined' ? window.location.origin + '/auth/callback' : '/auth/callback'),
    },
    cache: {
      cacheLocation: 'sessionStorage',
      storeAuthStateInCookie: false,
    },
  };
};

/**
 * Synchronous version for legacy compatibility - only works with direct env vars
 */
export const getMsalConfigSync = (): Configuration => {
  // Check if we're in build time - use placeholder values only during static generation
  const isBuildTime = typeof window === 'undefined' && 
                     process.env.NODE_ENV === 'production' && 
                     !process.env.NEXT_PUBLIC_AZURE_TENANT_ID;
  
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

  // If using placeholders, we can't provide sync config
  if (isUsingPlaceholders()) {
    throw new Error('Configuration uses runtime API. Use getMsalConfig() instead.');
  }

  // Standard synchronous configuration
  const tenantId = process.env.NEXT_PUBLIC_AZURE_TENANT_ID;
  const clientId = process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;

  if (!tenantId) {
    throw new Error('Required environment variable NEXT_PUBLIC_AZURE_TENANT_ID is not set');
  }
  if (!clientId) {
    throw new Error('Required environment variable NEXT_PUBLIC_AZURE_CLIENT_ID is not set');
  }

  return {
    auth: {
      clientId: clientId,
      authority: process.env.NEXT_PUBLIC_AZURE_AUTHORITY || 
                `https://login.microsoftonline.com/${tenantId}`,
      redirectUri: process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI || 
                  (typeof window !== 'undefined' ? window.location.origin + '/auth/callback' : '/auth/callback'),
    },
    cache: {
      cacheLocation: 'sessionStorage',
      storeAuthStateInCookie: false,
    },
  };
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
  }
});

/**
 * Scopes requested during login
 */
export const loginRequest: PopupRequest = {
  scopes: ['openid', 'profile', 'User.Read'],
};

/**
 * Scopes for token request
 */
export const tokenRequest: SilentRequest = {
  scopes: ['openid', 'profile', 'User.Read'],
  account: null as any,
};
