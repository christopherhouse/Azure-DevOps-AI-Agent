"""Example demonstrating enhanced OpenTelemetry usage in the backend."""

import logging
import time
from typing import Any

from opentelemetry import trace

from app.core.telemetry import create_span, get_tracer

logger = logging.getLogger(__name__)


def example_custom_tracing() -> list[dict[str, str]]:
    """Example of how to use custom tracing in your business logic."""

    # Get a tracer for this module
    tracer = get_tracer(__name__)

    # Start a custom span
    with tracer.start_as_current_span("example_business_operation") as span:
        # Add custom attributes to the span
        span.set_attributes(
            {
                "operation.type": "business_logic",
                "operation.name": "example_custom_tracing",
                "user.id": "example-user-123",
            }
        )

        # Simulate some work
        time.sleep(0.1)

        # Add events to the span
        span.add_event("Starting data processing")

        # Simulate some business logic
        result = _process_data()

        # Add more attributes based on results
        span.set_attributes({"result.count": len(result), "result.success": True})

        span.add_event("Data processing completed")

        return result


def example_nested_spans() -> dict[str, Any]:
    """Example of nested spans for complex operations."""

    tracer = get_tracer(__name__)

    with tracer.start_as_current_span("complex_operation") as parent_span:
        parent_span.set_attributes({"operation.type": "complex"})

        # First sub-operation
        with tracer.start_as_current_span("sub_operation_1") as span1:
            span1.set_attributes({"sub_operation.id": 1})
            time.sleep(0.05)
            span1.add_event("Sub-operation 1 completed")

        # Second sub-operation
        with tracer.start_as_current_span("sub_operation_2") as span2:
            span2.set_attributes({"sub_operation.id": 2})
            time.sleep(0.05)
            span2.add_event("Sub-operation 2 completed")

        # Final result
        result = {"operation": "complex", "sub_operations": 2}
        parent_span.set_attributes({"result.status": "success"})

        return result


def example_error_handling() -> None:
    """Example of how to handle errors in spans."""

    tracer = get_tracer(__name__)

    with tracer.start_as_current_span("operation_with_error") as span:
        span.set_attributes({"operation.type": "error_example"})

        try:
            # Simulate an operation that might fail
            _risky_operation()
            span.set_status(status=trace.Status(trace.StatusCode.OK))

        except Exception as e:
            # Record the error in the span
            span.record_exception(e)
            span.set_status(
                status=trace.Status(status_code=trace.StatusCode.ERROR, description=str(e))
            )

            # Log the error (which will also be captured by OpenTelemetry logging)
            logger.error(f"Operation failed: {e}", exc_info=True)

            # Re-raise or handle as needed
            raise


def example_manual_span_creation() -> dict[str, Any]:
    """Example using the create_span utility function."""

    # Create a span with attributes
    span = create_span(
        "manual_operation", {"operation.type": "manual", "created_by": "utility_function"}
    )

    try:
        # Do some work
        result = {"status": "completed", "method": "manual"}

        # Add more attributes
        span.set_attributes({"result.items": 1})
        span.add_event("Manual operation completed successfully")

        return result

    finally:
        # Always end the span
        span.end()


def example_logging_with_tracing() -> None:
    """Example of how logging integrates with tracing."""

    tracer = get_tracer(__name__)

    with tracer.start_as_current_span("logging_example") as span:
        span.set_attributes({"operation.type": "logging_demo"})

        # These log messages will be automatically correlated with the current span
        logger.info("Starting logging example operation")
        logger.debug("This is a debug message with trace correlation")
        logger.warning("This is a warning message")

        # You can also add structured logging data
        logger.info(
            "Operation progress update",
            extra={"progress": 50, "operation_id": "example-123", "user_id": "user-456"},
        )

        logger.info("Logging example completed successfully")


def _process_data() -> list[dict[str, str]]:
    """Simulate some data processing."""
    return [
        {"id": "1", "name": "Item 1"},
        {"id": "2", "name": "Item 2"},
        {"id": "3", "name": "Item 3"},
    ]


def _risky_operation() -> None:
    """Simulate an operation that might fail."""
    import random

    if random.random() > 0.5:  # nosec B311
        # Using standard random for demo purposes only - not for security/crypto
        raise ValueError("Simulated error for demonstration purposes")


if __name__ == "__main__":
    # Run examples (for testing purposes)
    from opentelemetry import trace

    from app.core.telemetry import setup_telemetry

    # Setup telemetry
    setup_telemetry()

    print("Running telemetry examples...")

    try:
        print("1. Custom tracing example:")
        result1 = example_custom_tracing()
        print(f"   Result: {result1}")

        print("2. Nested spans example:")
        result2 = example_nested_spans()
        print(f"   Result: {result2}")

        print("3. Manual span creation example:")
        result3 = example_manual_span_creation()
        print(f"   Result: {result3}")

        print("4. Logging with tracing example:")
        example_logging_with_tracing()

        print("5. Error handling example:")
        try:
            example_error_handling()
        except ValueError as e:
            print(f"   Caught expected error: {e}")

        print("All examples completed!")

    except Exception as e:
        print(f"Error running examples: {e}")
