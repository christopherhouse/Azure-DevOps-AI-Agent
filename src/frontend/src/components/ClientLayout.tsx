/**
 * Client-side layout wrapper with MSAL provider.
 * Uses the new client config API instead of NEXT_PUBLIC_* environment variables.
 */

'use client';

import React, { useState, useEffect } from 'react';
import { MsalProvider } from '@azure/msal-react';
import { PublicClientApplication } from '@azure/msal-browser';
import { createMsalConfigFromClientConfig } from '@/lib/auth-config';
import {
  useClientConfig,
  setCachedClientConfig,
} from '@/hooks/use-client-config';

interface ClientLayoutProps {
  children: React.ReactNode;
}

export function ClientLayout({ children }: ClientLayoutProps) {
  const [msalInstance, setMsalInstance] =
    useState<PublicClientApplication | null>(null);
  const [error, setError] = useState<string | null>(null);
  const {
    config: clientConfig,
    loading: configLoading,
    error: configError,
  } = useClientConfig();

  useEffect(() => {
    // Only initialize MSAL when client config is loaded
    if (!clientConfig || configLoading) {
      return;
    }

    async function initializeMsal() {
      try {
        // Cache the config for use by other components
        setCachedClientConfig(clientConfig);

        // Set telemetry configuration on window object for telemetry library
        if (typeof window !== 'undefined') {
          window.__CLIENT_CONFIG__ = {
            telemetry: {
              connectionString: clientConfig.telemetry.connectionString,
              enabled: clientConfig.telemetry.enabled,
            },
            debug: clientConfig.debug,
          };
        }

        // Create MSAL configuration from client config
        const msalConfig = createMsalConfigFromClientConfig(clientConfig);

        // Additional validation to ensure we're not using placeholder values
        if (msalConfig.auth.clientId === 'build-time-placeholder') {
          throw new Error(
            'MSAL configuration is using placeholder values. Please check your environment variables.'
          );
        }

        const instance = new PublicClientApplication(msalConfig);
        setMsalInstance(instance);
        setError(null);
      } catch (err: any) {
        console.error('Failed to initialize MSAL:', err);
        setError(err.message || 'Failed to initialize authentication');
      }
    }

    initializeMsal();
  }, [clientConfig, configLoading]);

  // Show loading state while config is loading
  if (configLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-2 text-sm text-gray-600">Loading configuration...</p>
        </div>
      </div>
    );
  }

  // Show configuration error
  if (configError) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="max-w-md w-full p-6">
          <div className="bg-red-50 border border-red-200 rounded-lg p-6">
            <div className="flex items-center mb-4">
              <div className="flex-shrink-0">
                <svg
                  className="h-8 w-8 text-red-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.96-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"
                  />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-lg font-medium text-red-800">
                  {configError.error}
                </h3>
              </div>
            </div>
            <div className="text-sm text-red-700">
              <p className="font-medium">{configError.message}</p>
              <p className="mt-2 text-red-600">{configError.details}</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Show loading state while MSAL initializes
  if (!msalInstance) {
    if (error) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50">
          <div className="max-w-md w-full p-6">
            <div className="bg-red-50 border border-red-200 rounded-lg p-6">
              <div className="text-sm text-red-700">
                <p className="font-medium">Authentication Error</p>
                <p className="mt-2">{error}</p>
              </div>
            </div>
          </div>
        </div>
      );
    }

    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-2 text-sm text-gray-600">
            Initializing authentication...
          </p>
        </div>
      </div>
    );
  }

  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}

export default ClientLayout;
