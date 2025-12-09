/**
 * Main chat interface component.
 */

import React, { useEffect, useRef, useState } from 'react';
import { ChatMessageComponent } from './ChatMessage';
import { ChatInput } from './ChatInput';
import { Button } from './Button';
import { Loading } from './Loading';
import { ThoughtProcess } from './ThoughtProcess';
import { useChat } from '@/hooks/use-chat';

type TabType = 'chat' | 'thought';

interface SelectedThoughtProcess {
  thoughtProcessId: string;
  conversationId: string;
}

export function ChatInterface() {
  const {
    messages,
    isLoading,
    error,
    sendMessage,
    clearChat,
    clearError,
    retryLastMessage,
  } = useChat();

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [activeTab, setActiveTab] = useState<TabType>('chat');
  const [selectedThoughtProcess, setSelectedThoughtProcess] =
    useState<SelectedThoughtProcess | null>(null);

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Get the latest assistant message with a thought process
  const getLatestThoughtProcess = (): SelectedThoughtProcess | null => {
    const assistantMessages = messages.filter(
      (msg) =>
        msg.role === 'assistant' && msg.thoughtProcessId && msg.conversationId
    );
    if (assistantMessages.length > 0) {
      const lastMessage = assistantMessages[assistantMessages.length - 1];
      return {
        thoughtProcessId: lastMessage.thoughtProcessId!,
        conversationId: lastMessage.conversationId!,
      };
    }
    return null;
  };

  // Handle tab change
  const handleTabChange = (tab: TabType) => {
    setActiveTab(tab);
    if (tab === 'thought' && !selectedThoughtProcess) {
      setSelectedThoughtProcess(getLatestThoughtProcess());
    }
  };

  // Handle message selection for thought process viewing
  const handleMessageThoughtProcessClick = (
    thoughtProcessId: string,
    conversationId: string
  ) => {
    setSelectedThoughtProcess({ thoughtProcessId, conversationId });
    setActiveTab('thought');
  };

  return (
    <div className="flex flex-col h-full bg-white">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-700 text-white">
        <div className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-xl font-semibold">
                🤖 Azure DevOps AI Agent
              </h1>
              <p className="text-blue-100 text-sm">
                Ask me anything about your Azure DevOps projects
              </p>
            </div>
            <div className="flex space-x-2">
              <Button
                variant="ghost"
                size="small"
                onClick={clearChat}
                className="text-white border-white hover:bg-blue-700"
              >
                Clear Chat
              </Button>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="border-b border-blue-500">
          <div className="flex space-x-0">
            <button
              onClick={() => handleTabChange('chat')}
              className={`px-6 py-3 text-sm font-medium transition-colors ${
                activeTab === 'chat'
                  ? 'bg-white text-blue-700 border-b-2 border-blue-700'
                  : 'text-blue-100 hover:text-white hover:bg-blue-600'
              }`}
            >
              💬 Chat
            </button>
            <button
              onClick={() => handleTabChange('thought')}
              className={`px-6 py-3 text-sm font-medium transition-colors ${
                activeTab === 'thought'
                  ? 'bg-white text-blue-700 border-b-2 border-blue-700'
                  : 'text-blue-100 hover:text-white hover:bg-blue-600'
              }`}
              disabled={!getLatestThoughtProcess()}
            >
              🧠 Thought Process
              {!getLatestThoughtProcess() && (
                <span className="ml-1 text-xs opacity-75">(unavailable)</span>
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Content Area */}
      <div className="flex flex-col flex-1 overflow-hidden">
        {activeTab === 'chat' ? (
          <>
            {/* Messages Area */}
            <div className="flex-1 overflow-y-auto p-4">
              {messages.length === 0 && !isLoading ? (
                <div className="text-center py-8">
                  <div className="text-gray-500 mb-4">
                    <svg
                      className="w-16 h-16 mx-auto mb-4 text-gray-300"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
                      />
                    </svg>
                    <h2 className="text-lg font-medium text-gray-900 mb-2">
                      Welcome to Azure DevOps AI Agent
                    </h2>
                    <p className="text-gray-800 max-w-md mx-auto">
                      Start a conversation by asking about your projects, work
                      items, repositories, or pipelines.
                    </p>
                  </div>

                  {/* Example prompts */}
                  <div className="max-w-2xl mx-auto">
                    <h3 className="text-sm font-medium text-gray-800 mb-3">
                      Try asking:
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-2 text-sm">
                      {[
                        'Show me my active work items',
                        'List all repositories in my project',
                        'What are the recent pipeline runs?',
                        'Create a new user story',
                      ].map((example, index) => (
                        <button
                          key={index}
                          className="text-left p-3 bg-gray-50 hover:bg-gray-100 rounded-md border border-gray-200 transition-colors text-gray-800"
                          onClick={() => sendMessage(example)}
                        >
                          &quot;{example}&quot;
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              ) : (
                <div>
                  {messages.map((message) => (
                    <div key={message.id} className="relative">
                      <ChatMessageComponent
                        message={message}
                        onSuggestionClick={sendMessage}
                      />
                      {/* Thought Process Button for Assistant Messages */}
                      {message.role === 'assistant' &&
                        message.thoughtProcessId &&
                        message.conversationId && (
                          <div className="mt-2 flex justify-end">
                            <button
                              onClick={() =>
                                handleMessageThoughtProcessClick(
                                  message.thoughtProcessId!,
                                  message.conversationId!
                                )
                              }
                              className="text-xs text-blue-600 hover:text-blue-800 bg-blue-50 hover:bg-blue-100 px-2 py-1 rounded border border-blue-200 transition-colors"
                            >
                              🧠 View thought process
                            </button>
                          </div>
                        )}
                    </div>
                  ))}

                  {isLoading && (
                    <div className="flex justify-start mb-4">
                      <div className="bg-gray-100 rounded-lg px-4 py-2">
                        <Loading size="small" message="Thinking..." />
                      </div>
                    </div>
                  )}

                  <div ref={messagesEndRef} />
                </div>
              )}
            </div>

            {/* Error Display */}
            {error && (
              <div className="mx-4 mb-4">
                <div className="bg-red-50 border border-red-200 rounded-md p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center">
                      <svg
                        className="w-5 h-5 text-red-400 mr-2"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                        />
                      </svg>
                      <p className="text-red-700 text-sm">{error}</p>
                    </div>
                    <div className="flex space-x-2">
                      <Button
                        variant="outline"
                        size="small"
                        onClick={retryLastMessage}
                        className="text-red-600 border-red-300 hover:bg-red-50"
                      >
                        Retry
                      </Button>
                      <Button
                        variant="ghost"
                        size="small"
                        onClick={clearError}
                        className="text-red-600 hover:bg-red-50"
                      >
                        Dismiss
                      </Button>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Input Area */}
            <ChatInput
              onSendMessage={sendMessage}
              disabled={isLoading}
              loading={isLoading}
            />
          </>
        ) : (
          /* Thought Process Tab */
          <div className="flex-1 overflow-y-auto">
            {selectedThoughtProcess ? (
              <ThoughtProcess
                thoughtProcessId={selectedThoughtProcess.thoughtProcessId}
                conversationId={selectedThoughtProcess.conversationId}
              />
            ) : (
              <div className="p-8 text-center text-gray-500">
                <div className="mb-4">🧠</div>
                <h3 className="text-lg font-medium text-gray-900 mb-2">
                  No Thought Process Selected
                </h3>
                <p className="text-sm">
                  Send a message to the AI agent first, then return to this tab
                  to see the thought process.
                </p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default ChatInterface;
