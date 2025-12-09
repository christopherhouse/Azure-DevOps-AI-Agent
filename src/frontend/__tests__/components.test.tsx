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

  it('displays timestamp for messages from today', () => {
    const todayMessage: ChatMessage = {
      ...mockChatMessage,
      timestamp: new Date(), // Current time
    };

    render(<ChatMessageComponent message={todayMessage} />);
    
    // Should display just the time (e.g., "12:34 PM" or "12:34")
    // The exact format depends on the user's locale, but it should not contain the date
    expect(screen.getByText(/\d{1,2}:\d{2}/)).toBeInTheDocument();
  });

  it('displays timestamp with date for older messages', () => {
    const olderMessage: ChatMessage = {
      ...mockChatMessage,
      timestamp: new Date('2024-01-01T12:00:00Z'), // Past date
    };

    render(<ChatMessageComponent message={olderMessage} />);
    
    // Should display date and time (e.g., "Jan 1, 12:00 PM")
    // The exact format depends on the user's locale
    expect(screen.getByText(/\d{1,2}:\d{2}/)).toBeInTheDocument();
  });

  it('handles valid Date objects for timestamp', () => {
    const messageWithValidDate: ChatMessage = {
      ...mockChatMessage,
      timestamp: new Date('2024-06-15T14:30:00Z'),
    };

    render(<ChatMessageComponent message={messageWithValidDate} />);
    
    // Should not display "Invalid date"
    expect(screen.queryByText('Invalid date')).not.toBeInTheDocument();
  });

  it('renders markdown tables with proper styling', () => {
    const markdownTableMessage: ChatMessage = {
      ...mockChatMessage,
      role: 'assistant',
      format: 'markdown',
      content: '| Column 1 | Column 2 |\n|----------|----------|\n| Value 1  | Value 2  |',
    };

    const { container } = render(<ChatMessageComponent message={markdownTableMessage} />);
    
    // Check that the markdown container has proper styling classes
    const markdownContainer = container.querySelector('.prose');
    expect(markdownContainer).toBeInTheDocument();
    expect(markdownContainer).toHaveClass('prose-sm');
    expect(markdownContainer).toHaveClass('max-w-none');
    
    // Check that overflow-x-auto wrapper exists for scrolling
    const scrollableContainer = container.querySelector('.overflow-x-auto');
    expect(scrollableContainer).toBeInTheDocument();
  });

  it('applies different max-width for user vs assistant messages', () => {
    const userMessage: ChatMessage = {
      ...mockChatMessage,
      role: 'user',
    };

    const assistantMessage: ChatMessage = {
      ...mockChatMessage,
      role: 'assistant',
    };

    const { container: userContainer } = render(<ChatMessageComponent message={userMessage} />);
    const { container: assistantContainer } = render(<ChatMessageComponent message={assistantMessage} />);
    
    // User messages should have constrained width
    const userWrapper = userContainer.querySelector('.max-w-xs');
    expect(userWrapper).toBeInTheDocument();
    
    // Assistant messages should have full width
    const assistantWrapper = assistantContainer.querySelector('.max-w-full');
    expect(assistantWrapper).toBeInTheDocument();
  });
});