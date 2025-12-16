/**
 * Tests for Application Insights telemetry integration
 */

import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import {
  initializeTelemetry,
  trackPageView,
  trackAuthEvent,
  trackApiCall,
  trackException,
  trackChatMessage,
  trackEvent,
  trackTrace,
  flushTelemetry,
  setAuthenticatedUserContext,
  clearAuthenticatedUserContext,
  _resetTelemetry,
} from '@/lib/telemetry';

// Mock the Application Insights module - use __mocks__ directory
jest.mock('@microsoft/applicationinsights-web');

describe('Telemetry', () => {
  let mockAppInsights: any;
  let MockApplicationInsightsConstructor: jest.Mock;

  beforeEach(() => {
    // Reset the singleton
    _resetTelemetry();

    // Reset all mocks
    jest.clearAllMocks();

    // Create mock Application Insights instance
    mockAppInsights = {
      loadAppInsights: jest.fn(),
      trackPageView: jest.fn(),
      trackEvent: jest.fn(),
      trackDependencyData: jest.fn(),
      trackException: jest.fn(),
      trackTrace: jest.fn(),
      flush: jest.fn(),
      setAuthenticatedUserContext: jest.fn(),
      clearAuthenticatedUserContext: jest.fn(),
    };

    // Get the mocked constructor
    MockApplicationInsightsConstructor = ApplicationInsights as unknown as jest.Mock;
    MockApplicationInsightsConstructor.mockReturnValue(mockAppInsights);

    // Mock window.__CLIENT_CONFIG__ with telemetry enabled
    (global as any).window = {
      __CLIENT_CONFIG__: {
        telemetry: {
          connectionString:
            'InstrumentationKey=test-key;IngestionEndpoint=https://test.com',
          enabled: true,
        },
        debug: false,
      },
    };
  });

  afterEach(() => {
    // Clean up window mock
    delete (global as any).window;
    _resetTelemetry();
  });

  describe('initializeTelemetry', () => {
    it('should not initialize when telemetry is disabled', () => {
      const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation();
      
      // Set up window with disabled telemetry
      (global as any).window = {
        __CLIENT_CONFIG__: {
          telemetry: {
            connectionString: 'InstrumentationKey=test-key;IngestionEndpoint=https://test.com',
            enabled: false,
          },
          debug: false,
        },
      };

      initializeTelemetry();

      expect(MockApplicationInsightsConstructor).not.toHaveBeenCalled();
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        'Application Insights is not configured or not enabled'
      );
      consoleWarnSpy.mockRestore();
    });

    it('should not initialize when connection string is missing', () => {
      (global as any).window.__CLIENT_CONFIG__.telemetry.connectionString = '';

      initializeTelemetry();

      expect(MockApplicationInsightsConstructor).not.toHaveBeenCalled();
    });

    it('should handle initialization errors gracefully', () => {
      const consoleErrorSpy = jest
        .spyOn(console, 'error')
        .mockImplementation();
      MockApplicationInsightsConstructor.mockImplementation(() => {
        throw new Error('Initialization failed');
      });

      initializeTelemetry();

      expect(consoleErrorSpy).toHaveBeenCalledWith(
        'Failed to initialize Application Insights:',
        expect.any(Error)
      );
      consoleErrorSpy.mockRestore();
    });
  });

  describe('trackPageView', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks(); // Clear the initial trackPageView call
    });

    it('should track page view with all parameters', () => {
      trackPageView('TestPage', '/test', { custom: 'property' });

      expect(mockAppInsights.trackPageView).toHaveBeenCalledWith({
        name: 'TestPage',
        uri: '/test',
        properties: { custom: 'property' },
      });
    });

    it('should track page view with minimal parameters', () => {
      trackPageView();

      expect(mockAppInsights.trackPageView).toHaveBeenCalledWith({
        name: undefined,
        uri: undefined,
        properties: undefined,
      });
    });
  });

  describe('trackAuthEvent', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should track authentication event with properties', () => {
      trackAuthEvent('login_success', { userId: '123' });

      expect(mockAppInsights.trackEvent).toHaveBeenCalledWith({
        name: 'Auth_login_success',
        properties: { userId: '123' },
      });
    });

    it('should track authentication event without properties', () => {
      trackAuthEvent('logout');

      expect(mockAppInsights.trackEvent).toHaveBeenCalledWith({
        name: 'Auth_logout',
        properties: undefined,
      });
    });
  });

  describe('trackApiCall', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should track successful API call', () => {
      trackApiCall('GET', '/api/users', 200, 150, true);

      expect(mockAppInsights.trackDependencyData).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'GET /api/users',
          duration: 150,
          success: true,
          responseCode: 200,
          type: 'HTTP',
          data: '/api/users',
          target: '/api/users',
        })
      );
    });

    it('should track failed API call', () => {
      trackApiCall('POST', '/api/data', 500, 200, false);

      expect(mockAppInsights.trackDependencyData).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'POST /api/data',
          duration: 200,
          success: false,
          responseCode: 500,
          type: 'HTTP',
        })
      );
    });

    it('should infer success from status code when not provided', () => {
      trackApiCall('GET', '/api/test', 404, 100);

      expect(mockAppInsights.trackDependencyData).toHaveBeenCalledWith(
        expect.objectContaining({
          success: false,
          responseCode: 404,
        })
      );
    });
  });

  describe('trackException', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should track exception with properties and severity', () => {
      const error = new Error('Test error');
      trackException(error, { context: 'test' }, 3);

      expect(mockAppInsights.trackException).toHaveBeenCalledWith({
        exception: error,
        properties: { context: 'test' },
        severityLevel: 3,
      });
    });

    it('should fallback to console.error when AI is not initialized', () => {
      const consoleErrorSpy = jest
        .spyOn(console, 'error')
        .mockImplementation();
      const error = new Error('Test error');

      // Don't initialize, so AI is null
      delete (global as any).window;

      trackException(error, { context: 'test' });

      expect(consoleErrorSpy).toHaveBeenCalledWith('Exception:', error, {
        context: 'test',
      });
      consoleErrorSpy.mockRestore();

      // Restore window for other tests
      (global as any).window = {
        __CLIENT_CONFIG__: {
          telemetry: {
            connectionString:
              'InstrumentationKey=test-key;IngestionEndpoint=https://test.com',
            enabled: true,
          },
          debug: false,
        },
      };
    });
  });

  describe('trackChatMessage', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should track chat message with all parameters', () => {
      trackChatMessage('user', 150);

      expect(mockAppInsights.trackEvent).toHaveBeenCalledWith({
        name: 'ChatMessage',
        properties: {
          messageType: 'user',
          messageLength: 150,
        },
      });
    });

    it('should track chat message without length', () => {
      trackChatMessage('assistant');

      expect(mockAppInsights.trackEvent).toHaveBeenCalledWith({
        name: 'ChatMessage',
        properties: {
          messageType: 'assistant',
          messageLength: undefined,
        },
      });
    });
  });

  describe('trackEvent', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should track custom event with properties and measurements', () => {
      trackEvent('CustomEvent', { prop: 'value' }, { metric: 42 });

      expect(mockAppInsights.trackEvent).toHaveBeenCalledWith({
        name: 'CustomEvent',
        properties: { prop: 'value' },
        measurements: { metric: 42 },
      });
    });

    it('should track event with minimal parameters', () => {
      trackEvent('SimpleEvent');

      expect(mockAppInsights.trackEvent).toHaveBeenCalledWith({
        name: 'SimpleEvent',
        properties: undefined,
        measurements: undefined,
      });
    });
  });

  describe('trackTrace', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should track trace message with all parameters', () => {
      trackTrace('Debug message', 1, { context: 'debug' });

      expect(mockAppInsights.trackTrace).toHaveBeenCalledWith({
        message: 'Debug message',
        severityLevel: 1,
        properties: { context: 'debug' },
      });
    });

    it('should track trace with minimal parameters', () => {
      trackTrace('Simple message');

      expect(mockAppInsights.trackTrace).toHaveBeenCalledWith({
        message: 'Simple message',
        severityLevel: undefined,
        properties: undefined,
      });
    });
  });

  describe('flushTelemetry', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should flush telemetry data', () => {
      flushTelemetry();

      expect(mockAppInsights.flush).toHaveBeenCalled();
    });
  });

  describe('setAuthenticatedUserContext', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should set authenticated user context with account ID', () => {
      setAuthenticatedUserContext('user123', 'account456');

      expect(mockAppInsights.setAuthenticatedUserContext).toHaveBeenCalledWith(
        'user123',
        'account456'
      );
    });

    it('should set authenticated user context without account ID', () => {
      setAuthenticatedUserContext('user123');

      expect(mockAppInsights.setAuthenticatedUserContext).toHaveBeenCalledWith(
        'user123',
        undefined
      );
    });
  });

  describe('clearAuthenticatedUserContext', () => {
    beforeEach(() => {
      initializeTelemetry();
      jest.clearAllMocks();
    });

    it('should clear authenticated user context', () => {
      clearAuthenticatedUserContext();

      expect(mockAppInsights.clearAuthenticatedUserContext).toHaveBeenCalled();
    });
  });

  describe('SSR safety', () => {
    it('should not initialize on server-side', () => {
      delete (global as any).window;

      initializeTelemetry();

      expect(MockApplicationInsightsConstructor).not.toHaveBeenCalled();
    });

    it('should handle tracking calls gracefully when not initialized', () => {
      // Don't initialize by removing window
      delete (global as any).window;

      trackEvent('TestEvent');
      trackPageView('TestPage');
      trackAuthEvent('test');

      // Should not throw errors and not call the mock methods
      expect(mockAppInsights.trackEvent).not.toHaveBeenCalled();
      expect(mockAppInsights.trackPageView).not.toHaveBeenCalled();
    });
  });
});
