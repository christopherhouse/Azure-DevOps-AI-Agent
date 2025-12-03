/**
 * Tests for conversationId functionality.
 *
 * This test suite verifies that:
 * 1. A conversationId (GUID) is generated when useChat hook initializes
 * 2. The conversationId remains the same across messages in a session
 * 3. A new conversationId is generated when clearing chat (starting a new session)
 * 4. The conversationId is always included in API requests
 */

import { renderHook, act, waitFor } from '@testing-library/react';

// Mock the api-client before importing the hook
jest.mock('../src/services/api-client', () => ({
  apiClient: {
    sendMessage: jest.fn(),
    getChatHistory: jest.fn(),
  },
}));

// Mock telemetry
jest.mock('../src/lib/telemetry', () => ({
  trackChatMessage: jest.fn(),
  trackEvent: jest.fn(),
}));

// Now import the hook and api-client (after mocking)
import { useChat } from '../src/hooks/use-chat';
import { apiClient } from '../src/services/api-client';

// UUID regex pattern
const uuidRegex =
  /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

describe('ConversationId Functionality', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('useChat hook', () => {
    it('generates a conversationId (GUID) on initialization', () => {
      const { result } = renderHook(() => useChat());

      expect(result.current.conversationId).toBeDefined();
      expect(result.current.conversationId).not.toBeNull();
      expect(result.current.conversationId).toMatch(uuidRegex);
    });

    it('maintains the same conversationId across multiple messages', async () => {
      const mockResponse = {
        success: true,
        data: {
          message: 'AI response',
          conversation_id: 'backend-conv-id',
          timestamp: new Date().toISOString(),
        },
      };
      (apiClient.sendMessage as jest.Mock).mockResolvedValue(mockResponse);

      const { result } = renderHook(() => useChat());

      const initialConversationId = result.current.conversationId;

      // Send first message
      await act(async () => {
        await result.current.sendMessage('First message');
      });

      // Verify conversationId remains the same
      expect(result.current.conversationId).toBe(initialConversationId);

      // Send second message
      await act(async () => {
        await result.current.sendMessage('Second message');
      });

      // Verify conversationId still remains the same
      expect(result.current.conversationId).toBe(initialConversationId);
    });

    it('generates a new conversationId when clearChat is called', () => {
      const { result } = renderHook(() => useChat());

      const initialConversationId = result.current.conversationId;
      expect(initialConversationId).toMatch(uuidRegex);

      // Clear chat (starts a new session)
      act(() => {
        result.current.clearChat();
      });

      const newConversationId = result.current.conversationId;

      // Verify a new conversationId was generated
      expect(newConversationId).toMatch(uuidRegex);
      expect(newConversationId).not.toBe(initialConversationId);
    });

    it('includes conversationId in API request payload', async () => {
      const mockResponse = {
        success: true,
        data: {
          message: 'AI response',
          conversation_id: 'backend-conv-id',
          timestamp: new Date().toISOString(),
        },
      };
      (apiClient.sendMessage as jest.Mock).mockResolvedValue(mockResponse);

      const { result } = renderHook(() => useChat());

      const conversationId = result.current.conversationId;

      await act(async () => {
        await result.current.sendMessage('Test message');
      });

      // Verify the API was called with the conversationId
      expect(apiClient.sendMessage).toHaveBeenCalledWith({
        message: 'Test message',
        conversationId: conversationId,
      });
    });

    it('never sends undefined or null conversationId', async () => {
      const mockResponse = {
        success: true,
        data: {
          message: 'AI response',
          conversation_id: 'backend-conv-id',
          timestamp: new Date().toISOString(),
        },
      };
      (apiClient.sendMessage as jest.Mock).mockResolvedValue(mockResponse);

      const { result } = renderHook(() => useChat());

      await act(async () => {
        await result.current.sendMessage('Test message');
      });

      const callArgs = (apiClient.sendMessage as jest.Mock).mock.calls[0][0];
      expect(callArgs.conversationId).toBeDefined();
      expect(callArgs.conversationId).not.toBeNull();
      expect(callArgs.conversationId).not.toBeUndefined();
    });
  });

  describe('ChatState type', () => {
    it('conversationId is always a string (not null)', () => {
      const { result } = renderHook(() => useChat());

      // TypeScript ensures conversationId is string, not string | null
      // This test verifies runtime behavior matches the type
      expect(typeof result.current.conversationId).toBe('string');
      expect(result.current.conversationId.length).toBeGreaterThan(0);
    });
  });
});
