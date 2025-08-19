"""Minimal FastAPI backend stub for CI compatibility."""

from fastapi import FastAPI

app = FastAPI(
    title="Azure DevOps AI Agent Backend",
    description="Backend API for Azure DevOps AI Agent",
    version="1.0.0"
)

@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy", "message": "Backend is running"}

@app.get("/")
async def root():
    """Root endpoint."""
    return {"message": "Azure DevOps AI Agent Backend API"}