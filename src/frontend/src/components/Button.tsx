/**
 * Reusable button component with various styles and states.
 */

import React from 'react';
import { Loading } from './Loading';
import type { ButtonProps } from '@/types';

const variantClasses = {
  primary: 'bg-blue-600 hover:bg-blue-700 text-white border-transparent',
  secondary: 'bg-gray-100 hover:bg-gray-200 text-gray-900 border-gray-300',
  outline: 'bg-transparent hover:bg-blue-50 text-blue-600 border-blue-600',
  ghost: 'bg-transparent hover:bg-gray-100 text-gray-700 border-transparent',
};

const sizeClasses = {
  small: 'px-3 py-1.5 text-sm',
  medium: 'px-4 py-2 text-base',
  large: 'px-6 py-3 text-lg',
};

export function Button({
  variant = 'primary',
  size = 'medium',
  loading = false,
  disabled = false,
  onClick,
  children,
  className = '',
}: ButtonProps) {
  const isDisabled = disabled || loading;

  const baseClasses =
    'inline-flex items-center justify-center font-medium border rounded-md transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2';
  const disabledClasses = 'opacity-50 cursor-not-allowed';

  const buttonClasses = [
    baseClasses,
    variantClasses[variant],
    sizeClasses[size],
    isDisabled ? disabledClasses : '',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button
      type="button"
      className={buttonClasses}
      onClick={onClick}
      disabled={isDisabled}
      aria-label={typeof children === 'string' ? children : undefined}
    >
      {loading && <Loading size="small" />}
      {loading && <span className="ml-2" />}
      {children}
    </button>
  );
}

export default Button;
