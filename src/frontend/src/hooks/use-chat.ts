/**
 * Chat hook for managing chat state and interactions.
 */

import { useState, useCallback } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/services/api-client';
import { trackChatMessage, trackEvent } from '@/lib/telemetry';
import type { ChatMessage, ChatState } from '@/types';

export function useChat() {
  const [chatState, setChatState] = useState<ChatState>({
    messages: [],
    isLoading: false,
    error: null,
    conversationId: null,
  });

  /**
   * Add a message to the chat
   */
  const addMessage = useCallback((message: Omit<ChatMessage, 'id'>): string => {
    const id = uuidv4();
    const chatMessage: ChatMessage = {
      ...message,
      id,
    };

    setChatState((prev) => ({
      ...prev,
      messages: [...prev.messages, chatMessage],
    }));

    // Track the message
    trackChatMessage('user', message.content.length);

    return id;
  }, []);

  /**
   * Update a message by ID
   */
  const updateMessage = useCallback(
    (messageId: string, updates: Partial<ChatMessage>) => {
      setChatState((prev) => ({
        ...prev,
        messages: prev.messages.map((msg) =>
          msg.id === messageId ? { ...msg, ...updates } : msg
        ),
      }));
    },
    []
  );

  /**
   * Send a message to the backend
   */
  const sendMessage = useCallback(
    async (content: string): Promise<boolean> => {
      if (!content.trim()) {
        return false;
      }

      setChatState((prev) => ({ ...prev, isLoading: true, error: null }));

      try {
        // Add user message
        addMessage({
          content: content.trim(),
          role: 'user',
          timestamp: new Date(),
          conversationId: chatState.conversationId || undefined,
        });

        // Send to backend
        const response = await apiClient.sendMessage({
          message: content.trim(),
          conversation_id: chatState.conversationId || undefined,
        });

        if (response.success && response.data) {
          // Update conversation ID if this is the first message
          if (!chatState.conversationId && response.data.conversation_id) {
            setChatState((prev) => ({
              ...prev,
              conversationId: response.data!.conversation_id,
            }));
          }

          // Add assistant response
          addMessage({
            content: response.data.message,
            role: 'assistant',
            timestamp: new Date(response.data.timestamp),
            conversationId: response.data.conversation_id,
          });

          setChatState((prev) => ({ ...prev, isLoading: false }));
          return true;
        } else {
          // Handle error
          const errorMessage = response.error || 'Failed to send message';
          setChatState((prev) => ({
            ...prev,
            error: errorMessage,
            isLoading: false,
          }));

          // Add error message to chat
          addMessage({
            content: `Sorry, I encountered an error: ${errorMessage}`,
            role: 'assistant',
            timestamp: new Date(),
            conversationId: chatState.conversationId || undefined,
          });

          return false;
        }
      } catch (error: any) {
        const errorMessage = error.message || 'An unexpected error occurred';
        setChatState((prev) => ({
          ...prev,
          error: errorMessage,
          isLoading: false,
        }));

        // Add error message to chat
        addMessage({
          content: `Sorry, I encountered an error: ${errorMessage}`,
          role: 'assistant',
          timestamp: new Date(),
          conversationId: chatState.conversationId || undefined,
        });

        return false;
      }
    },
    [addMessage, chatState.conversationId]
  );

  /**
   * Clear the chat
   */
  const clearChat = useCallback(() => {
    setChatState({
      messages: [],
      isLoading: false,
      error: null,
      conversationId: null,
    });

    trackEvent('ChatCleared');
  }, []);

  /**
   * Load chat history
   */
  const loadChatHistory = useCallback(
    async (conversationId?: string): Promise<boolean> => {
      setChatState((prev) => ({ ...prev, isLoading: true, error: null }));

      try {
        const response = await apiClient.getChatHistory(conversationId);

        if (response.success && response.data) {
          const messages: ChatMessage[] = response.data.map((msg) => ({
            id: uuidv4(),
            content: msg.message,
            role: 'assistant',
            timestamp: new Date(msg.timestamp),
            conversationId: msg.conversation_id,
          }));

          setChatState((prev) => ({
            ...prev,
            messages,
            conversationId: conversationId || null,
            isLoading: false,
          }));

          trackEvent('ChatHistoryLoaded', { messageCount: messages.length });
          return true;
        } else {
          setChatState((prev) => ({
            ...prev,
            error: response.error || 'Failed to load chat history',
            isLoading: false,
          }));
          return false;
        }
      } catch (error: any) {
        setChatState((prev) => ({
          ...prev,
          error: error.message || 'Failed to load chat history',
          isLoading: false,
        }));
        return false;
      }
    },
    []
  );

  /**
   * Retry last message
   */
  const retryLastMessage = useCallback(async (): Promise<boolean> => {
    const lastUserMessage = [...chatState.messages]
      .reverse()
      .find((msg) => msg.role === 'user');

    if (lastUserMessage) {
      return await sendMessage(lastUserMessage.content);
    }

    return false;
  }, [chatState.messages, sendMessage]);

  /**
   * Clear error state
   */
  const clearError = useCallback(() => {
    setChatState((prev) => ({ ...prev, error: null }));
  }, []);

  return {
    ...chatState,
    sendMessage,
    clearChat,
    loadChatHistory,
    retryLastMessage,
    clearError,
    addMessage,
    updateMessage,
  };
}

// Install uuid dependency
// Note: We'll need to install this later: npm install uuid @types/uuid
