/**
 * Tests for the /api/clientConfig endpoint
 * @jest-environment node
 */

import { NextRequest } from 'next/server'

// Mock environment variables
const mockEnvVars = {
  AZURE_TENANT_ID: 'test-tenant-id',
  AZURE_CLIENT_ID: 'test-client-id',
  BACKEND_URL: 'http://localhost:8000',
  FRONTEND_URL: 'http://localhost:3000',
  AZURE_AUTHORITY: 'https://login.microsoftonline.com/test-tenant-id',
  AZURE_REDIRECT_URI: 'http://localhost:3000/auth/callback',
  AZURE_SCOPES: 'openid,profile,User.Read'
}

describe('/api/clientConfig', () => {
  let originalEnv: NodeJS.ProcessEnv
  let GET: any

  beforeEach(async () => {
    // Save original environment
    originalEnv = process.env

    // Set mock environment variables
    process.env = {
      ...originalEnv,
      ...mockEnvVars
    }

    // Dynamically import the GET function to ensure it uses the mocked environment
    const module = await import('@/app/api/clientConfig/route')
    GET = module.GET
  })

  afterEach(() => {
    // Restore original environment
    process.env = originalEnv
  })

  describe('GET', () => {
    it('should return client configuration with all required fields', async () => {
      const response = await GET()
      const data = await response.json()

      expect(response.status).toBe(200)
      expect(data).toEqual({
        azure: {
          tenantId: 'test-tenant-id',
          clientId: 'test-client-id',
          authority: 'https://login.microsoftonline.com/test-tenant-id',
          redirectUri: 'http://localhost:3000/auth/callback',
          scopes: ['openid', 'profile', 'User.Read']
        },
        backend: {
          url: 'http://localhost:8000/api'
        },
        frontend: {
          url: 'http://localhost:3000'
        }
      })
    })

    it('should use defaults for optional values when not provided', async () => {
      // Remove optional environment variables
      delete process.env.AZURE_AUTHORITY
      delete process.env.AZURE_REDIRECT_URI
      delete process.env.AZURE_SCOPES
      delete process.env.FRONTEND_URL

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(200)
      expect(data.azure.authority).toBe('https://login.microsoftonline.com/test-tenant-id')
      expect(data.azure.redirectUri).toBe('http://localhost:3000/auth/callback')
      expect(data.azure.scopes).toEqual(['openid', 'profile', 'User.Read'])
      expect(data.frontend.url).toBe('http://localhost:3000')
    })

    it('should return error when AZURE_TENANT_ID is missing', async () => {
      delete process.env.AZURE_TENANT_ID

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(500)
      expect(data.error).toBe('Configuration Error')
      expect(data.message).toBe('AZURE_TENANT_ID environment variable is not set')
      expect(data.details).toContain('AZURE_TENANT_ID')
    })

    it('should return error when AZURE_CLIENT_ID is missing', async () => {
      delete process.env.AZURE_CLIENT_ID

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(500)
      expect(data.error).toBe('Configuration Error')
      expect(data.message).toBe('AZURE_CLIENT_ID environment variable is not set')
      expect(data.details).toContain('AZURE_CLIENT_ID')
    })

    it('should return error when BACKEND_URL is missing', async () => {
      delete process.env.BACKEND_URL

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(500)
      expect(data.error).toBe('Configuration Error')
      expect(data.message).toBe('BACKEND_URL environment variable is not set')
      expect(data.details).toContain('BACKEND_URL')
    })

    it('should parse scopes correctly when provided as comma-separated string', async () => {
      process.env.AZURE_SCOPES = 'scope1, scope2 , scope3'

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(200)
      expect(data.azure.scopes).toEqual(['scope1', 'scope2', 'scope3'])
    })

    it('should handle custom authority and redirect URI', async () => {
      process.env.AZURE_AUTHORITY = 'https://custom.authority.com'
      process.env.AZURE_REDIRECT_URI = 'https://custom.app.com/callback'

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(200)
      expect(data.azure.authority).toBe('https://custom.authority.com')
      expect(data.azure.redirectUri).toBe('https://custom.app.com/callback')
    })

    it('should handle missing frontend URL in redirect URI generation', async () => {
      delete process.env.FRONTEND_URL
      delete process.env.AZURE_REDIRECT_URI

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(200)
      expect(data.azure.redirectUri).toBe('http://localhost:3000/auth/callback')
    })

    it('should not double-add /api suffix to backend URL', async () => {
      process.env.BACKEND_URL = 'http://localhost:8000/api'

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(200)
      expect(data.backend.url).toBe('http://localhost:8000/api')
    })

    it('should construct redirect URI from frontend URL when provided', async () => {
      process.env.FRONTEND_URL = 'https://myapp.region.azurecontainerapps.io'
      delete process.env.AZURE_REDIRECT_URI

      // Re-import the module to pick up env changes
      jest.resetModules()
      const module = await import('@/app/api/clientConfig/route')
      const response = await module.GET()
      const data = await response.json()

      expect(response.status).toBe(200)
      expect(data.azure.redirectUri).toBe('https://myapp.region.azurecontainerapps.io/auth/callback')
    })
  })
})