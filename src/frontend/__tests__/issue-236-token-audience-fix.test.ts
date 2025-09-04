/**
 * Test for Issue #236: Update token for backend API call
 * @jest-environment node
 * 
 * The issue states:
 * - Currently: audience (aud) = "api://[backend app id]/Api.All" ❌
 * - Should be: audience (aud) = "[backend app id]" ✅ 
 * - And: scope (scp) = "api://[backend app id]/Api.All" ✅ (should remain)
 * 
 * Per MSAL documentation, when requesting scope "[client_id]", 
 * the audience should be set correctly to just the client ID.
 */

import { GET } from '@/app/api/clientConfig/route';

describe('Issue #236: Token audience fix', () => {
  beforeEach(() => {
    // Clear environment variables
    delete process.env.AZURE_TENANT_ID;
    delete process.env.AZURE_CLIENT_ID;
    delete process.env.BACKEND_CLIENT_ID;
    delete process.env.BACKEND_URL;
    delete process.env.FRONTEND_URL;
    delete process.env.AZURE_AUTHORITY;
    delete process.env.AZURE_REDIRECT_URI;
    delete process.env.AZURE_SCOPES;
  });

  it('should configure scopes to use backend client ID for correct audience claim', async () => {
    // Set up environment variables
    process.env.AZURE_TENANT_ID = 'test-tenant-id';
    process.env.AZURE_CLIENT_ID = 'frontend-client-id';
    process.env.BACKEND_CLIENT_ID = 'backend-api-client-id';
    process.env.BACKEND_URL = 'https://api.example.com';
    process.env.FRONTEND_URL = 'https://app.example.com';

    const response = await GET();
    const config = await response.json();

    // Verify the response is successful
    expect(response.status).toBe(200);
    expect(config.error).toBeUndefined();

    // Verify the scope includes just the backend client ID (not the full API URI)
    // This should cause MSAL to set the audience (aud) claim to just the client ID
    expect(config.azure.scopes).toContain('backend-api-client-id');
    
    // Verify it doesn't contain the full API URI (which was causing incorrect audience)
    expect(config.azure.scopes).not.toContain('api://backend-api-client-id/Api.All');

    // Verify all expected scopes are present
    expect(config.azure.scopes).toEqual([
      'openid',
      'profile', 
      'User.Read',
      'email',
      'backend-api-client-id'  // Backend client ID only for correct audience
    ]);
  });

  it('should explain the difference from previous behavior', async () => {
    // This test documents the change in behavior for Issue #236
    process.env.AZURE_TENANT_ID = 'test-tenant-id';
    process.env.AZURE_CLIENT_ID = 'frontend-client-id';
    process.env.BACKEND_CLIENT_ID = 'my-backend-api';
    process.env.BACKEND_URL = 'https://backend.example.com';
    process.env.FRONTEND_URL = 'https://frontend.example.com';

    const response = await GET();
    const config = await response.json();

    // BEFORE (Issue #236): 
    // Scope would be: "api://my-backend-api/Api.All"
    // This caused: audience (aud) = "api://my-backend-api/Api.All" ❌
    // And: scope (scp) = "api://my-backend-api/Api.All"

    // AFTER (Issue #236 fixed):
    // Scope is now: "my-backend-api" 
    // This should cause: audience (aud) = "my-backend-api" ✅
    // Per MSAL docs, requesting client ID as scope sets correct audience

    expect(config.azure.scopes).toContain('my-backend-api');
    expect(config.azure.scopes).not.toContain('api://my-backend-api/Api.All');
  });

  it('should maintain backward compatibility with OIDC scopes', async () => {
    // Ensure OIDC scopes are still included correctly
    process.env.AZURE_TENANT_ID = 'test-tenant-id';
    process.env.AZURE_CLIENT_ID = 'frontend-client-id';
    process.env.BACKEND_CLIENT_ID = 'backend-client-id';
    process.env.BACKEND_URL = 'https://api.example.com';
    process.env.FRONTEND_URL = 'https://app.example.com';

    const response = await GET();
    const config = await response.json();

    // OIDC scopes should remain unchanged
    expect(config.azure.scopes).toContain('openid');
    expect(config.azure.scopes).toContain('profile');
    expect(config.azure.scopes).toContain('User.Read');
    expect(config.azure.scopes).toContain('email');
    
    // Only the backend scope format should change
    expect(config.azure.scopes).toContain('backend-client-id');
  });
});