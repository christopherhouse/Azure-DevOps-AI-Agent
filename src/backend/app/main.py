"""Azure DevOps AI Agent Backend API."""

import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware
from starlette.exceptions import HTTPException as StarletteHTTPException

# Import API routes
from app.api import auth, chat, projects, workitems

# Import configuration and telemetry
from app.core.config import settings
from app.core.telemetry import setup_telemetry

# Import middleware
from app.middleware.error_handlers import (
    general_exception_handler,
    http_exception_handler,
    validation_exception_handler,
)
from app.middleware.security import RequestLoggingMiddleware, SecurityHeadersMiddleware

# Configure logging
logging.basicConfig(
    level=logging.INFO if not settings.debug else logging.DEBUG,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan manager."""
    # Startup
    logger.info("Starting Azure DevOps AI Agent Backend")

    # Initialize telemetry
    setup_telemetry()

    yield

    # Shutdown
    logger.info("Shutting down Azure DevOps AI Agent Backend")


# Create FastAPI application
app = FastAPI(
    title=settings.app_name,
    description="Backend API for Azure DevOps AI Agent with Entra ID authentication",
    version=settings.app_version,
    debug=settings.debug,
    lifespan=lifespan,
    docs_url="/docs" if settings.debug else None,
    redoc_url="/redoc" if settings.debug else None,
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.allowed_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Add custom middleware
app.add_middleware(SecurityHeadersMiddleware)
app.add_middleware(RequestLoggingMiddleware)

# Add exception handlers
app.add_exception_handler(StarletteHTTPException, http_exception_handler)  # type: ignore
app.add_exception_handler(RequestValidationError, validation_exception_handler)  # type: ignore
app.add_exception_handler(Exception, general_exception_handler)  # type: ignore

# Include API routes
app.include_router(auth.router, prefix="/api/auth", tags=["authentication"])
app.include_router(chat.router, prefix="/api/chat", tags=["chat"])
app.include_router(projects.router, prefix="/api/projects", tags=["projects"])
app.include_router(workitems.router, prefix="/api", tags=["workitems"])


# Health check endpoints
@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {
        "status": "healthy",
        "message": "Azure DevOps AI Agent Backend is running",
        "version": settings.app_version,
        "environment": settings.environment,
    }


@app.get("/")
async def root():
    """Root endpoint."""
    return {
        "message": "Azure DevOps AI Agent Backend API",
        "version": settings.app_version,
        "docs_url": "/docs" if settings.debug else "Documentation disabled in production",
    }
