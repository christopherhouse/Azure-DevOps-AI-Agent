/**
 * Configuration status component
 * Shows whether runtime configuration is loaded and displays any errors
 */

'use client'

import { useRuntimeConfig } from '@/hooks/use-runtime-config'

export function ConfigStatus() {
  const { config, loading, error } = useRuntimeConfig()

  if (loading) {
    return (
      <div className="flex items-center justify-center p-4">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        <span className="ml-2 text-sm text-gray-600">Loading configuration...</span>
      </div>
    )
  }

  if (error) {
    return (
      <div className="max-w-md mx-auto mt-8 p-6 bg-red-50 border border-red-200 rounded-lg">
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
              {error.error}
            </h3>
          </div>
        </div>
        <div className="text-sm text-red-700">
          <p className="font-medium">{error.message}</p>
          <p className="mt-2 text-red-600">{error.details}</p>
          <div className="mt-4 p-3 bg-red-100 rounded">
            <p className="text-xs">
              Please check your environment configuration and contact your system administrator if this error persists.
            </p>
          </div>
        </div>
      </div>
    )
  }

  if (!config) {
    return (
      <div className="max-w-md mx-auto mt-8 p-6 bg-yellow-50 border border-yellow-200 rounded-lg">
        <div className="text-center text-yellow-800">
          <p>Configuration not available</p>
        </div>
      </div>
    )
  }

  // Configuration loaded successfully
  return (
    <div className="max-w-md mx-auto mt-8 p-6 bg-green-50 border border-green-200 rounded-lg">
      <div className="flex items-center mb-4">
        <div className="flex-shrink-0">
          <svg
            className="h-8 w-8 text-green-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div className="ml-3">
          <h3 className="text-lg font-medium text-green-800">
            Configuration Loaded
          </h3>
        </div>
      </div>
      <div className="text-sm text-green-700">
        <p>Environment: <span className="font-medium">{config.environment}</span></p>
        <p>Azure Tenant: <span className="font-medium">{config.azure.tenantId}</span></p>
        <p>Backend URL: <span className="font-medium">{config.backend.url}</span></p>
      </div>
    </div>
  )
}