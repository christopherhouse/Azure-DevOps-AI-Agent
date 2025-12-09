/**
 * Demo page for showcasing the chat interface with thought process feature
 */

'use client';

import React, { useState } from 'react';
import { ThoughtProcess } from '@/components/ThoughtProcess';
import { ChatMessageComponent } from '@/components/ChatMessage';
import type {
  ChatMessage,
  ThoughtProcess as ThoughtProcessType,
} from '@/types';

// Mock data for demonstration
const mockMessages: ChatMessage[] = [
  {
    id: '1',
    content: 'Show me my active work items',
    role: 'user',
    timestamp: new Date(), // Current time - shows time only
    conversationId: 'demo-conversation',
  },
  {
    id: '2',
    content:
      'I found 5 active work items in your current sprint. Here they are:\n\n1. **User Story #1234**: Implement user authentication\n2. **Bug #1235**: Fix login redirect issue\n3. **Task #1236**: Update API documentation\n4. **Feature #1237**: Add search functionality\n5. **Epic #1238**: Mobile app development\n\nWould you like me to show details for any specific work item?',
    role: 'assistant',
    timestamp: new Date(), // Current time - shows time only
    conversationId: 'demo-conversation',
    thoughtProcessId: 'demo-thought-process-1',
    suggestions: [
      'Show details for User Story #1234',
      'What are the blockers for these items?',
      'Show work items assigned to me',
    ],
  },
  {
    id: '3',
    content: 'Show me the users in the organization',
    role: 'user',
    timestamp: new Date(Date.now() - 1 * 60 * 1000), // 1 minute ago
    conversationId: 'demo-conversation',
  },
  {
    id: '4',
    content:
      'Here is the list of users for the Azure DevOps organization **chris0477**:\n\n| Display Name | Email | License | Status | Last Accessed Date | Date Created |\n|--------------|-------|---------|--------|-------------------|-------------|\n| Christopher House (low-priv) | chris@MngEnvMCAP064264.onmicrosoft.com | Basic | Active | 2025-12-09 12:51:36 | 2024-11-01 |\n| Chris House | chhouse@microsoft.com | Visual Studio Enterprise subscription | Active | 2025-12-02 14:55:50 | 2024-11-01 |\n\nFeel free to ask if you need more information or assistance with Azure DevOps!',
    role: 'assistant',
    format: 'markdown',
    timestamp: new Date(Date.now() - 30 * 1000), // 30 seconds ago
    conversationId: 'demo-conversation',
  },
  {
    id: '5',
    content: 'What was discussed last week?',
    role: 'user',
    timestamp: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // 7 days ago - shows date and time
    conversationId: 'demo-conversation',
  },
];

const mockThoughtProcess: ThoughtProcessType = {
  id: 'demo-thought-process-1',
  conversationId: 'demo-conversation',
  messageId: '2',
  startTime: '2024-12-19T10:30:00Z',
  endTime: '2024-12-19T10:30:15Z',
  durationMs: 15000,
  steps: [
    {
      id: 'step-1',
      description: 'Analyzing user request for active work items',
      type: 'analysis',
      timestamp: '2024-12-19T10:30:00Z',
      details: {
        message_length: 25,
        conversation_id: 'demo-conversation',
        intent: 'query_work_items',
        confidence: 0.95,
      },
    },
    {
      id: 'step-2',
      description: 'Planning Azure DevOps API query approach',
      type: 'planning',
      timestamp: '2024-12-19T10:30:02Z',
      details: {
        api_endpoint: '/api/workitems',
        query_parameters: {
          state: 'active',
          assignedTo: '@me',
          sprint: 'current',
        },
        estimated_complexity: 'medium',
      },
    },
    {
      id: 'step-3',
      description: 'Executing Azure DevOps API call to fetch work items',
      type: 'tool_invocation',
      timestamp: '2024-12-19T10:30:05Z',
      details: {
        tool_name: 'azure_devops_client',
        operation: 'query_work_items',
      },
    },
    {
      id: 'step-4',
      description: 'Processing and formatting work items data',
      type: 'reasoning',
      timestamp: '2024-12-19T10:30:10Z',
      details: {
        items_found: 5,
        formatting_style: 'numbered_list',
        include_suggestions: true,
      },
    },
    {
      id: 'step-5',
      description: 'Response generated successfully with suggestions',
      type: 'completion',
      timestamp: '2024-12-19T10:30:15Z',
      details: {
        response_length: 280,
        suggestions_count: 3,
        confidence_score: 0.92,
      },
    },
  ],
  toolInvocations: [
    {
      toolName: 'azure_devops_query',
      status: 'success',
      timestamp: '2024-12-19T10:30:05Z',
      parameters: {
        query:
          "SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType] FROM WorkItems WHERE [System.State] = 'Active' AND [System.AssignedTo] = @me",
        organization: 'contoso',
        project: 'MyProject',
      },
      result: {
        workItems: [
          {
            id: 1234,
            title: 'Implement user authentication',
            type: 'User Story',
            state: 'Active',
          },
          {
            id: 1235,
            title: 'Fix login redirect issue',
            type: 'Bug',
            state: 'Active',
          },
          {
            id: 1236,
            title: 'Update API documentation',
            type: 'Task',
            state: 'Active',
          },
          {
            id: 1237,
            title: 'Add search functionality',
            type: 'Feature',
            state: 'Active',
          },
          {
            id: 1238,
            title: 'Mobile app development',
            type: 'Epic',
            state: 'Active',
          },
        ],
      },
    },
  ],
};

