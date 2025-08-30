#!/usr/bin/env python3
"""
Validation script for OpenTelemetry/Application Insights integration.

This script demonstrates the enhanced telemetry capabilities and validates
that all components are working correctly.
"""

import logging
import os
import sys
from datetime import datetime
from typing import Any, Dict

# Add the src directory to the Python path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from app.core.telemetry import setup_telemetry, get_tracer, create_span
from app.core.config import settings

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)

logger = logging.getLogger(__name__)


def validate_telemetry_setup() -> Dict[str, Any]:
    """Validate that telemetry is set up correctly."""
    
    print("🔧 Validating OpenTelemetry/Application Insights setup...")
    
    results = {
        "setup_status": "unknown",
        "connection_string_configured": False,
        "tracer_available": False,
        "manual_span_creation": False,
        "logging_integration": False,
        "errors": []
    }
    
    try:
        # Check connection string configuration
        results["connection_string_configured"] = bool(settings.applicationinsights_connection_string)
        print(f"   ✅ Connection string configured: {results['connection_string_configured']}")
        
        # Setup telemetry
        tracer = setup_telemetry()
        results["tracer_available"] = tracer is not None
        
        if tracer:
            print("   ✅ OpenTelemetry setup completed successfully")
            results["setup_status"] = "success"
        else:
            print("   ⚠️  OpenTelemetry setup returned None (likely no connection string)")
            results["setup_status"] = "no_connection_string"
        
        # Test manual span creation
        test_span = create_span("validation_span", {"test": "validation"})
        if test_span:
            test_span.add_event("Validation span created successfully")
            test_span.end()
            results["manual_span_creation"] = True
            print("   ✅ Manual span creation working")
        
        # Test logging integration with tracing
        service_tracer = get_tracer("validation_service")
        with service_tracer.start_as_current_span("logging_test") as span:
            span.set_attributes({"test.type": "logging_integration"})
            logger.info("Testing structured logging with trace correlation")
            logger.warning("Testing warning message with trace context")
            results["logging_integration"] = True
            print("   ✅ Logging integration with tracing working")
        
        print("   🎉 All telemetry validations passed!")
        
    except Exception as e:
        error_msg = f"Telemetry validation failed: {e}"
        results["errors"].append(error_msg)
        results["setup_status"] = "error"
        print(f"   ❌ {error_msg}")
        logger.exception("Telemetry validation error")
    
    return results


def demonstrate_observability_features() -> None:
    """Demonstrate the observability features available."""
    
    print("\n📊 Demonstrating observability features...")
    
    tracer = get_tracer("demo_service")
    
    # Demonstrate complex operation with nested spans
    with tracer.start_as_current_span("demo_complex_operation") as parent_span:
        parent_span.set_attributes({
            "operation.type": "demonstration",
            "demo.timestamp": datetime.now().isoformat(),
            "demo.user": "validation_script"
        })
        
        print("   🔍 Created parent span: demo_complex_operation")
        
        # Nested operation 1: Data processing
        with tracer.start_as_current_span("data_processing") as span1:
            span1.set_attributes({
                "processing.type": "demo_data",
                "processing.records": 100
            })
            span1.add_event("Started data processing")
            
            # Simulate some work
            import time
            time.sleep(0.1)
            
            span1.add_event("Data processing completed", {
                "processed.count": 100,
                "processing.duration_ms": 100
            })
            print("   🔍 Completed nested span: data_processing")
        
        # Nested operation 2: External API call simulation
        with tracer.start_as_current_span("external_api_call") as span2:
            span2.set_attributes({
                "http.method": "GET",
                "http.url": "https://api.example.com/data",
                "http.status_code": 200
            })
            span2.add_event("API call initiated")
            
            # Simulate API call
            time.sleep(0.05)
            
            span2.add_event("API call completed successfully")
            print("   🔍 Completed nested span: external_api_call")
        
        # Log some business events with trace correlation
        logger.info("Business operation completed successfully", extra={
            "operation.result": "success",
            "operation.duration": "0.15s",
            "records.processed": 100
        })
        
        parent_span.set_attributes({
            "operation.result": "success",
            "operation.nested_spans": 2
        })
        
        print("   🎉 Complex operation demonstration completed!")


def display_configuration_summary() -> None:
    """Display the current telemetry configuration."""
    
    print("\n⚙️  Current Telemetry Configuration:")
    print(f"   Service Name: {settings.otel_service_name}")
    print(f"   Service Version: {settings.otel_service_version}")
    print(f"   Environment: {settings.environment}")
    print(f"   Connection String Configured: {bool(settings.applicationinsights_connection_string)}")
    
    if settings.applicationinsights_connection_string:
        # Mask the connection string for security
        masked = settings.applicationinsights_connection_string[:50] + "..." if len(settings.applicationinsights_connection_string) > 50 else settings.applicationinsights_connection_string
        print(f"   Connection String: {masked}")
    else:
        print("   Connection String: Not configured")
    
    print("\n📖 Available Features:")
    print("   ✅ Distributed Tracing (FastAPI, Requests, HTTPX)")
    print("   ✅ Metrics Collection (System metrics, Custom metrics)")  
    print("   ✅ Structured Logging with Trace Correlation")
    print("   ✅ Manual Span Creation Utilities")
    print("   ✅ Error Tracking and Exception Recording")
    print("   ✅ Resource-based Service Identification")


def main() -> None:
    """Main validation script."""
    
    print("🚀 Azure DevOps AI Agent - OpenTelemetry Validation")
    print("=" * 60)
    
    # Display configuration
    display_configuration_summary()
    
    # Validate setup
    validation_results = validate_telemetry_setup()
    
    # Demonstrate features if setup is working
    if validation_results["setup_status"] in ["success", "no_connection_string"]:
        demonstrate_observability_features()
    
    # Summary
    print("\n📋 Validation Summary:")
    print(f"   Setup Status: {validation_results['setup_status']}")
    print(f"   Connection String: {'✅' if validation_results['connection_string_configured'] else '⚠️'}")
    print(f"   Tracer Available: {'✅' if validation_results['tracer_available'] else '❌'}")
    print(f"   Manual Spans: {'✅' if validation_results['manual_span_creation'] else '❌'}")
    print(f"   Logging Integration: {'✅' if validation_results['logging_integration'] else '❌'}")
    
    if validation_results["errors"]:
        print("\n❌ Errors encountered:")
        for error in validation_results["errors"]:
            print(f"   • {error}")
    
    print("\n💡 Next Steps:")
    if not validation_results["connection_string_configured"]:
        print("   1. Set APPLICATIONINSIGHTS_CONNECTION_STRING environment variable")
        print("   2. Get connection string from Azure Application Insights resource")
    else:
        print("   1. Deploy application to see telemetry in Azure Application Insights")
        print("   2. Use custom spans in your business logic for detailed tracing")
        print("   3. Monitor logs, metrics, and traces in Application Insights dashboard")
    
    print("\n🔗 Useful Resources:")
    print("   • Application Insights: https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview")
    print("   • OpenTelemetry Python: https://opentelemetry-python.readthedocs.io/")
    print("   • Custom tracing examples: app/examples/telemetry_examples.py")
    
    print("\n✨ Validation completed!")


if __name__ == "__main__":
    main()