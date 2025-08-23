/**
 * Loading component with spinner and optional message.
 */

import React from 'react';
import type { LoadingProps } from '@/types';

const sizeClasses = {
  small: 'w-4 h-4',
  medium: 'w-8 h-8',
  large: 'w-12 h-12',
};

export function Loading({ size = 'medium', message }: LoadingProps) {
  return (
    <div className="flex flex-col items-center justify-center p-4">
      <div
        className={`animate-spin rounded-full border-b-2 border-blue-600 ${sizeClasses[size]}`}
        role="status"
        aria-label="Loading"
      >
        <span className="sr-only">Loading...</span>
      </div>
      {message && (
        <p className="mt-2 text-sm text-gray-600 text-center">{message}</p>
      )}
    </div>
  );
}

export default Loading;