export default function DemoPage() {
  const [selectedTab, setSelectedTab] = useState<'chat' | 'thought'>('chat');

  // Override the ThoughtProcess component's API client for demo
  React.useEffect(() => {
    const originalFetch = window.fetch;
    window.fetch = async (url: RequestInfo | URL, options?: RequestInit) => {
      if (
        typeof url === 'string' &&
        url.includes('thought-process/demo-thought-process-1')
      ) {
        return new Response(JSON.stringify(mockThoughtProcess), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        });
      }
      return originalFetch(url, options);
    };

    return () => {
      window.fetch = originalFetch;
    };
  }, []);

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Demo Header */}
      <div className="bg-yellow-50 border-b border-yellow-200 p-4">
        <div className="max-w-4xl mx-auto">
          <div className="flex items-center space-x-2">
            <div className="text-yellow-600">⚠️</div>
            <div className="text-sm text-yellow-800">
              <strong>Demo Mode:</strong> This is a demonstration of the thought
              process feature. Real Azure DevOps integration requires
              authentication.
            </div>
          </div>
        </div>
      </div>

      {/* Main Interface */}
      <div className="h-screen flex flex-col">
        <div className="flex-1">
          {selectedTab === 'chat' ? (
            <DemoChatInterface />
          ) : (
            <div className="h-full overflow-y-auto">
              <DemoThoughtProcess />
            </div>
          )}
        </div>

        {/* Tab Navigation */}
        <div className="bg-white border-t border-gray-200 p-4">
          <div className="flex space-x-4 justify-center">
            <button
              onClick={() => setSelectedTab('chat')}
              className={`px-6 py-2 rounded-lg font-medium transition-colors ${
                selectedTab === 'chat'
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
              }`}
            >
              💬 Chat Interface
            </button>
            <button
              onClick={() => setSelectedTab('thought')}
              className={`px-6 py-2 rounded-lg font-medium transition-colors ${
                selectedTab === 'thought'
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
              }`}
            >
              🧠 Thought Process
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function DemoChatInterface() {
  return (
    <div className="h-full bg-white">
      <div className="bg-gradient-to-r from-blue-600 to-blue-700 text-white p-4">
        <h1 className="text-xl font-semibold">
          🤖 Azure DevOps AI Agent - Demo
        </h1>
        <p className="text-blue-100 text-sm">
          Demonstrating the timestamp display feature (no more &quot;Invalid
          date&quot;!)
        </p>
      </div>

      <div className="flex-1 overflow-y-auto p-4">
        <div className="space-y-4">
          {mockMessages.map((message) => (
            <div key={message.id} className="space-y-2">
              <ChatMessageComponent message={message} />

              {message.role === 'assistant' && message.thoughtProcessId && (
                <div className="flex justify-start">
                  <div className="text-xs text-blue-600 bg-blue-50 px-3 py-1 rounded border border-blue-200">
                    🧠 Thought process available - switch to &quot;Thought
                    Process&quot; tab to view
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function DemoThoughtProcess() {
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});

  const toggleExpanded = (id: string) => {
    setExpanded((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const getStepIcon = (type: string) => {
    switch (type) {
      case 'analysis':
        return '🔍';
      case 'planning':
        return '📋';
      case 'reasoning':
        return '🤔';
      case 'tool_invocation':
        return '🔧';
      case 'completion':
        return '✅';
      default:
        return '💭';
    }
  };

  const getStepColor = (type: string) => {
    switch (type) {
      case 'analysis':
        return 'bg-blue-50 border-blue-200';
      case 'planning':
        return 'bg-purple-50 border-purple-200';
      case 'reasoning':
        return 'bg-yellow-50 border-yellow-200';
      case 'tool_invocation':
        return 'bg-green-50 border-green-200';
      case 'completion':
        return 'bg-emerald-50 border-emerald-200';
      default:
        return 'bg-gray-50 border-gray-200';
    }
  };

  return (
    <div className="p-4 space-y-4">
      {/* Header */}
      <div className="border-b border-gray-200 pb-4">
        <h3 className="text-lg font-semibold text-gray-900 mb-2">
          🧠 Agent Thought Process
        </h3>
        <div className="text-sm text-gray-600 space-y-1">
          <div>Duration: {mockThoughtProcess.durationMs}ms</div>
          <div>Steps: {mockThoughtProcess.steps.length}</div>
          <div>Tools used: {mockThoughtProcess.toolInvocations.length}</div>
        </div>
      </div>

      {/* Thought Steps */}
      <div className="space-y-3">
        <h4 className="font-medium text-gray-800">Reasoning Steps</h4>
        {mockThoughtProcess.steps.map((step, index) => (
          <div
            key={step.id}
            className={`border rounded-lg p-3 ${getStepColor(step.type)}`}
          >
            <div className="flex items-start space-x-3">
              <div className="flex-shrink-0 w-6 h-6 bg-white rounded-full border border-gray-300 flex items-center justify-center text-sm font-medium">
                {index + 1}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center space-x-2">
                  <span className="text-lg">{getStepIcon(step.type)}</span>
                  <span className="text-sm font-medium text-gray-900 capitalize">
                    {step.type.replace('_', ' ')}
                  </span>
                  <span className="text-xs text-gray-500">
                    {new Date(step.timestamp).toLocaleTimeString()}
                  </span>
                </div>
                <p className="text-sm text-gray-700 mt-1">{step.description}</p>

                {step.details && Object.keys(step.details).length > 0 && (
                  <div className="mt-2">
                    <button
                      onClick={() => toggleExpanded(step.id)}
                      className="text-xs text-blue-600 hover:text-blue-800 font-medium"
                    >
                      {expanded[step.id] ? 'Hide details' : 'Show details'}
                    </button>
                    {expanded[step.id] && (
                      <div className="mt-2 p-2 bg-white rounded border text-xs font-mono">
                        <pre className="whitespace-pre-wrap">
                          {JSON.stringify(step.details, null, 2)}
                        </pre>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Tool Invocations */}
      <div className="space-y-3">
        <h4 className="font-medium text-gray-800">Tool Invocations</h4>
        {mockThoughtProcess.toolInvocations.map((tool, index) => (
          <div
            key={index}
            className="border border-gray-200 rounded-lg p-3 bg-white"
          >
            <div className="flex items-start justify-between">
              <div className="flex items-center space-x-3">
                <div className="flex-shrink-0 w-6 h-6 bg-blue-100 rounded-full border border-blue-300 flex items-center justify-center text-sm font-medium text-blue-700">
                  {index + 1}
                </div>
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="text-sm font-medium text-gray-900">
                      🔧 {tool.toolName}
                    </span>
                    <span className="px-2 py-1 text-xs rounded-full text-green-600 bg-green-50">
                      {tool.status}
                    </span>
                  </div>
                  <div className="text-xs text-gray-500 mt-1">
                    {new Date(tool.timestamp).toLocaleTimeString()}
                  </div>
                </div>
              </div>

              <button
                onClick={() => toggleExpanded(`tool-${index}`)}
                className="text-xs text-blue-600 hover:text-blue-800 font-medium"
              >
                {expanded[`tool-${index}`] ? 'Hide' : 'Show'}
              </button>
            </div>

            {expanded[`tool-${index}`] && (
              <div className="mt-3 space-y-2">
                {tool.parameters && (
                  <div>
                    <div className="text-xs font-medium text-gray-600 mb-1">
                      Parameters:
                    </div>
                    <div className="p-2 bg-gray-50 rounded border text-xs font-mono">
                      <pre className="whitespace-pre-wrap">
                        {JSON.stringify(tool.parameters, null, 2)}
                      </pre>
                    </div>
                  </div>
                )}

                {tool.result && (
                  <div>
                    <div className="text-xs font-medium text-gray-600 mb-1">
                      Result:
                    </div>
                    <div className="p-2 bg-gray-50 rounded border text-xs font-mono">
                      <pre className="whitespace-pre-wrap">
                        {JSON.stringify(tool.result, null, 2)}
                      </pre>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
