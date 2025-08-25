/**
 * Client-side layout wrapper with MSAL provider.
 */

'use client';

import React from 'react';
import { MsalProvider } from '@azure/msal-react';
import { PublicClientApplication } from '@azure/msal-browser';
import { msalConfig } from '@/lib/auth-config';

// Initialize MSAL instance
const msalInstance = new PublicClientApplication(msalConfig);

interface ClientLayoutProps {
  children: React.ReactNode;
}

export function ClientLayout({ children }: ClientLayoutProps) {
  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}

export default ClientLayout;
