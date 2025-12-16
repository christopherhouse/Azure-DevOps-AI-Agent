/**
 * Telemetry and Application Insights integration
 */

import { ApplicationInsights } from '@microsoft/applicationinsights-web';

// Define interface for window client config
interface ClientConfig {
  telemetry: {
    connectionString: string;
    enabled: boolean;
  };
  debug: boolean;
}

// Extend Window interface to include our custom property
declare global {
  interface Window {
    __CLIENT_CONFIG__?: ClientConfig;
  }
}

// Global instance of Application Insights
let appInsights: ApplicationInsights | null = null;

/**
 * Reset the Application Insights instance (for testing purposes)
 * @internal
 */
export const _resetTelemetry = (): void => {
  appInsights = null;
};

/**
 * Get or create the Application Insights instance
 */
const getAppInsights = (): ApplicationInsights | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  if (appInsights) {
    return appInsights;
  }

  // Get configuration from window object (set by ClientLayout)
  const config = window.__CLIENT_CONFIG__;

  if (!config?.telemetry?.connectionString || !config?.telemetry?.enabled) {
    console.warn('Application Insights is not configured or not enabled');
    return null;
  }

  try {
    appInsights = new ApplicationInsights({
      config: {
        connectionString: config.telemetry.connectionString,
        enableAutoRouteTracking: true,
        enableCorsCorrelation: true,
        enableRequestHeaderTracking: true,
        enableResponseHeaderTracking: true,
        enableUnhandledPromiseRejectionTracking: true,
        disableFetchTracking: false,
        disableAjaxTracking: false,
        autoTrackPageVisitTime: true,
        enableDebug: config.debug || false,
      },
    });

    appInsights.loadAppInsights();

    // Track initial page view
    appInsights.trackPageView();

    console.log('Application Insights initialized successfully');
    return appInsights;
  } catch (error) {
    console.error('Failed to initialize Application Insights:', error);
    return null;
  }
};

/**
 * Initialize telemetry
 */
export const initializeTelemetry = (): void => {
  if (typeof window !== 'undefined') {
    getAppInsights();
  }
};

/**
 * Track a page view
 */
export const trackPageView = (
  name?: string,
  uri?: string,
  properties?: Record<string, any>
): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.trackPageView({
      name,
      uri,
      properties,
    });
  }
};

/**
 * Track authentication events
 */
export const trackAuthEvent = (
  eventName: string,
  properties?: Record<string, any>
): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.trackEvent({
      name: `Auth_${eventName}`,
      properties,
    });
  }
};

/**
 * Track API calls as dependencies
 */
export const trackApiCall = (
  method: string,
  url: string,
  statusCode: number,
  duration: number,
  success?: boolean
): void => {
  const ai = getAppInsights();
  if (ai) {
    // Use method and URL as ID for better correlation of similar API calls
    // This allows Application Insights to group calls to the same endpoint
    ai.trackDependencyData({
      id: `${method}_${url}`,
      name: `${method} ${url}`,
      duration,
      success: success ?? (statusCode >= 200 && statusCode < 400),
      responseCode: statusCode,
      type: 'HTTP',
      data: url,
      target: url,
    });
  }
};

/**
 * Track exceptions
 */
export const trackException = (
  error: Error,
  properties?: Record<string, any>,
  severityLevel?: number
): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.trackException({
      exception: error,
      properties,
      severityLevel,
    });
  } else {
    // Fallback to console if AI is not available
    console.error('Exception:', error, properties);
  }
};

/**
 * Track chat messages as custom events
 */
export const trackChatMessage = (
  messageType: string,
  messageLength?: number
): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.trackEvent({
      name: 'ChatMessage',
      properties: {
        messageType,
        messageLength,
      },
    });
  }
};

/**
 * Track custom events
 */
export const trackEvent = (
  eventName: string,
  properties?: Record<string, any>,
  measurements?: Record<string, number>
): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.trackEvent({
      name: eventName,
      properties,
      measurements,
    });
  }
};

/**
 * Track a trace/log message
 */
export const trackTrace = (
  message: string,
  severityLevel?: number,
  properties?: Record<string, any>
): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.trackTrace({
      message,
      severityLevel,
      properties,
    });
  }
};

/**
 * Flush telemetry data (useful before page unload)
 */
export const flushTelemetry = (): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.flush();
  }
};

/**
 * Set authenticated user context
 */
export const setAuthenticatedUserContext = (
  userId: string,
  accountId?: string
): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.setAuthenticatedUserContext(userId, accountId);
  }
};

/**
 * Clear authenticated user context
 */
export const clearAuthenticatedUserContext = (): void => {
  const ai = getAppInsights();
  if (ai) {
    ai.clearAuthenticatedUserContext();
  }
};
