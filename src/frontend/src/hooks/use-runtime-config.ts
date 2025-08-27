/**
 * Hook for loading runtime configuration on the client side
 * This fetches configuration from the API route that reads environment variables at runtime
 */

import { useState, useEffect } from 'react'

export interface ClientConfig {
  azure: {
    tenantId: string
    clientId: string
    authority: string
    redirectUri: string
    scopes: string[]
  }
  backend: {
    url: string
  }
  telemetry: {
    connectionString: string
    enabled: boolean
  }
  environment: string
  debug: boolean
}

interface ConfigError {
  error: string
  message: string
  details: string
}

interface UseRuntimeConfigResult {
  config: ClientConfig | null
  loading: boolean
  error: ConfigError | null
}

export function useRuntimeConfig(): UseRuntimeConfigResult {
  const [config, setConfig] = useState<ClientConfig | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ConfigError | null>(null)

  useEffect(() => {
    async function loadConfig() {
      try {
        const response = await fetch('/api/config')
        
        if (!response.ok) {
          const errorData = await response.json()
          setError(errorData)
          return
        }

        const configData = await response.json()
        setConfig(configData)
      } catch (err) {
        setError({
          error: 'Network Error',
          message: 'Failed to load configuration',
          details: err instanceof Error ? err.message : 'Unknown network error',
        })
      } finally {
        setLoading(false)
      }
    }

    loadConfig()
  }, [])

  return { config, loading, error }
}

/**
 * Legacy config interface for backward compatibility
 */
export interface Config {
  backendUrl: string
  frontendUrl: string
  environment: string
  debug: boolean
  api: {
    baseUrl: string
  }
  azure: {
    tenantId: string
    clientId: string
    authority: string
    redirectUri: string
    scopes: string[]
  }
  telemetry: {
    connectionString: string
    enabled: boolean
  }
  security: {
    sessionTimeout: number
    requireHttps: boolean
  }
}

/**
 * Convert new config format to legacy format for backward compatibility
 */
export function convertToLegacyConfig(clientConfig: ClientConfig): Config {
  return {
    backendUrl: clientConfig.backend.url,
    frontendUrl: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:3000',
    environment: clientConfig.environment,
    debug: clientConfig.debug,
    api: {
      baseUrl: clientConfig.backend.url,
    },
    azure: clientConfig.azure,
    telemetry: clientConfig.telemetry,
    security: {
      sessionTimeout: 3600, // Default value
      requireHttps: false, // Default value
    },
  }
}