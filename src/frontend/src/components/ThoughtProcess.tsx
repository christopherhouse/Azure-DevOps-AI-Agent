/**
 * ThoughtProcess component for displaying agent reasoning steps.
 */

import React, { useState, useEffect } from 'react';
import { ThoughtProcess as ThoughtProcessType, ThoughtStep, ToolInvocation } from '@/types';
import { apiClient } from '@/services/api-client';
import { Loading } from './Loading';

interface ThoughtProcessProps {
  thoughtProcessId: string;
}

export function ThoughtProcess({ thoughtProcessId }: ThoughtProcessProps) {
  const [thoughtProcess, setThoughtProcess] = useState<ThoughtProcessType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchThoughtProcess = async () => {
      if (!thoughtProcessId) {
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError(null);
        const response = await apiClient.get(`/api/chat/thought-process/${thoughtProcessId}`);
        setThoughtProcess(response.data as ThoughtProcessType);
      } catch (err: any) {
        console.error('Error fetching thought process:', err);
        setError(err.response?.data?.error?.message || 'Failed to load thought process');
      } finally {
        setLoading(false);
      }
    };

    fetchThoughtProcess();
  }, [thoughtProcessId]);

  if (loading) {
    return (
      <div className="p-4 text-center">
        <Loading size="medium" message="Loading thought process..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 text-center text-red-600">
        <div className="mb-2">⚠️ Error loading thought process</div>
        <div className="text-sm">{error}</div>
      </div>
    );
  }

  if (!thoughtProcess) {
    return (
      <div className="p-4 text-center text-gray-500">
        <div className="mb-2">🤔 No thought process available</div>
        <div className="text-sm">This message doesn&apos;t have associated reasoning data.</div>
      </div>
    );
  }

  return (
    <div className="p-4 space-y-4">
      {/* Header */}
      <div className="border-b border-gray-200 pb-4">
        <h3 className="text-lg font-semibold text-gray-900 mb-2">🧠 Agent Thought Process</h3>
        <div className="text-sm text-gray-600 space-y-1">
          <div>Duration: {thoughtProcess.durationMs ? `${thoughtProcess.durationMs}ms` : 'Unknown'}</div>
          <div>Steps: {thoughtProcess.steps.length}</div>
          {thoughtProcess.toolInvocations.length > 0 && (
            <div>Tools used: {thoughtProcess.toolInvocations.length}</div>
          )}
        </div>
      </div>

      {/* Thought Steps */}
      <div className="space-y-3">
        <h4 className="font-medium text-gray-800">Reasoning Steps</h4>
        {thoughtProcess.steps.map((step, index) => (
          <ThoughtStepComponent key={step.id} step={step} index={index + 1} />
        ))}
      </div>

      {/* Tool Invocations */}
      {thoughtProcess.toolInvocations.length > 0 && (
        <div className="space-y-3">
          <h4 className="font-medium text-gray-800">Tool Invocations</h4>
          {thoughtProcess.toolInvocations.map((tool, index) => (
            <ToolInvocationComponent key={index} tool={tool} index={index + 1} />
          ))}
        </div>
      )}
    </div>
  );
}

interface ThoughtStepComponentProps {
  step: ThoughtStep;
  index: number;
}

function ThoughtStepComponent({ step, index }: ThoughtStepComponentProps) {
  const [expanded, setExpanded] = useState(false);
  
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
      case 'post_processing':
        return '⚙️';
      case 'error':
        return '❌';
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
      case 'post_processing':
        return 'bg-gray-50 border-gray-200';
      case 'error':
        return 'bg-red-50 border-red-200';
      default:
        return 'bg-gray-50 border-gray-200';
    }
  };

  return (
    <div className={`border rounded-lg p-3 ${getStepColor(step.type)}`}>
      <div className="flex items-start space-x-3">
        <div className="flex-shrink-0 w-6 h-6 bg-white rounded-full border border-gray-300 flex items-center justify-center text-sm font-medium">
          {index}
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
                onClick={() => setExpanded(!expanded)}
                className="text-xs text-blue-600 hover:text-blue-800 font-medium"
              >
                {expanded ? 'Hide details' : 'Show details'}
              </button>
              {expanded && (
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
  );
}

interface ToolInvocationComponentProps {
  tool: ToolInvocation;
  index: number;
}

function ToolInvocationComponent({ tool, index }: ToolInvocationComponentProps) {
  const [expanded, setExpanded] = useState(false);
  
  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'success':
      case 'completed':
        return 'text-green-600 bg-green-50';
      case 'failed':
      case 'error':
        return 'text-red-600 bg-red-50';
      case 'pending':
      case 'running':
        return 'text-yellow-600 bg-yellow-50';
      default:
        return 'text-gray-600 bg-gray-50';
    }
  };

  return (
    <div className="border border-gray-200 rounded-lg p-3 bg-white">
      <div className="flex items-start justify-between">
        <div className="flex items-center space-x-3">
          <div className="flex-shrink-0 w-6 h-6 bg-blue-100 rounded-full border border-blue-300 flex items-center justify-center text-sm font-medium text-blue-700">
            {index}
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <span className="text-sm font-medium text-gray-900">🔧 {tool.toolName}</span>
              <span className={`px-2 py-1 text-xs rounded-full ${getStatusColor(tool.status)}`}>
                {tool.status}
              </span>
            </div>
            <div className="text-xs text-gray-500 mt-1">
              {new Date(tool.timestamp).toLocaleTimeString()}
            </div>
          </div>
        </div>
        
        <button
          onClick={() => setExpanded(!expanded)}
          className="text-xs text-blue-600 hover:text-blue-800 font-medium"
        >
          {expanded ? 'Hide' : 'Show'}
        </button>
      </div>

      {tool.errorMessage && (
        <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded text-sm text-red-700">
          <strong>Error:</strong> {tool.errorMessage}
        </div>
      )}

      {expanded && (
        <div className="mt-3 space-y-2">
          {tool.parameters && Object.keys(tool.parameters).length > 0 && (
            <div>
              <div className="text-xs font-medium text-gray-600 mb-1">Parameters:</div>
              <div className="p-2 bg-gray-50 rounded border text-xs font-mono">
                <pre className="whitespace-pre-wrap">
                  {JSON.stringify(tool.parameters, null, 2)}
                </pre>
              </div>
            </div>
          )}
          
          {tool.result && (
            <div>
              <div className="text-xs font-medium text-gray-600 mb-1">Result:</div>
              <div className="p-2 bg-gray-50 rounded border text-xs font-mono">
                <pre className="whitespace-pre-wrap">
                  {typeof tool.result === 'string' ? tool.result : JSON.stringify(tool.result, null, 2)}
                </pre>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default ThoughtProcess;