"""Application Insights telemetry integration."""

import json
import logging
import uuid
from datetime import datetime
from typing import Any

from config import settings

logger = logging.getLogger(__name__)


class TelemetryClient:
    """Client for sending telemetry data to Application Insights."""

    def __init__(self):
        """Initialize telemetry client."""
        self.connection_string = settings.applicationinsights_connection_string
        self.enabled = settings.enable_telemetry and bool(self.connection_string)
        self.session_id = str(uuid.uuid4())

        if self.enabled:
            logger.info("Application Insights telemetry enabled")
        else:
            logger.info("Application Insights telemetry disabled")

    def track_event(
        self,
        name: str,
        properties: dict[str, Any] | None = None,
        measurements: dict[str, float] | None = None,
    ) -> None:
        """Track a custom event.

        Args:
            name: Event name
            properties: Custom properties
            measurements: Custom measurements
        """
        if not self.enabled:
            return

        try:
            event_data = {
                "name": name,
                "timestamp": datetime.utcnow().isoformat(),
                "session_id": self.session_id,
                "properties": properties or {},
                "measurements": measurements or {},
            }

            # In a real implementation, this would send to Application Insights
            # For now, we'll log the telemetry data
            logger.info(f"Telemetry Event: {json.dumps(event_data, indent=2)}")

        except Exception as e:
            logger.error(f"Failed to track event '{name}': {e}")

    def track_page_view(
        self,
        name: str,
        url: str | None = None,
        properties: dict[str, Any] | None = None,
    ) -> None:
        """Track a page view.

        Args:
            name: Page name
            url: Page URL
            properties: Custom properties
        """
        if not self.enabled:
            return

        try:
            page_data = {
                "name": name,
                "url": url,
                "timestamp": datetime.utcnow().isoformat(),
                "session_id": self.session_id,
                "properties": properties or {},
            }

            logger.info(f"Telemetry Page View: {json.dumps(page_data, indent=2)}")

        except Exception as e:
            logger.error(f"Failed to track page view '{name}': {e}")

    def track_exception(
        self, exception: Exception, properties: dict[str, Any] | None = None
    ) -> None:
        """Track an exception.

        Args:
            exception: Exception object
            properties: Custom properties
        """
        if not self.enabled:
            return

        try:
            exception_data = {
                "exception_type": type(exception).__name__,
                "message": str(exception),
                "timestamp": datetime.utcnow().isoformat(),
                "session_id": self.session_id,
                "properties": properties or {},
            }

            logger.error(f"Telemetry Exception: {json.dumps(exception_data, indent=2)}")

        except Exception as e:
            logger.error(f"Failed to track exception: {e}")

    def track_user_action(
        self,
        action: str,
        user_id: str | None = None,
        properties: dict[str, Any] | None = None,
    ) -> None:
        """Track a user action.

        Args:
            action: Action name
            user_id: User identifier
            properties: Custom properties
        """
        action_properties = {"user_id": user_id, **(properties or {})}

        self.track_event(f"user_action_{action}", action_properties)

    def track_api_call(
        self,
        endpoint: str,
        method: str,
        status_code: int,
        duration_ms: float,
        properties: dict[str, Any] | None = None,
    ) -> None:
        """Track an API call.

        Args:
            endpoint: API endpoint
            method: HTTP method
            status_code: Response status code
            duration_ms: Request duration in milliseconds
            properties: Custom properties
        """
        api_properties = {
            "endpoint": endpoint,
            "method": method,
            "status_code": status_code,
            **(properties or {}),
        }

        measurements = {"duration_ms": duration_ms}

        self.track_event("api_call", api_properties, measurements)

    def track_chat_message(
        self,
        message_type: str,  # 'user' or 'assistant'
        message_length: int,
        conversation_id: str | None = None,
        user_id: str | None = None,
    ) -> None:
        """Track a chat message.

        Args:
            message_type: Type of message (user or assistant)
            message_length: Length of message in characters
            conversation_id: Conversation identifier
            user_id: User identifier
        """
        properties = {
            "message_type": message_type,
            "conversation_id": conversation_id,
            "user_id": user_id,
        }

        measurements = {"message_length": float(message_length)}

        self.track_event("chat_message", properties, measurements)

    def get_javascript_snippet(self) -> str:
        """Get JavaScript snippet for client-side telemetry.

        Returns:
            HTML script tag with Application Insights JavaScript SDK
        """
        if not self.enabled or not self.connection_string:
            return ""

        # Extract instrumentation key from connection string
        # Format: InstrumentationKey=key;IngestionEndpoint=endpoint;...
        instrumentation_key = ""
        for part in self.connection_string.split(";"):
            if part.startswith("InstrumentationKey="):
                instrumentation_key = part.split("=", 1)[1]
                break

        if not instrumentation_key:
            logger.warning(
                "Could not extract instrumentation key from connection string"
            )
            return ""

        return f"""
        <script type="text/javascript">
        !function(T,l,y){{var S=T.location,k="script",D="instrumentationKey",C="ingestionendpoint",I="disableExceptionTracking",E="ai.device.",b="toLowerCase",w="crossOrigin",N="POST",e="appInsightsSDK",t=y.name||"appInsights";(y.name||T[e])&&(T[e]=t);var n=T[t]||function(d){{var g=!1,f=!1,m={{initialize:!0,queue:[],sv:"5",version:2,config:d}};function v(e,t){{var n={{}},a="Browser";return n[E+"id"]=a[b](),n[E+"type"]=a,n["ai.cloud.role"]=d.namePrefix||"",n["ai.internal.sdkVersion"]="javascript:snippet_"+(m.sv||m.version),n}}var h=d.url||y.src;if(h){{m.queue.push((function(){{var a=T.createElement(k);a.src=h;var e=T.getElementsByTagName(k)[0];e.parentNode.insertBefore(a,e)}}))}};function a(e){{m.queue.push((function(){{m[e].apply(m,arguments)}}))}}var r=["track","trackEvent","trackException","trackMetric","trackPageView","trackTrace","trackDependencyData"];r.push("flush"),r.forEach((function(e){{m[e]=function(){{a(e)}}}}));var s=!1;var o=m,p=function(e){{m=e}};return o.queue&&o.queue.length>0&&(o.track=function(){{o.queue.push((function(){{o.track.apply(o,arguments)}}));}},s=!0),o.initialize=function(e){{var t=e.config||{{}};t[D]=e[D],t.endpointUrl=e.endpointUrl||e.config.endpointUrl,e.queue&&e.queue.length>0&&(o.queue.push.apply(o.queue,e.queue),o.queue=[]),p(appInsights.loadAppInsights(t)),o.trackPageView({{}})}},o}}({{
        config:{{
        instrumentationKey: "{instrumentation_key}",
        connectionString: "{self.connection_string}",
        enableAutoRouteTracking: true,
        disableAjaxTracking: false,
        disableFetchTracking: false,
        enableCorsCorrelation: true,
        enableRequestHeaderTracking: true,
        enableResponseHeaderTracking: true
        }}
        }});
        window[n]=appInsights;
        appInsights.queue&&0===appInsights.queue.length?(appInsights.queue.push((function(){{appInsights.trackPageView(({{name:document.title}}))}})),appInsights.flush()):appInsights.trackPageView(({{name:document.title}})));
        }}(window,document,{{
        src: "https://js.monitor.azure.com/scripts/b/ai.2.min.js",
        crossOrigin: "anonymous",
        cfg: {{
        instrumentationKey: "{instrumentation_key}"
        }}
        }});
        </script>
        """


# Global telemetry client
telemetry = TelemetryClient()
