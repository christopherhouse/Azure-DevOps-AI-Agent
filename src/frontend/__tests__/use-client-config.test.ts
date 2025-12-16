/**
 * Tests for the useClientConfig hook
 */

import { renderHook, waitFor } from '@testing-library/react'
import { useClientConfig, getCachedClientConfig, setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config'
import { createMockClientConfigOidcOnly } from './test-helpers'

// Mock fetch
global.fetch = jest.fn()

const mockClientConfig = createMockClientConfigOidcOnly()

describe('useClientConfig', () => {
  beforeEach(() => {
    // Clear cache before each test
    clearCachedClientConfig()
    // Reset fetch mock
    jest.clearAllMocks()
  })

  it('should load client configuration successfully', async () => {
    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockClientConfig
    })

    const { result } = renderHook(() => useClientConfig())

    // Initially loading
    expect(result.current.loading).toBe(true)
    expect(result.current.config).toBe(null)
    expect(result.current.error).toBe(null)

    // Wait for config to load
    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    expect(result.current.config).toEqual(mockClientConfig)
    expect(result.current.error).toBe(null)
  })

  it('should handle fetch error response', async () => {
    const errorResponse = {
      error: 'Configuration Error',
      message: 'AZURE_TENANT_ID environment variable is not set',
      details: 'Please check that required environment variables are set'
    }

    ;(fetch as jest.Mock).mockResolvedValueOnce({
      ok: false,
      json: async () => errorResponse
    })

    const { result } = renderHook(() => useClientConfig())

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    expect(result.current.config).toBe(null)
    expect(result.current.error).toEqual(errorResponse)
  })

  it('should handle network error', async () => {
    ;(fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'))

    const { result } = renderHook(() => useClientConfig())

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    expect(result.current.config).toBe(null)
    expect(result.current.error).toEqual({
      error: 'Network Error',
      message: 'Failed to load client configuration',
      details: 'Network error'
    })
  })

  it('should call the correct API endpoint', async () => {
    ;(fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockClientConfig
    })

    renderHook(() => useClientConfig())

    await waitFor(() => {
      expect(fetch).toHaveBeenCalledWith('/api/clientConfig')
    })
  })
})

describe('Client config cache utilities', () => {
  beforeEach(() => {
    clearCachedClientConfig()
  })

  it('should cache and retrieve client config', () => {
    expect(getCachedClientConfig()).toBe(null)

    setCachedClientConfig(mockClientConfig)
    expect(getCachedClientConfig()).toEqual(mockClientConfig)
  })

  it('should clear cached client config', () => {
    setCachedClientConfig(mockClientConfig)
    expect(getCachedClientConfig()).toEqual(mockClientConfig)

    clearCachedClientConfig()
    expect(getCachedClientConfig()).toBe(null)
  })
})