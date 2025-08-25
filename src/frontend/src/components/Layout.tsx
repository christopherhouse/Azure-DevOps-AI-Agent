/**
 * Main layout component with authentication state management.
 */

import React from 'react';
import { useAuth } from '@/hooks/use-auth';
import { LoginPage } from './LoginPage';
import { ChatInterface } from './ChatInterface';
import { Loading } from './Loading';
import { Button } from './Button';

export function Layout() {
  const { isAuthenticated, isLoading, user, logout } = useAuth();

  // Show loading while initializing authentication
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <Loading size="large" message="Initializing application..." />
      </div>
    );
  }

  // Show login page if not authenticated
  if (!isAuthenticated) {
    return <LoginPage />;
  }

  // Show main interface if authenticated
  return (
    <div className="h-screen flex flex-col bg-gray-50">
      {/* Top Navigation */}
      <nav className="bg-white border-b border-gray-200 px-4 py-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <h1 className="text-lg font-semibold text-gray-900">
              Azure DevOps AI Agent
            </h1>
          </div>

          <div className="flex items-center space-x-4">
            {user && (
              <div className="flex items-center space-x-3">
                <div className="text-sm">
                  <div className="font-medium text-gray-900">{user.name}</div>
                  <div className="text-gray-500">{user.email}</div>
                </div>
                <div className="h-8 w-8 bg-blue-100 rounded-full flex items-center justify-center">
                  <span className="text-sm font-medium text-blue-600">
                    {user.name?.charAt(0).toUpperCase() || 'U'}
                  </span>
                </div>
                <Button
                  variant="outline"
                  size="small"
                  onClick={logout}
                  className="ml-3"
                >
                  Sign Out
                </Button>
              </div>
            )}
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="flex-1 overflow-hidden">
        <ChatInterface />
      </main>
    </div>
  );
}

export default Layout;
