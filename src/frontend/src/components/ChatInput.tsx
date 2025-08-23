/**
 * Chat input component for sending messages.
 */

import React, { useState, useRef, KeyboardEvent } from 'react';
import { Button } from './Button';

interface ChatInputProps {
  onSendMessage: (message: string) => Promise<boolean>;
  disabled?: boolean;
  loading?: boolean;
}

export function ChatInput({ onSendMessage, disabled = false, loading = false }: ChatInputProps) {
  const [message, setMessage] = useState('');
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const handleSubmit = async () => {
    if (!message.trim() || disabled || loading) {
      return;
    }

    const messageToSend = message.trim();
    setMessage('');
    
    // Auto-resize textarea
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
    }

    await onSendMessage(messageToSend);
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  const handleTextareaChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setMessage(e.target.value);
    
    // Auto-resize textarea
    const textarea = e.target;
    textarea.style.height = 'auto';
    textarea.style.height = Math.min(textarea.scrollHeight, 150) + 'px';
  };

  return (
    <div className="border-t border-gray-200 bg-white p-4">
      <div className="flex space-x-3">
        <div className="flex-1">
          <textarea
            ref={textareaRef}
            value={message}
            onChange={handleTextareaChange}
            onKeyDown={handleKeyDown}
            placeholder="Type your message here... (Press Enter to send, Shift+Enter for new line)"
            className="w-full resize-none border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            style={{ minHeight: '40px', maxHeight: '150px' }}
            disabled={disabled || loading}
            rows={1}
          />
        </div>
        <div>
          <Button
            onClick={handleSubmit}
            disabled={!message.trim() || disabled}
            loading={loading}
            variant="primary"
            size="medium"
            className="h-10"
          >
            Send
          </Button>
        </div>
      </div>
      <div className="text-xs text-gray-500 mt-2">
        Press Enter to send, Shift+Enter for a new line
      </div>
    </div>
  );
}

export default ChatInput;