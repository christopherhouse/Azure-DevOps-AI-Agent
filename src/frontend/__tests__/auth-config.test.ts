/**
 * Tests for MSAL configuration with new client config system
 */

import { 
  createMsalConfigFromClientConfig, 
  getMsalConfigSync, 
  getLoginRequest, 
  getTokenRequest,
  loginRequest,
  tokenRequest 
} from '@/lib/auth-config';
import { setCachedClientConfig, clearCachedClientConfig } from '@/hooks/use-client-config';

// Mock client config with backend API scope
const mockClientConfigWithBackendScope = {
  azure: {
    tenantId: 'test-tenant-id-12345',
    clientId: 'test-client-id-67890',
    authority: 'https://login.microsoftonline.com/test-tenant-id-12345',
    redirectUri: 'http://localhost:3000/auth/callback',
    scopes: ['openid', 'profile', 'User.Read', 'email', 'backend-client-id']
  },
  backend: {
    url: 'http://localhost:8000'
  },
  frontend: {
    url: 'http://localhost:3000'
  }
};

// Mock client config
const mockClientConfig = {
  azure: {
    tenantId: 'test-tenant-id-12345',
    clientId: 'test-client-id-67890',
    authority: 'https://login.microsoftonline.com/test-tenant-id-12345',
    redirectUri: 'http://localhost:3000/auth/callback',
    scopes: ['openid', 'profile', 'User.Read']
  },
  backend: {
    url: 'http://localhost:8000'
  },
  frontend: {
    url: 'http://localhost:3000'
  }
};

describe('MSAL Configuration with Client Config', () => {
  const originalWindow = global.window;

  beforeEach(() => {
    jest.resetModules();
    clearCachedClientConfig();
    // Mock browser environment
    (global as any).window = { location: { origin: 'http://localhost:3000' } };
  });

  afterEach(() => {
    clearCachedClientConfig();
    global.window = originalWindow;
  });

  it('should create MSAL config from client config', () => {
    const config = createMsalConfigFromClientConfig(mockClientConfig);

    expect(config.auth.clientId).toBe('test-client-id-67890');
    expect(config.auth.authority).toBe('https://login.microsoftonline.com/test-tenant-id-12345');
    expect(config.auth.redirectUri).toBe('http://localhost:3000/auth/callback');
    expect(config.cache.cacheLocation).toBe('sessionStorage');
  });

  it('should work with getMsalConfigSync when config is cached', () => {
    setCachedClientConfig(mockClientConfig);
    
    const config = getMsalConfigSync();

    expect(config.auth.clientId).toBe('test-client-id-67890');
    expect(config.auth.authority).toBe('https://login.microsoftonline.com/test-tenant-id-12345');
    expect(config.auth.redirectUri).toBe('http://localhost:3000/auth/callback');
  });

  it('should throw error when config is not cached for getMsalConfigSync', () => {
    expect(() => getMsalConfigSync()).toThrow('Client configuration not loaded yet. Use getMsalConfig() instead or ensure useClientConfig has completed loading.');
  });

  it('should use placeholder values during build time', () => {
    // Mock build time environment
    const originalWindow = global.window;
    delete (global as any).window;
    const originalNodeEnv = process.env.NODE_ENV;
    
    // Use Object.defineProperty to safely modify NODE_ENV
    Object.defineProperty(process.env, 'NODE_ENV', {
      value: 'production',
      writable: true,
      enumerable: true,
      configurable: true
    });

    const config = getMsalConfigSync();

    expect(config.auth.clientId).toBe('build-time-placeholder');
    expect(config.auth.authority).toBe('https://login.microsoftonline.com/build-time-placeholder');

    // Restore environment
    (global as any).window = originalWindow;
    if (originalNodeEnv !== undefined) {
      Object.defineProperty(process.env, 'NODE_ENV', {
        value: originalNodeEnv,
        writable: true,
        enumerable: true,
        configurable: true
      });
    }
  });

  it('should handle custom redirect URI in client config', () => {
    const customConfig = {
      ...mockClientConfig,
      azure: {
        ...mockClientConfig.azure,
        redirectUri: 'https://custom.domain.com/callback'
      }
    };

    const config = createMsalConfigFromClientConfig(customConfig);
    expect(config.auth.redirectUri).toBe('https://custom.domain.com/callback');
  });
});

describe('Dynamic Auth Requests', () => {
  beforeEach(() => {
    clearCachedClientConfig();
  });

  afterEach(() => {
    clearCachedClientConfig();
  });

  describe('getLoginRequest', () => {
    it('should return login request with default scopes when no config is cached', () => {
      const request = getLoginRequest();
      
      expect(request.scopes).toEqual(['openid', 'profile', 'User.Read', 'email']);
    });

    it('should return login request with scopes from cached client config', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      const request = getLoginRequest();
      
      expect(request.scopes).toEqual(['openid', 'profile', 'User.Read', 'email', 'backend-client-id']);
    });

    it('should include backend client ID when present in config', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      const request = getLoginRequest();
      
      expect(request.scopes).toContain('backend-client-id');
    });
  });

  describe('getTokenRequest', () => {
    it('should return token request with default scopes when no config is cached', () => {
      const request = getTokenRequest();
      
      expect(request.scopes).toEqual(['openid', 'profile', 'User.Read', 'email']);
      expect(request.account).toBe(null);
    });

    it('should return token request with scopes from cached client config', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      const request = getTokenRequest();
      
      expect(request.scopes).toEqual(['openid', 'profile', 'User.Read', 'email', 'backend-client-id']);
      expect(request.account).toBe(null);
    });

    it('should include backend client ID when present in config', () => {
      setCachedClientConfig(mockClientConfigWithBackendScope);
      
      const request = getTokenRequest();
      
      expect(request.scopes).toContain('backend-client-id');
    });
  });

  describe('Legacy exports (deprecated)', () => {
    it('should maintain backward compatibility for loginRequest', () => {
      expect(loginRequest.scopes).toEqual(['openid', 'profile', 'User.Read', 'email']);
    });

    it('should maintain backward compatibility for tokenRequest', () => {
      expect(tokenRequest.scopes).toEqual(['openid', 'profile', 'User.Read', 'email']);
      expect(tokenRequest.account).toBe(null);
    });
  });
});