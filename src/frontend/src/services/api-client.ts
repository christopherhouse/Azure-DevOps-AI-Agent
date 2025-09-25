/**
 * API client for communicating with the FastAPI backend.
 * Uses the new client config API instead of NEXT_PUBLIC_* environment variables.
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { getCachedClientConfig } from '@/hooks/use-client-config';
import { trackApiCall, trackException } from '@/lib/telemetry';
import { MfaHandler, MfaHandlerOptions } from '@/services/mfa-handler';
import type {
  ApiResponse,
  ChatRequest,
  ChatResponse,
  BackendStatus,
} from '@/types';

// Extend AxiosRequestConfig to include metadata
declare module 'axios' {
  interface InternalAxiosRequestConfig {
    metadata?: {
      startTime: number;
    };
  }
}

export class ApiClient {
  private client: AxiosInstance;
  private accessToken: string | null = null;
  private mfaHandler: MfaHandler | null = null;

  constructor() {
    // Initialize axios client with a placeholder URL
    // The actual baseURL will be set lazily when first request is made
    this.client = axios.create({
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor to add auth token and ensure baseURL is set
    this.client.interceptors.request.use(
      (config) => {
        // Lazily set the baseURL if not already set
        if (!config.baseURL) {
          const clientConfig = getCachedClientConfig();
          if (clientConfig) {
            config.baseURL = clientConfig.backend.url;
          } else {
            // Fallback for when config isn't loaded yet
            config.baseURL = 'http://localhost:8000';
          }
        }

        if (this.accessToken) {
          config.headers.Authorization = `Bearer ${this.accessToken}`;
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor for logging and error handling
    this.client.interceptors.response.use(
      (response: AxiosResponse) => {
        // Track successful API calls
        const startTime = response.config.metadata?.startTime || Date.now();
        const duration = Date.now() - startTime;

        trackApiCall(
          response.config.url || '',
          response.config.method?.toUpperCase() || 'GET',
          response.status,
          duration,
          true
        );

        return response;
      },
      async (error) => {
        // Track failed API calls
        const response = error.response;
        const startTime = error.config?.metadata?.startTime || Date.now();
        const duration = Date.now() - startTime;

        trackApiCall(
          error.config?.url || '',
          error.config?.method?.toUpperCase() || 'GET',
          response?.status || 0,
          duration,
          false
        );

        // Handle MFA challenge if we have a handler configured and it's an MFA error
        if (this.mfaHandler && this.mfaHandler.isMfaChallengeError(error)) {
          try {
            console.log('MFA challenge detected, attempting to handle...');
            const newToken = await this.mfaHandler.handleMfaChallengeFromError(error);
            
            // Update our token and retry the request
            this.setAccessToken(newToken);
            
            // Clone and retry the original request with the new token
            const originalRequest = { ...error.config };
            originalRequest.headers = originalRequest.headers || {};
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            
            return this.client.request(originalRequest);
          } catch (mfaError) {
            console.error('Failed to handle MFA challenge:', mfaError);
            // Return original error if MFA handling fails
          }
        }

        // Track exception for serious errors
        if (!response || response.status >= 500) {
          trackException(error, {
            url: error.config?.url,
            method: error.config?.method,
            status: response?.status,
          });
        }

        return Promise.reject(error);
      }
    );

    // Add request timestamp for duration tracking
    this.client.interceptors.request.use((config) => {
      config.metadata = { startTime: Date.now() };
      return config;
    });
  }

  /**
   * Set the MFA handler for automatic MFA challenge handling
   */
  setMfaHandler(mfaHandler: MfaHandler) {
    this.mfaHandler = mfaHandler;
  }

  /**
   * Set the access token for authenticated requests
   */
  setAccessToken(token: string | null) {
    this.accessToken = token;
  }

  /**
   * Set the backend API token for authenticated requests (preferred method)
   * This method is specifically for backend API tokens as part of the split token approach
   */
  setBackendApiToken(token: string | null) {
    this.accessToken = token;
  }

  /**
   * Get backend health status
   */
  async getHealthStatus(): Promise<BackendStatus> {
    try {
      const response = await this.client.get<BackendStatus>('/health');
      return response.data;
    } catch (error) {
      console.error('Health check failed:', error);
      return {
        status: 'unhealthy',
        services: {},
      };
    }
  }

  /**
   * Send a chat message
   */
  async sendMessage(request: ChatRequest): Promise<ApiResponse<ChatResponse>> {
    try {
      const response = await this.client.post<ChatResponse>(
        '/chat/message',
        request
      );
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage =
        error.response?.data?.detail ||
        error.message ||
        'Failed to send message';
      return {
        error: errorMessage,
        success: false,
      };
    }
  }

  /**
   * Get chat history
   */
  async getChatHistory(
    conversationId?: string
  ): Promise<ApiResponse<ChatResponse[]>> {
    try {
      const params = conversationId ? { conversation_id: conversationId } : {};
      const response = await this.client.get<ChatResponse[]>('/chat/history', {
        params,
      });
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage =
        error.response?.data?.detail ||
        error.message ||
        'Failed to get chat history';
      return {
        error: errorMessage,
        success: false,
      };
    }
  }

  /**
   * Get user projects from Azure DevOps
   */
  async getProjects(): Promise<ApiResponse<any[]>> {
    try {
      const response = await this.client.get<any[]>('/projects');
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage =
        error.response?.data?.detail ||
        error.message ||
        'Failed to get projects';
      return {
        error: errorMessage,
        success: false,
      };
    }
  }

  /**
   * Generic GET request
   */
  async get<T>(
    url: string,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.client.get<T>(url, config);
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage =
        error.response?.data?.detail || error.message || 'Request failed';
      return {
        error: errorMessage,
        success: false,
      };
    }
  }

  /**
   * Generic POST request
   */
  async post<T>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.client.post<T>(url, data, config);
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage =
        error.response?.data?.detail || error.message || 'Request failed';
      return {
        error: errorMessage,
        success: false,
      };
    }
  }
}

// Global API client instance
export const apiClient = new ApiClient();

// Export default for easier imports
export default apiClient;
