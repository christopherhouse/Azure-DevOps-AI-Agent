/**
 * Authentication hook using MSAL React.
 */

import { useEffect, useState, useCallback } from 'react';
import { useMsal, useAccount } from '@azure/msal-react';
import { InteractionStatus, SilentRequest } from '@azure/msal-browser';
import { getLoginRequest, getTokenRequest } from '@/lib/auth-config';
import { trackAuthEvent } from '@/lib/telemetry';
import { apiClient } from '@/services/api-client';
import type { User, AuthState } from '@/types';

export function useAuth() {
  const { instance, accounts, inProgress } = useMsal();
  const account = useAccount(accounts[0] || {});

  const [authState, setAuthState] = useState<AuthState>({
    isAuthenticated: false,
    user: null,
    accessToken: null,
    error: null,
    isLoading: true,
  });

  /**
   * Extract user information from account
   */
  const extractUserInfo = (account: any): User | null => {
    if (!account) return null;

    return {
      id: account.homeAccountId || account.localAccountId,
      email: account.username,
      name: account.name || account.username,
      tenantId: account.tenantId || '',
    };
  };

  /**
   * Acquire access token silently
   */
  const acquireTokenSilently = useCallback(async (): Promise<string | null> => {
    if (!account) return null;

    try {
      const tokenReq = getTokenRequest();
      const request: SilentRequest = {
        ...tokenReq,
        account,
      };

      const response = await instance.acquireTokenSilent(request);
      return response.accessToken;
    } catch (error) {
      console.error('Silent token acquisition failed:', error);
      return null;
    }
  }, [account, instance]);

  /**
   * Login user
   */
  const login = async () => {
    setAuthState((prev) => ({ ...prev, isLoading: true, error: null }));

    try {
      trackAuthEvent('login_attempt');
      const loginReq = getLoginRequest();
      await instance.loginPopup(loginReq);
      trackAuthEvent('login_success');
    } catch (error: any) {
      console.error('Login failed:', error);
      trackAuthEvent('login_failure', { error: error.message });
      setAuthState((prev) => ({
        ...prev,
        error: error.message || 'Login failed',
        isLoading: false,
      }));
    }
  };

  /**
   * Logout user
   */
  const logout = async () => {
    try {
      trackAuthEvent('logout');
      apiClient.setAccessToken(null);
      await instance.logoutPopup();
    } catch (error: any) {
      console.error('Logout failed:', error);
      // Even if logout fails, clear local state
      setAuthState({
        isAuthenticated: false,
        user: null,
        accessToken: null,
        error: null,
        isLoading: false,
      });
    }
  };

  /**
   * Get current access token
   */
  const getAccessToken = async (): Promise<string | null> => {
    if (!authState.isAuthenticated || !account) {
      return null;
    }

    // Try to use cached token first
    if (authState.accessToken) {
      return authState.accessToken;
    }

    // Acquire new token silently
    return await acquireTokenSilently();
  };

  // Effect to handle authentication state changes
  useEffect(() => {
    const updateAuthState = async () => {
      if (inProgress === InteractionStatus.Startup) {
        return; // Still loading
      }

      if (account) {
        // User is authenticated
        const user = extractUserInfo(account);
        const accessToken = await acquireTokenSilently();

        if (accessToken) {
          apiClient.setAccessToken(accessToken);
        }

        setAuthState({
          isAuthenticated: true,
          user,
          accessToken,
          error: null,
          isLoading: false,
        });
      } else {
        // User is not authenticated
        apiClient.setAccessToken(null);
        setAuthState({
          isAuthenticated: false,
          user: null,
          accessToken: null,
          error: null,
          isLoading: false,
        });
      }
    };

    updateAuthState();
  }, [account, inProgress, acquireTokenSilently]);

  // Effect to refresh token periodically
  useEffect(() => {
    if (!authState.isAuthenticated || !account) {
      return;
    }

    const refreshToken = async () => {
      const newToken = await acquireTokenSilently();
      if (newToken && newToken !== authState.accessToken) {
        apiClient.setAccessToken(newToken);
        setAuthState((prev) => ({ ...prev, accessToken: newToken }));
      }
    };

    // Refresh token every 30 minutes
    const interval = setInterval(refreshToken, 30 * 60 * 1000);

    return () => clearInterval(interval);
  }, [
    authState.isAuthenticated,
    account,
    authState.accessToken,
    acquireTokenSilently,
  ]);

  return {
    ...authState,
    login,
    logout,
    getAccessToken,
  };
}
