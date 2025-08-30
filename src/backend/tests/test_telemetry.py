"""Tests for OpenTelemetry telemetry configuration."""

import pytest
from unittest.mock import patch, MagicMock

from app.core.telemetry import setup_telemetry, get_tracer, create_span


class TestTelemetrySetup:
    """Test telemetry setup functionality."""

    def test_setup_telemetry_no_connection_string(self):
        """Test telemetry setup when no connection string is provided."""
        with patch("app.core.telemetry.settings") as mock_settings:
            mock_settings.applicationinsights_connection_string = None
            
            result = setup_telemetry()
            
            assert result is None

    def test_setup_telemetry_with_connection_string(self):
        """Test telemetry setup with a valid connection string."""
        with patch("app.core.telemetry.settings") as mock_settings:
            mock_settings.applicationinsights_connection_string = "InstrumentationKey=test-key"
            mock_settings.otel_service_name = "test-service"
            mock_settings.otel_service_version = "1.0.0"
            mock_settings.environment = "test"
            
            with patch("app.core.telemetry.trace") as mock_trace:
                with patch("app.core.telemetry.metrics") as mock_metrics:
                    with patch("app.core.telemetry._setup_auto_instrumentation"):
                        with patch("app.core.telemetry._setup_logging_instrumentation"):
                            with patch("app.core.telemetry._setup_system_metrics"):
                                mock_tracer = MagicMock()
                                mock_trace.get_tracer.return_value = mock_tracer
                                
                                result = setup_telemetry()
                                
                                assert result == mock_tracer
                                mock_trace.set_tracer_provider.assert_called_once()
                                mock_metrics.set_meter_provider.assert_called_once()

    def test_setup_telemetry_azure_monitor_import_error(self):
        """Test telemetry setup when Azure Monitor is not available."""
        with patch("app.core.telemetry.settings") as mock_settings:
            mock_settings.applicationinsights_connection_string = "InstrumentationKey=test-key"
            mock_settings.otel_service_name = "test-service"
            mock_settings.otel_service_version = "1.0.0"
            mock_settings.environment = "test"
            
            with patch("app.core.telemetry.trace") as mock_trace:
                with patch("app.core.telemetry.metrics") as mock_metrics:
                    with patch("app.core.telemetry._setup_auto_instrumentation"):
                        with patch("app.core.telemetry._setup_logging_instrumentation"):
                            with patch("app.core.telemetry._setup_system_metrics"):
                                # Mock Azure Monitor import error
                                with patch("builtins.__import__", side_effect=ImportError):
                                    mock_tracer = MagicMock()
                                    mock_trace.get_tracer.return_value = mock_tracer
                                    
                                    result = setup_telemetry()
                                    
                                    assert result == mock_tracer

    def test_setup_telemetry_exception_handling(self):
        """Test telemetry setup handles exceptions gracefully."""
        with patch("app.core.telemetry.settings") as mock_settings:
            mock_settings.applicationinsights_connection_string = "InstrumentationKey=test-key"
            
            with patch("app.core.telemetry.trace.set_tracer_provider", side_effect=Exception("Test error")):
                result = setup_telemetry()
                
                assert result is None


class TestTelemetryUtilities:
    """Test telemetry utility functions."""

    def test_get_tracer(self):
        """Test getting a tracer instance."""
        with patch("app.core.telemetry.trace") as mock_trace:
            mock_tracer = MagicMock()
            mock_trace.get_tracer.return_value = mock_tracer
            
            result = get_tracer("test-service")
            
            mock_trace.get_tracer.assert_called_once_with("test-service")
            assert result == mock_tracer

    def test_create_span_without_attributes(self):
        """Test creating a span without attributes."""
        with patch("app.core.telemetry.get_tracer") as mock_get_tracer:
            mock_tracer = MagicMock()
            mock_span = MagicMock()
            mock_tracer.start_span.return_value = mock_span
            mock_get_tracer.return_value = mock_tracer
            
            result = create_span("test-span")
            
            mock_tracer.start_span.assert_called_once_with("test-span")
            mock_span.set_attributes.assert_not_called()
            assert result == mock_span

    def test_create_span_with_attributes(self):
        """Test creating a span with attributes."""
        with patch("app.core.telemetry.get_tracer") as mock_get_tracer:
            mock_tracer = MagicMock()
            mock_span = MagicMock()
            mock_tracer.start_span.return_value = mock_span
            mock_get_tracer.return_value = mock_tracer
            
            attributes = {"key1": "value1", "key2": "value2"}
            result = create_span("test-span", attributes)
            
            mock_tracer.start_span.assert_called_once_with("test-span")
            mock_span.set_attributes.assert_called_once_with(attributes)
            assert result == mock_span


class TestInstrumentationSetup:
    """Test individual instrumentation setup functions."""

    def test_setup_auto_instrumentation(self):
        """Test auto-instrumentation setup."""
        from app.core.telemetry import _setup_auto_instrumentation
        
        with patch("app.core.telemetry.FastAPIInstrumentor") as mock_fastapi:
            with patch("app.core.telemetry.RequestsInstrumentor") as mock_requests:
                with patch("app.core.telemetry.HTTPXClientInstrumentor") as mock_httpx:
                    mock_fastapi_instance = MagicMock()
                    mock_requests_instance = MagicMock()
                    mock_httpx_instance = MagicMock()
                    
                    mock_fastapi.return_value = mock_fastapi_instance
                    mock_requests.return_value = mock_requests_instance
                    mock_httpx.return_value = mock_httpx_instance
                    
                    _setup_auto_instrumentation()
                    
                    mock_fastapi_instance.instrument.assert_called_once()
                    mock_requests_instance.instrument.assert_called_once()
                    mock_httpx_instance.instrument.assert_called_once()

    def test_setup_auto_instrumentation_httpx_error(self):
        """Test auto-instrumentation setup when HTTPX fails."""
        from app.core.telemetry import _setup_auto_instrumentation
        
        with patch("app.core.telemetry.FastAPIInstrumentor") as mock_fastapi:
            with patch("app.core.telemetry.RequestsInstrumentor") as mock_requests:
                with patch("app.core.telemetry.HTTPXClientInstrumentor") as mock_httpx:
                    mock_fastapi_instance = MagicMock()
                    mock_requests_instance = MagicMock()
                    mock_httpx_instance = MagicMock()
                    mock_httpx_instance.instrument.side_effect = Exception("HTTPX error")
                    
                    mock_fastapi.return_value = mock_fastapi_instance
                    mock_requests.return_value = mock_requests_instance
                    mock_httpx.return_value = mock_httpx_instance
                    
                    # Should not raise exception
                    _setup_auto_instrumentation()
                    
                    mock_fastapi_instance.instrument.assert_called_once()
                    mock_requests_instance.instrument.assert_called_once()

    def test_setup_logging_instrumentation(self):
        """Test logging instrumentation setup."""
        from app.core.telemetry import _setup_logging_instrumentation
        
        with patch("app.core.telemetry.LoggingInstrumentor") as mock_logging:
            mock_logging_instance = MagicMock()
            mock_logging.return_value = mock_logging_instance
            
            _setup_logging_instrumentation()
            
            mock_logging_instance.instrument.assert_called_once_with(set_logging_format=True)

    def test_setup_system_metrics(self):
        """Test system metrics instrumentation setup."""
        from app.core.telemetry import _setup_system_metrics
        
        with patch("app.core.telemetry.SystemMetricsInstrumentor") as mock_system:
            mock_system_instance = MagicMock()
            mock_system.return_value = mock_system_instance
            
            _setup_system_metrics()
            
            mock_system_instance.instrument.assert_called_once()