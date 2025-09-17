/**
 * Type definitions for the Azure DevOps AI Agent frontend.
 */

// Authentication types
export interface User {
  id: string;
  email: string;
  name: string;
  tenantId: string;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  accessToken: string | null;
  error: string | null;
  isLoading: boolean;
}

// Chat types
export interface ChatMessage {
  id: string;
  content: string;
  role: 'user' | 'assistant';
  timestamp: Date;
  conversationId?: string;
  citations?: Citation[];
  suggestions?: string[];
}

export interface ChatState {
  messages: ChatMessage[];
  isLoading: boolean;
  error: string | null;
  conversationId: string | null;
}

// Citation types
export interface Citation {
  title: string;
  url?: string;
  type?: string;
  description?: string;
}

// API types
export interface ApiResponse<T = any> {
  data?: T;
  error?: string;
  success: boolean;
}

export interface ChatRequest {
  message: string;
  conversation_id?: string;
  context?: Record<string, any>;
}

export interface ChatResponse {
  message: string;
  conversation_id: string;
  timestamp: string;
  suggestions?: string[];
  citations?: Citation[];
  metadata?: Record<string, any>;
}

// Backend status types
export interface BackendStatus {
  status: 'healthy' | 'unhealthy' | 'unknown';
  version?: string;
  uptime?: number;
  services?: Record<string, 'up' | 'down'>;
}

// Error types
export interface AppError {
  message: string;
  code?: string;
  details?: Record<string, any>;
}

// UI Component types
export interface LoadingProps {
  size?: 'small' | 'medium' | 'large';
  message?: string;
}

export interface ButtonProps {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
  size?: 'small' | 'medium' | 'large';
  loading?: boolean;
  disabled?: boolean;
  onClick?: () => void;
  children: React.ReactNode;
  className?: string;
}

// Application state types
export interface AppState {
  auth: AuthState;
  chat: ChatState;
  ui: {
    theme: 'light' | 'dark';
    sidebarOpen: boolean;
  };
}
