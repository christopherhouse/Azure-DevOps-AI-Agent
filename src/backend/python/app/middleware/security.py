"""Request validation middleware."""

import logging
import time
from collections.abc import Awaitable, Callable

from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware

logger = logging.getLogger(__name__)


class RequestLoggingMiddleware(BaseHTTPMiddleware):
    """Middleware for logging requests and responses."""

    async def dispatch(
        self, request: Request, call_next: Callable[[Request], Awaitable[Response]]
    ) -> Response:
        """Process request and log details."""
        start_time = time.time()

        # Log request
        logger.info(
            f"Request: {request.method} {request.url} - "
            f"User-Agent: {request.headers.get('user-agent', 'unknown')}"
        )

        # Process request
        try:
            response = await call_next(request)
        except Exception as e:
            logger.error(f"Request failed: {e}")
            raise

        # Calculate processing time
        process_time = time.time() - start_time

        # Log response
        logger.info(f"Response: {response.status_code} - Processing time: {process_time:.3f}s")

        # Add processing time header
        response.headers["X-Process-Time"] = str(process_time)

        return response


class SecurityHeadersMiddleware(BaseHTTPMiddleware):
    """Middleware for adding security headers."""

    async def dispatch(
        self, request: Request, call_next: Callable[[Request], Awaitable[Response]]
    ) -> Response:
        """Add security headers to response."""
        response = await call_next(request)

        # Add security headers
        response.headers["X-Content-Type-Options"] = "nosniff"
        response.headers["X-Frame-Options"] = "DENY"
        response.headers["X-XSS-Protection"] = "1; mode=block"
        response.headers["Referrer-Policy"] = "strict-origin-when-cross-origin"

        # Add HSTS header for HTTPS
        if request.url.scheme == "https":
            response.headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains"

        return response
