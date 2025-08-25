'use client';

import { useEffect } from 'react';
import { ClientLayout } from '@/components/ClientLayout';
import { useMsal } from '@azure/msal-react';
import { Loading } from '@/components/Loading';

export default function AuthCallback() {
  return (
    <ClientLayout>
      <AuthCallbackContent />
    </ClientLayout>
  );
}

function AuthCallbackContent() {
  const { instance } = useMsal();

  useEffect(() => {
    // Handle the auth response
    instance
      .handleRedirectPromise()
      .then((response) => {
        if (response) {
          // Authentication successful, redirect to home
          window.location.href = '/';
        }
      })
      .catch((error) => {
        console.error('Authentication callback error:', error);
        // Redirect to home with error
        window.location.href = '/?error=auth_failed';
      });
  }, [instance]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <Loading size="large" message="Completing authentication..." />
    </div>
  );
}
