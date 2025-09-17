/**
 * Individual chat message component.
 */

import React from 'react';
import { ExternalLink, BookOpen, FileText, Info } from 'lucide-react';
import type { ChatMessage, Citation } from '@/types';

interface ChatMessageProps {
  message: ChatMessage;
  onSuggestionClick?: (suggestion: string) => void;
}

export function ChatMessageComponent({ message, onSuggestionClick }: ChatMessageProps) {
  const isUser = message.role === 'user';
  const timestamp = message.timestamp.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  });

  const getCitationIcon = (type?: string) => {
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
  };

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'} mb-4`}>
      <div className={`max-w-xs lg:max-w-md ${isUser ? 'items-end' : 'items-start'} flex flex-col`}>
        <div
          className={`px-4 py-2 rounded-lg ${
            isUser
              ? 'bg-blue-600 text-white rounded-br-none'
              : 'bg-gray-100 text-gray-900 rounded-bl-none'
          }`}
        >
          <div className="text-sm leading-relaxed whitespace-pre-wrap">
            {message.content}
          </div>
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
            <div className="text-xs text-gray-600 mb-1 font-medium">Sources:</div>
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
            <div className="text-xs text-gray-600 mb-1 font-medium">You might also ask:</div>
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
