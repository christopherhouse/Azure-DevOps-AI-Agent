/**
 * Client-side layout wrapper with MSAL provider.
 */

'use client';

import React, { useState, useEffect } from 'react';
import { MsalProvider } from '@azure/msal-react';
import { PublicClientApplication } from '@azure/msal-browser';
import { getMsalConfig } from '@/lib/auth-config';

interface ClientLayoutProps {
  children: React.ReactNode;
}

export function ClientLayout({ children }: ClientLayoutProps) {
  const [msalInstance, setMsalInstance] = useState<PublicClientApplication | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Only initialize MSAL on the client side after mount
    try {
      // Validate required environment variables
      if (!process.env.NEXT_PUBLIC_AZURE_TENANT_ID) {
        throw new Error('Required environment variable NEXT_PUBLIC_AZURE_TENANT_ID is not set');
      }
      if (!process.env.NEXT_PUBLIC_AZURE_CLIENT_ID) {
        throw new Error('Required environment variable NEXT_PUBLIC_AZURE_CLIENT_ID is not set');
      }

      const config = getMsalConfig();
      
      // Additional validation to ensure we're not using placeholder values
      if (config.auth.clientId === 'build-time-placeholder') {
        throw new Error('MSAL configuration is using placeholder values. Please check your environment variables.');
      }

      const instance = new PublicClientApplication(config);
      setMsalInstance(instance);
    } catch (err: any) {
      console.error('Failed to initialize MSAL:', err);
      setError(err.message || 'Failed to initialize authentication');
    }
  }, []);

  // Show loading state while MSAL initializes
  if (!msalInstance) {
    if (error) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-red-50">
          <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full">
            <div className="text-center">
              <svg
                className="w-12 h-12 text-red-500 mx-auto mb-4"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <h2 className="text-lg font-semibold text-gray-900 mb-2">Authentication Configuration Error</h2>
              <p className="text-sm text-gray-600 mb-4">{error}</p>
              <p className="text-xs text-gray-500">
                Please check your environment configuration and contact your system administrator if this error persists.
              </p>
            </div>
          </div>
        </div>
      );
    }

    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-2 text-sm text-gray-600">Initializing application...</p>
        </div>
      </div>
    );
  }

  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}

export default ClientLayout;
