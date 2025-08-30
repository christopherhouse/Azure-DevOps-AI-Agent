"""OpenTelemetry configuration for Application Insights integration."""

import logging

from opentelemetry import metrics, trace
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.instrumentation.httpx import HTTPXClientInstrumentor
from opentelemetry.instrumentation.logging import LoggingInstrumentor
from opentelemetry.instrumentation.requests import RequestsInstrumentor
from opentelemetry.instrumentation.system_metrics import SystemMetricsInstrumentor
from opentelemetry.sdk.metrics import MeterProvider
from opentelemetry.sdk.metrics.export import PeriodicExportingMetricReader
from opentelemetry.sdk.resources import SERVICE_NAME, SERVICE_VERSION, Resource
from opentelemetry.sdk.trace import TracerProvider

from app.core.config import settings

logger = logging.getLogger(__name__)


def setup_telemetry() -> trace.Tracer | None:
    """Set up OpenTelemetry tracing, metrics, and logging with Azure Monitor."""

    if not settings.applicationinsights_connection_string:
        logger.warning("Application Insights connection string not configured - telemetry disabled")
        return None

    try:
        # Create resource with service identification
        resource = Resource.create(
            {
                SERVICE_NAME: settings.otel_service_name,
                SERVICE_VERSION: settings.otel_service_version,
                "service.instance.id": f"{settings.otel_service_name}-{settings.environment}",
                "deployment.environment": settings.environment,
            }
        )

        # Set up tracer provider with resource
        trace.set_tracer_provider(TracerProvider(resource=resource))
        tracer = trace.get_tracer(__name__)

        # Set up metrics provider with resource
        metric_readers = []

        # Try to set up Azure Monitor exporter for traces and metrics
        try:
            from azure.monitor.opentelemetry import configure_azure_monitor
            from azure.monitor.opentelemetry.exporter import AzureMonitorMetricExporter

            # Configure Azure Monitor with resource
            configure_azure_monitor(
                connection_string=settings.applicationinsights_connection_string,
                resource=resource,
                enable_logging=True,  # Enable logging export to App Insights
            )

            # Add metric reader for Azure Monitor
            metric_reader = PeriodicExportingMetricReader(
                exporter=AzureMonitorMetricExporter(
                    connection_string=settings.applicationinsights_connection_string
                ),
                export_interval_millis=60000,  # Export every 60 seconds
            )
            metric_readers.append(metric_reader)

            logger.info("Azure Monitor OpenTelemetry configured successfully")
        except ImportError:
            logger.warning("Azure Monitor OpenTelemetry not available - using basic tracing")
        except Exception as e:
            logger.error(f"Failed to configure Azure Monitor: {e}")

        # Set up metrics provider
        metrics.set_meter_provider(MeterProvider(resource=resource, metric_readers=metric_readers))

        # Set up comprehensive auto-instrumentation
        _setup_auto_instrumentation()

        # Set up logging instrumentation
        _setup_logging_instrumentation()

        # Set up system metrics collection
        _setup_system_metrics()

        logger.info("OpenTelemetry tracing, metrics, and logging configured successfully")
        return tracer

    except Exception as e:
        logger.error(f"Failed to configure OpenTelemetry: {e}")
        return None


def _setup_auto_instrumentation() -> None:
    """Set up auto-instrumentation for various libraries."""
    try:
        # FastAPI instrumentation
        FastAPIInstrumentor().instrument()

        # HTTP client instrumentations
        RequestsInstrumentor().instrument()

        # HTTPX instrumentation (if httpx is available)
        try:
            HTTPXClientInstrumentor().instrument()
        except Exception:  # nosec B110
            # HTTPX may not be used, skip silently - this is acceptable for optional instrumentation
            pass

        logger.debug("Auto-instrumentation configured successfully")
    except Exception as e:
        logger.error(f"Failed to set up auto-instrumentation: {e}")


def _setup_logging_instrumentation() -> None:
    """Set up logging instrumentation to send logs to Application Insights."""
    try:
        LoggingInstrumentor().instrument(set_logging_format=True)
        logger.debug("Logging instrumentation configured successfully")
    except Exception as e:
        logger.error(f"Failed to set up logging instrumentation: {e}")


def _setup_system_metrics() -> None:
    """Set up system metrics collection."""
    try:
        SystemMetricsInstrumentor().instrument()
        logger.debug("System metrics instrumentation configured successfully")
    except Exception as e:
        logger.error(f"Failed to set up system metrics: {e}")


def get_tracer(name: str) -> trace.Tracer:
    """Get a tracer instance for manual span creation."""
    return trace.get_tracer(name)


def create_span(name: str, attributes: dict | None = None) -> trace.Span:
    """Create a new span with optional attributes."""
    tracer = get_tracer(__name__)
    span = tracer.start_span(name)
    if attributes:
        span.set_attributes(attributes)
    return span


# Global tracer instance
tracer = setup_telemetry()
