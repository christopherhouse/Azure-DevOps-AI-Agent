/**
 * Authentication configuration for Microsoft Authentication Library (MSAL)
 */

import { Configuration, PopupRequest, SilentRequest } from '@azure/msal-browser';

/**
 * MSAL configuration object
 */
export const msalConfig: Configuration = {
  auth: {
    clientId: process.env.NEXT_PUBLIC_AZURE_CLIENT_ID || '',
    authority:
      process.env.NEXT_PUBLIC_AZURE_AUTHORITY ||
      `https://login.microsoftonline.com/${process.env.NEXT_PUBLIC_AZURE_TENANT_ID}`,
    redirectUri: process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI || '/',
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
};

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