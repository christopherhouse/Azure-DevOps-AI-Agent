/**
 * Authentication configuration for Microsoft Authentication Library (MSAL)
 */

import {
  Configuration,
  PopupRequest,
  SilentRequest,
} from '@azure/msal-browser';

/**
 * Get MSAL configuration object - deferred to avoid build-time issues
 */
export const getMsalConfig = (): Configuration => {
  // Check if we're in build time - use placeholder values
  const isBuildTime = typeof window === 'undefined' && process.env.NODE_ENV === 'production' && !process.env.NEXT_PUBLIC_AZURE_TENANT_ID;
  
  return {
    auth: {
      clientId: isBuildTime ? 'build-time-placeholder' : (process.env.NEXT_PUBLIC_AZURE_CLIENT_ID || ''),
      authority: isBuildTime 
        ? 'https://login.microsoftonline.com/build-time-placeholder'
        : (process.env.NEXT_PUBLIC_AZURE_AUTHORITY ||
           `https://login.microsoftonline.com/${process.env.NEXT_PUBLIC_AZURE_TENANT_ID}`),
      redirectUri: process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI || '/',
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
      msalConfigInstance = getMsalConfig();
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
