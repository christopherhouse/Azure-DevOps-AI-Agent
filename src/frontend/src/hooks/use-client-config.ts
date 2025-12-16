/**
 * Hook for loading client configuration from the server-side API
 *
 * This hook fetches configuration from /api/clientConfig immediately when the page loads
 * to ensure configuration values are available to configure MSAL and the rest of the app.
 *
 * This replaces the NEXT_PUBLIC_* environment variable approach to enable
 * building once and deploying multiple times with different runtime configurations.
 */

import { useState, useEffect } from 'react';

export interface ClientConfig {
  azure: {
    tenantId: string;
    clientId: string;
    authority: string;
    redirectUri: string;
    scopes: string[];
  };
  backend: {
    url: string;
  };
  frontend: {
    url: string;
  };
  telemetry: {
    connectionString: string;
    enabled: boolean;
  };
  debug: boolean;
}

interface ConfigError {
  error: string;
  message: string;
  details: string;
}

interface UseClientConfigResult {
  config: ClientConfig | null;
  loading: boolean;
  error: ConfigError | null;
}

export function useClientConfig(): UseClientConfigResult {
  const [config, setConfig] = useState<ClientConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ConfigError | null>(null);

  useEffect(() => {
    async function loadClientConfig() {
      try {
        const response = await fetch('/api/clientConfig');

        if (!response.ok) {
          const errorData = await response.json();
          setError(errorData);
          return;
        }

        const configData = await response.json();
        setConfig(configData);
        // Cache the config immediately when loaded so other components can access it
        setCachedClientConfig(configData);
      } catch (err) {
        setError({
          error: 'Network Error',
          message: 'Failed to load client configuration',
          details: err instanceof Error ? err.message : 'Unknown network error',
        });
      } finally {
        setLoading(false);
      }
    }

    loadClientConfig();
  }, []);

  return { config, loading, error };
}

/**
 * Synchronous access to config (for cases where we know config is already loaded)
 * Returns null if config is not yet loaded
 */
let cachedClientConfig: ClientConfig | null = null;

export function getCachedClientConfig(): ClientConfig | null {
  return cachedClientConfig;
}

export function setCachedClientConfig(config: ClientConfig): void {
  cachedClientConfig = config;
}

/**
 * Clear cached config (useful for testing)
 */
export function clearCachedClientConfig(): void {
  cachedClientConfig = null;
}
