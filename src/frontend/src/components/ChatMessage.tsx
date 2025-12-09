/**
 * Individual chat message component.
 */

import React from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { ExternalLink, BookOpen, FileText, Info } from 'lucide-react';
import type { ChatMessage, Citation } from '@/types';

interface ChatMessageProps {
  message: ChatMessage;
  onSuggestionClick?: (suggestion: string) => void;
}

export function ChatMessageComponent({
  message,
  onSuggestionClick,
}: ChatMessageProps) {
  const isUser = message.role === 'user';

  // Format timestamp based on user's locale
  // Show date and time for older messages, just time for today's messages
  const formatTimestamp = (date: Date): string => {
    const now = new Date();
    const isToday = date.toDateString() === now.toDateString();

    if (isToday) {
      return date.toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit',
      });
    } else {
      return date.toLocaleString([], {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    }
  };

  const timestamp = formatTimestamp(message.timestamp);

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'} mb-4`}>
      <div
        className={`${isUser ? 'max-w-xs lg:max-w-md items-end' : 'max-w-full lg:max-w-4xl items-start'} flex flex-col`}
      >
        <div
          className={`px-4 py-2 rounded-lg ${
            isUser
              ? 'bg-blue-600 text-white rounded-br-none'
              : 'bg-gray-100 text-gray-900 rounded-bl-none'
          }`}
        >
          {/* Render content based on format */}
          {message.format === 'markdown' && !isUser ? (
            <div className="text-sm leading-relaxed prose prose-sm max-w-none prose-slate prose-p:my-2 prose-p:text-gray-900 prose-headings:my-2 prose-headings:text-gray-900 prose-headings:font-semibold prose-ul:my-1 prose-ol:my-1 prose-li:my-0.5 prose-li:text-gray-900 prose-table:my-2 prose-thead:bg-gray-200 prose-th:px-3 prose-th:py-2 prose-th:font-semibold prose-th:text-gray-900 prose-td:px-3 prose-td:py-2 prose-td:text-gray-900 prose-tr:border-gray-300 prose-strong:text-gray-900 prose-strong:font-semibold prose-code:text-gray-900 prose-pre:bg-gray-800 prose-pre:text-gray-100">
              <div className="overflow-x-auto">
                <ReactMarkdown remarkPlugins={[remarkGfm]}>
                  {message.content}
                </ReactMarkdown>
              </div>
            </div>
          ) : (
            <div className="text-sm leading-relaxed whitespace-pre-wrap">
              {message.content}
            </div>
          )}
          <div
            className={`text-xs mt-1 ${
              isUser ? 'text-blue-100' : 'text-gray-500'
            }`}
          >
            {timestamp}
          </div>
        </div>

        {/* Citations */}
        {!isUser && message.citations && message.citations.length > 0 && (
          <div className="mt-2 max-w-full">
            <div className="text-xs text-gray-600 mb-1 font-medium">
              Sources:
            </div>
            <div className="space-y-1">
              {message.citations.map((citation, index) => (
                <CitationComponent key={index} citation={citation} />
              ))}
            </div>
          </div>
        )}

        {/* Suggestions */}
        {!isUser && message.suggestions && message.suggestions.length > 0 && (
          <div className="mt-2 max-w-full">
            <div className="text-xs text-gray-600 mb-1 font-medium">
              You might also ask:
            </div>
            <div className="space-y-1">
              {message.suggestions.map((suggestion, index) => (
                <button
                  key={index}
                  className="block text-left text-xs bg-gray-50 hover:bg-gray-100 border border-gray-200 rounded px-2 py-1 text-gray-700 transition-colors"
                  onClick={() => onSuggestionClick?.(suggestion)}
                >
                  {suggestion}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function CitationComponent({ citation }: { citation: Citation }) {
  const icon = getCitationIcon(citation.type);

  if (citation.url) {
    return (
      <a
        href={citation.url}
        target="_blank"
        rel="noopener noreferrer"
        className="flex items-center gap-1 text-xs bg-blue-50 hover:bg-blue-100 border border-blue-200 rounded px-2 py-1 text-blue-700 transition-colors"
      >
        {icon}
        <span className="truncate">{citation.title}</span>
        <ExternalLink className="w-2 h-2 flex-shrink-0" />
      </a>
    );
  }

  return (
    <div className="flex items-center gap-1 text-xs bg-gray-50 border border-gray-200 rounded px-2 py-1 text-gray-700">
      {icon}
      <span className="truncate">{citation.title}</span>
    </div>
  );
}

function getCitationIcon(type?: string) {
  switch (type) {
    case 'documentation':
      return <BookOpen className="w-3 h-3" />;
    case 'reference':
      return <FileText className="w-3 h-3" />;
    case 'guide':
      return <Info className="w-3 h-3" />;
    default:
      return <ExternalLink className="w-3 h-3" />;
  }
}

export default ChatMessageComponent;
