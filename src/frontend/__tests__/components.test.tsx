/**
 * Tests for React components.
 */

import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { Button } from '@/components/Button';
import { Loading } from '@/components/Loading';
import { ChatMessageComponent } from '@/components/ChatMessage';
import type { ChatMessage } from '@/types';

// Mock chat message for testing
const mockChatMessage: ChatMessage = {
  id: 'test-message-1',
  content: 'Hello, this is a test message',
  role: 'user',
  timestamp: new Date('2024-01-01T12:00:00Z'),
  conversationId: 'test-conversation',
};

describe('Button Component', () => {
  it('renders with default props', () => {
    render(<Button>Click me</Button>);
    
    const button = screen.getByRole('button');
    expect(button).toBeInTheDocument();
    expect(button).toHaveTextContent('Click me');
    expect(button).toHaveClass('bg-blue-600'); // primary variant
  });

  it('handles click events', () => {
    const handleClick = jest.fn();
    render(<Button onClick={handleClick}>Click me</Button>);
    
    fireEvent.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('disables when disabled prop is true', () => {
    render(<Button disabled>Disabled</Button>);
    
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
  });
});

describe('Loading Component', () => {
  it('renders with default props', () => {
    render(<Loading />);
    
    const spinner = screen.getByRole('status');
    expect(spinner).toBeInTheDocument();
    expect(spinner).toHaveClass('animate-spin');
  });

  it('renders with message', () => {
    render(<Loading message="Loading data..." />);
    
    expect(screen.getByText('Loading data...')).toBeInTheDocument();
  });
});

describe('ChatMessage Component', () => {
  it('renders user message correctly', () => {
    render(<ChatMessageComponent message={mockChatMessage} />);
    
    expect(screen.getByText('Hello, this is a test message')).toBeInTheDocument();
  });

  it('renders assistant message correctly', () => {
    const assistantMessage: ChatMessage = {
      ...mockChatMessage,
      role: 'assistant',
    };

    render(<ChatMessageComponent message={assistantMessage} />);
    
    expect(screen.getByText('Hello, this is a test message')).toBeInTheDocument();
  });
});