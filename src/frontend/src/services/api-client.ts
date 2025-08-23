/**
 * API client for communicating with the FastAPI backend.
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { config } from '@/lib/config';
import { trackApiCall, trackException } from '@/lib/telemetry';
import type { ApiResponse, ChatRequest, ChatResponse, BackendStatus } from '@/types';

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

  constructor() {
    this.client = axios.create({
      baseURL: config.api.baseUrl,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor to add auth token
    this.client.interceptors.request.use(
      (config) => {
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
          duration
        );

        return response;
      },
      (error) => {
        // Track failed API calls
        const response = error.response;
        const startTime = error.config?.metadata?.startTime || Date.now();
        const duration = Date.now() - startTime;

        trackApiCall(
          error.config?.url || '',
          error.config?.method?.toUpperCase() || 'GET',
          response?.status || 0,
          duration
        );

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
   * Set the access token for authenticated requests
   */
  setAccessToken(token: string | null) {
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
      const response = await this.client.post<ChatResponse>('/chat/message', request);
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage = error.response?.data?.detail || error.message || 'Failed to send message';
      return {
        error: errorMessage,
        success: false,
      };
    }
  }

  /**
   * Get chat history
   */
  async getChatHistory(conversationId?: string): Promise<ApiResponse<ChatResponse[]>> {
    try {
      const params = conversationId ? { conversation_id: conversationId } : {};
      const response = await this.client.get<ChatResponse[]>('/chat/history', { params });
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage = error.response?.data?.detail || error.message || 'Failed to get chat history';
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
      const errorMessage = error.response?.data?.detail || error.message || 'Failed to get projects';
      return {
        error: errorMessage,
        success: false,
      };
    }
  }

  /**
   * Generic GET request
   */
  async get<T>(url: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
    try {
      const response = await this.client.get<T>(url, config);
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage = error.response?.data?.detail || error.message || 'Request failed';
      return {
        error: errorMessage,
        success: false,
      };
    }
  }

  /**
   * Generic POST request
   */
  async post<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
    try {
      const response = await this.client.post<T>(url, data, config);
      return {
        data: response.data,
        success: true,
      };
    } catch (error: any) {
      const errorMessage = error.response?.data?.detail || error.message || 'Request failed';
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