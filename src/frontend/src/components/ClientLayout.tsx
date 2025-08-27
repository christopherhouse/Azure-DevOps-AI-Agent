/**
 * Client-side layout wrapper with MSAL provider.
 */

'use client';

import React, { useState, useEffect } from 'react';
import { MsalProvider } from '@azure/msal-react';
import { PublicClientApplication } from '@azure/msal-browser';
import { getMsalConfigSync } from '@/lib/auth-config';

interface ClientLayoutProps {
  children: React.ReactNode;
}

export function ClientLayout({ children }: ClientLayoutProps) {
  const [msalInstance, setMsalInstance] = useState<PublicClientApplication | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Only initialize MSAL on the client side after mount
    async function initializeMsal() {
      try {
        let config;
        
        try {
          // Try synchronous config first (works with direct env vars)
          config = getMsalConfigSync();
        } catch (syncError) {
          // If sync config fails due to placeholders, this is expected in container environments
          // The runtime config will be loaded by the ConfigStatus component
          console.log('Using runtime configuration for MSAL');
          setError('Waiting for configuration to load...');
          return;
        }
        
        // Additional validation to ensure we're not using placeholder values
        if (config.auth.clientId === 'build-time-placeholder') {
          throw new Error('MSAL configuration is using placeholder values. Please check your environment variables.');
        }

        const instance = new PublicClientApplication(config);
        setMsalInstance(instance);
        setError(null);
      } catch (err: any) {
        console.error('Failed to initialize MSAL:', err);
        setError(err.message || 'Failed to initialize authentication');
      }
    }

    initializeMsal();
  }, []);

  // Show loading state while MSAL initializes
  if (!msalInstance) {
    if (error) {
      // For configuration errors, still show the children (which includes ConfigStatus)
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50">
          <div className="max-w-md w-full p-6">
            {children}
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
