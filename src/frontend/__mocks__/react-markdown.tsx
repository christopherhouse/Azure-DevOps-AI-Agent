/**
 * Mock for react-markdown used in tests
 */
import React from 'react';

interface ReactMarkdownProps {
  children: string;
  remarkPlugins?: any[];
}

const ReactMarkdown: React.FC<ReactMarkdownProps> = ({ children }) => {
  return <div data-testid="markdown-content">{children}</div>;
};

export default ReactMarkdown;
