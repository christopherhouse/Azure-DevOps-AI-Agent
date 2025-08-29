/**
 * Test to validate environment variable configuration and identify potential gaps
 * that could cause JWT tokens to miss the backend API scope
 */

import { GET } from '@/app/api/clientConfig/route';

// Mock NextResponse
jest.mock('next/server', () => ({
  NextResponse: {
    json: jest.fn((data, options) => ({
      json: async () => data,
      ok: !options?.status || options.status < 400,
      status: options?.status || 200,
    })),
  },
}));

describe('Environment Variable Validation', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    // Reset environment variables
    process.env = { ...originalEnv };
  });

  afterEach(() => {
    // Restore original environment variables
    process.env = originalEnv;
  });

  it('should return proper config when all environment variables are set', async () => {
    // Set all required environment variables
    process.env.AZURE_TENANT_ID = 'test-tenant-id';
    process.env.AZURE_CLIENT_ID = 'test-client-id';
    process.env.BACKEND_CLIENT_ID = 'backend-client-id';
    process.env.BACKEND_URL = 'http://localhost:8000';
    process.env.FRONTEND_URL = 'http://localhost:3000';

    const response = await GET();
    const config = await response.json();

    // Should include all 5 required scopes
    expect(config.azure.scopes).toEqual([
      'openid',
      'profile', 
      'User.Read',
      'email',
      'api://backend-client-id/Api.All'
    ]);
  });

  it('should fail when BACKEND_CLIENT_ID is missing (potential real-world issue)', async () => {
    // Set most environment variables but miss BACKEND_CLIENT_ID
    process.env.AZURE_TENANT_ID = 'test-tenant-id';
    process.env.AZURE_CLIENT_ID = 'test-client-id';
    // Missing: process.env.BACKEND_CLIENT_ID
    process.env.BACKEND_URL = 'http://localhost:8000';
    process.env.FRONTEND_URL = 'http://localhost:3000';

    const response = await GET();
    const error = await response.json();

    // Should return an error about missing BACKEND_CLIENT_ID
    expect(error.error).toBe('Configuration Error');
    expect(error.message).toContain('BACKEND_CLIENT_ID environment variable is not set');
  });

  it('should demonstrate the issue: missing backend client ID causes scope fallback', async () => {
    // This simulates what might be happening in the real deployment
    // where BACKEND_CLIENT_ID is not set, causing the /api/clientConfig to fail
    
    // Set only basic variables (missing BACKEND_CLIENT_ID)
    process.env.AZURE_TENANT_ID = 'test-tenant-id';
    process.env.AZURE_CLIENT_ID = 'test-client-id';
    process.env.BACKEND_URL = 'http://localhost:8000';
    process.env.FRONTEND_URL = 'http://localhost:3000';
    // Missing: BACKEND_CLIENT_ID

    const response = await GET();
    
    expect(response.ok).toBe(false);
    expect(response.status).toBe(500);
    
    const error = await response.json();
    expect(error.message).toContain('BACKEND_CLIENT_ID');
    
    console.log('❌ This would cause client config loading to fail');
    console.log('❌ Frontend would fall back to default scopes without backend API scope');
    console.log('❌ JWT tokens would only contain: openid, profile, User.Read, email');
    console.log('❌ Missing: api://backend-client-id/Api.All');
  });
});