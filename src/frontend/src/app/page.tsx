'use client';

import { useEffect } from 'react';
import { ClientLayout } from '@/components/ClientLayout';
import { Layout } from '@/components/Layout';
import { initializeTelemetry } from '@/lib/telemetry';

export default function Home() {
  useEffect(() => {
    // Initialize Application Insights on client side
    initializeTelemetry();
  }, []);

  return (
    <ClientLayout>
      <Layout />
    </ClientLayout>
  );
}
