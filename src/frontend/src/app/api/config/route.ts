/**
 * API route to provide runtime configuration to client
 * This enables reading environment variables at runtime for containerized deployments
 */

import { NextResponse } from 'next/server'
import { loadRuntimeConfig, getClientConfig } from '@/lib/runtime-config'

export async function GET() {
  try {
    const runtimeConfig = await loadRuntimeConfig()
    const clientConfig = getClientConfig(runtimeConfig)
    
    return NextResponse.json(clientConfig)
  } catch (error) {
    console.error('Failed to load runtime configuration:', error)
    
    // Return error with helpful information
    return NextResponse.json(
      {
        error: 'Configuration Error',
        message: error instanceof Error ? error.message : 'Unknown configuration error',
        details: 'Please check your environment variables are properly set',
      },
      { status: 500 }
    )
  }
}