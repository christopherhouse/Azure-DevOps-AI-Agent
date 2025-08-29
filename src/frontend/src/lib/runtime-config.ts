/**
 * Runtime configuration for server-side API routes
 *
 * This module provides functions for loading configuration at runtime,
 * particularly for the /api/config endpoint that serves configuration
 * to client components in containerized deployments.
 */

import { Config, loadConfig } from './config';
import { ClientConfig } from '@/hooks/use-runtime-config';

/**
 * Load runtime configuration on the server side
 * This reads environment variables at runtime and returns the full config
 */
export async function loadRuntimeConfig(): Promise<Config> {
  return loadConfig();
}

/**
 * Convert server config to client-safe configuration
 * This removes server-only properties and formats for client consumption
 */
export function getClientConfig(config: Config): ClientConfig {
  return {
    azure: {
      tenantId: config.azure.tenantId,
      clientId: config.azure.clientId,
      authority: config.azure.authority,
      redirectUri: config.azure.redirectUri,
      scopes: config.azure.scopes,
    },
    backend: {
      url: config.api.baseUrl,
    },
    telemetry: {
      connectionString: config.telemetry.connectionString,
      enabled: config.telemetry.enabled,
    },
    environment: config.environment,
    debug: config.debug,
  };
}
