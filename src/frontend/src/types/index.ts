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
  thoughtProcessId?: string;
  format?: 'markdown' | 'text'; // Format of the message content
}

export interface ChatState {
  messages: ChatMessage[];
  isLoading: boolean;
  error: string | null;
  conversationId: string;
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
  conversationId: string;
  context?: Record<string, any>;
}

export interface ChatResponse {
  message: string;
  conversation_id: string;
  format?: string; // Format of the message content (e.g., "markdown", "text")
  timestamp: string;
  suggestions?: string[];
  citations?: Citation[];
  metadata?: Record<string, any>;
  thoughtProcessId?: string;
}

// Thought process types
export interface ThoughtStep {
  id: string;
  description: string;
  type: string;
  timestamp: string;
  details?: Record<string, any>;
}

export interface ToolInvocation {
  toolName: string;
  parameters?: Record<string, any>;
  result?: any;
  status: string;
  errorMessage?: string;
  timestamp: string;
}

export interface ThoughtProcess {
  id: string;
  conversationId: string;
  messageId: string;
  steps: ThoughtStep[];
  toolInvocations: ToolInvocation[];
  startTime: string;
  endTime?: string;
  durationMs?: number;
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

// MFA challenge types
export interface MfaChallengeDetails {
  claimsChallenge: string;
  scopes: string[];
  correlationId?: string;
  errorCode: string;
  classification: string;
}

export interface MfaChallengeError {
  error: {
    code: number;
    message: string;
    type: 'mfa_required';
    details: MfaChallengeDetails;
  };
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
