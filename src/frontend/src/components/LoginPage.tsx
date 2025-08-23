/**
 * Login component for Microsoft Entra ID authentication.
 */

import React from 'react';
import { Button } from './Button';
import { useAuth } from '@/hooks/use-auth';

export function LoginPage() {
  const { login, isLoading, error } = useAuth();

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div className="text-center">
          <div className="mx-auto h-12 w-12 text-blue-600">
            <svg
              className="w-full h-full"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
              />
            </svg>
          </div>
          <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
            Azure DevOps AI Agent
          </h2>
          <p className="mt-2 text-sm text-gray-600">
            Intelligent assistant for Azure DevOps project management
          </p>
        </div>

        <div className="bg-white rounded-lg shadow-md p-8">
          <div className="space-y-6">
            <div>
              <h3 className="text-lg font-medium text-gray-900 mb-3">
                Get Started
              </h3>
              <p className="text-sm text-gray-600 mb-6">
                Sign in with your Microsoft account to access your Azure DevOps projects, 
                work items, repositories, and pipelines through natural language conversations.
              </p>
            </div>

            {error && (
              <div className="bg-red-50 border border-red-200 rounded-md p-4">
                <div className="flex">
                  <svg
                    className="w-5 h-5 text-red-400 mr-2 mt-0.5"
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
                  <div>
                    <h4 className="text-sm font-medium text-red-800">
                      Authentication Error
                    </h4>
                    <p className="text-sm text-red-700 mt-1">{error}</p>
                  </div>
                </div>
              </div>
            )}

            <div>
              <Button
                onClick={login}
                loading={isLoading}
                disabled={isLoading}
                variant="primary"
                size="large"
                className="w-full flex items-center justify-center"
              >
                <svg
                  className="w-5 h-5 mr-2"
                  viewBox="0 0 24 24"
                  fill="currentColor"
                >
                  <path d="M23.5 12c0-6.35-5.15-11.5-11.5-11.5S.5 5.65.5 12 5.65 23.5 12 23.5 23.5 18.35 23.5 12zM12 21.75c-5.38 0-9.75-4.37-9.75-9.75S6.62 2.25 12 2.25 21.75 6.62 21.75 12 17.38 21.75 12 21.75z"/>
                  <path d="M10.5 8.5h3v7h-3v-7zm0-2h3v1.5h-3V6.5z"/>
                </svg>
                Sign in with Microsoft
              </Button>
            </div>

            <div className="text-xs text-gray-500 space-y-2">
              <div className="flex items-center">
                <svg
                  className="w-4 h-4 text-green-500 mr-2"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                Secured with Microsoft Entra ID
              </div>
              <div className="flex items-center">
                <svg
                  className="w-4 h-4 text-green-500 mr-2"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                Powered by Azure OpenAI
              </div>
              <div className="flex items-center">
                <svg
                  className="w-4 h-4 text-green-500 mr-2"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                Built with Next.js & TypeScript
              </div>
            </div>
          </div>
        </div>

        <div className="text-center">
          <p className="text-xs text-gray-500">
            For support, please contact your system administrator or visit{' '}
            <a 
              href="https://github.com/christopherhouse/Azure-DevOps-AI-Agent"
              className="text-blue-600 hover:text-blue-500"
              target="_blank"
              rel="noopener noreferrer"
            >
              our documentation
            </a>
          </p>
        </div>
      </div>
    </div>
  );
}

export default LoginPage;