import '@testing-library/jest-dom'

// Add custom Jest matchers
declare global {
  namespace jest {
    interface Matchers<R> {
      toBeInTheDocument(): R;
      toHaveClass(className: string): R;
      toHaveTextContent(text: string): R;
      toBeDisabled(): R;
    }
  }
}