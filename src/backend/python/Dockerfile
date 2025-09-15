# Optimized Backend Dockerfile for faster CI builds with uv
FROM python:3.11-slim

# Set environment variables for optimal Python performance
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy

# Install system dependencies and uv in a single layer
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    apt-get clean && \
    pip install --no-cache-dir uv

# Create non-root user
RUN groupadd --gid 1000 appuser && \
    useradd --uid 1000 --gid appuser --shell /bin/bash --create-home appuser

# Set working directory
WORKDIR /app

# Copy uv files first for better Docker layer caching
COPY uv.lock pyproject.toml ./

# Copy LICENSE and README files to expected locations for hatchling metadata validation
COPY LICENSE /LICENSE
COPY README.md /README.md

# Install dependencies using uv
RUN uv sync --frozen --no-cache --no-dev

# Copy application code
COPY --chown=appuser:appuser . .

# Create necessary directories and set permissions
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8000/health || exit 1

# Command to run the application using uv
CMD ["uv", "run", "uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]