"""OpenTelemetry configuration for Application Insights integration."""

import logging

from opentelemetry import trace
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.instrumentation.requests import RequestsInstrumentor
from opentelemetry.sdk.trace import TracerProvider

from app.core.config import settings

logger = logging.getLogger(__name__)


def setup_telemetry() -> trace.Tracer | None:
    """Set up OpenTelemetry tracing with Azure Monitor."""

    if not settings.applicationinsights_connection_string:
        logger.warning("Application Insights connection string not configured - telemetry disabled")
        return None

    try:
        # Set up tracer provider
        trace.set_tracer_provider(TracerProvider())
        tracer = trace.get_tracer(__name__)

        # Try to set up Azure Monitor exporter
        try:
            from azure.monitor.opentelemetry import configure_azure_monitor

            configure_azure_monitor(
                connection_string=settings.applicationinsights_connection_string
            )
            logger.info("Azure Monitor OpenTelemetry configured successfully")
        except ImportError:
            logger.warning("Azure Monitor OpenTelemetry not available - using basic tracing")

        # Set up auto-instrumentation for FastAPI and requests
        FastAPIInstrumentor.instrument()
        RequestsInstrumentor.instrument()

        logger.info("OpenTelemetry tracing configured successfully")
        return tracer

    except Exception as e:
        logger.error(f"Failed to configure OpenTelemetry: {e}")
        return None


# Global tracer instance
tracer = setup_telemetry()
