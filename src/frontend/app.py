"""Main Gradio application for Azure DevOps AI Agent."""

import logging
import sys
from pathlib import Path

# Add the current directory to Python path for imports
sys.path.insert(0, str(Path(__file__).parent))

import gradio as gr
from dotenv import load_dotenv

from components.auth import auth_state, create_login_interface
from components.chat import create_chat_interface
from config import settings
from services.telemetry import telemetry

# Load environment variables
load_dotenv()

# Configure logging
logging.basicConfig(
    level=logging.INFO if settings.debug else logging.WARNING,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler("frontend.log") if not settings.debug else logging.NullHandler(),
    ],
)

logger = logging.getLogger(__name__)


def create_main_interface() -> gr.Blocks:
    """Create the main application interface.

    Returns:
        Gradio Blocks interface
    """
    # Custom CSS for the application
    custom_css = """
    /* Global styles */
    .gradio-container {
        max-width: 1400px !important;
        margin: 0 auto;
    }

    /* Header styles */
    .app-header {
        background: linear-gradient(135deg, #0078d4 0%, #106ebe 100%);
        color: white;
        padding: 1.5rem;
        border-radius: 12px;
        margin-bottom: 2rem;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }

    .app-header h1 {
        margin: 0;
        font-size: 2.5rem;
        font-weight: 600;
    }

    .app-header p {
        margin: 0.5rem 0 0 0;
        font-size: 1.1rem;
        opacity: 0.9;
    }

    /* Chat interface styles */
    .chat-container {
        background: white;
        border-radius: 12px;
        padding: 1.5rem;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
        border: 1px solid #e1e5e9;
    }

    /* Login interface styles */
    .login-container {
        max-width: 500px;
        margin: 2rem auto;
        background: white;
        padding: 3rem;
        border-radius: 16px;
        box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
        border: 1px solid #e1e5e9;
    }

    .login-container h1 {
        text-align: center;
        color: #323130;
        margin-bottom: 0.5rem;
    }

    .login-container h3 {
        text-align: center;
        color: #605e5c;
        margin-bottom: 2rem;
        font-weight: 400;
    }

    /* Button styles */
    .primary-button {
        background: #0078d4 !important;
        border: none !important;
        color: white !important;
        font-weight: 600 !important;
        padding: 12px 24px !important;
        border-radius: 6px !important;
        transition: all 0.2s ease !important;
    }

    .primary-button:hover {
        background: #106ebe !important;
        transform: translateY(-1px);
        box-shadow: 0 4px 8px rgba(0, 120, 212, 0.3) !important;
    }

    /* Status indicator styles */
    .status-connected {
        background: #dff6dd !important;
        border: 1px solid #107c10 !important;
        color: #107c10 !important;
        padding: 0.75rem !important;
        border-radius: 6px !important;
        font-weight: 500 !important;
    }

    .status-error {
        background: #fde7e9 !important;
        border: 1px solid #d13438 !important;
        color: #d13438 !important;
        padding: 0.75rem !important;
        border-radius: 6px !important;
        font-weight: 500 !important;
    }

    /* Example prompts */
    .example-prompts {
        background: #f8f9fa;
        padding: 1rem;
        border-radius: 8px;
        margin: 1rem 0;
        border: 1px solid #e1e5e9;
    }

    .example-prompts h4 {
        margin-top: 0;
        color: #323130;
    }

    /* Footer */
    .app-footer {
        text-align: center;
        padding: 2rem;
        color: #605e5c;
        font-size: 0.9rem;
        border-top: 1px solid #e1e5e9;
        margin-top: 3rem;
    }

    /* Responsive design */
    @media (max-width: 768px) {
        .gradio-container {
            padding: 1rem;
        }

        .app-header h1 {
            font-size: 2rem;
        }

        .login-container {
            margin: 1rem;
            padding: 2rem;
        }

        .chat-container {
            padding: 1rem;
        }
    }
    """

    # Add Application Insights JavaScript SDK
    app_insights_js = telemetry.get_javascript_snippet()
    if app_insights_js:
        custom_css += f"\n/* Application Insights */\n{app_insights_js}"

    with gr.Blocks(
        title="Azure DevOps AI Agent",
        theme=gr.themes.Soft(primary_hue="blue", secondary_hue="gray", neutral_hue="gray"),
        css=custom_css,
        head=app_insights_js,
    ) as app:
        # Application header
        with gr.Row():
            with gr.Column():
                gr.HTML("""
                <div class="app-header">
                    <h1>🤖 Azure DevOps AI Agent</h1>
                    <p>Intelligent assistant for Azure DevOps project management</p>
                </div>
                """)

        # Authentication state and interface switching
        # Track authentication state
        gr.State(value=auth_state.is_authenticated())

        # Conditional interface rendering based on authentication
        with gr.Column(visible=not auth_state.is_authenticated()):
            create_login_interface()

        with gr.Column(visible=auth_state.is_authenticated()):
            create_chat_interface()

        # Footer
        with gr.Row():
            gr.HTML("""
            <div class="app-footer">
                <p>
                    🔒 Secured with Microsoft Entra ID |
                    📊 Powered by Azure OpenAI |
                    🛠️ Built with Gradio & FastAPI
                </p>
                <p>
                    <small>
                        For support, please contact your system administrator or
                        <a href="https://github.com/christopherhouse/Azure-DevOps-AI-Agent" target="_blank">
                            visit our GitHub repository
                        </a>
                    </small>
                </p>
            </div>
            """)

        # Track page view
        def track_app_load():
            """Track application load."""
            try:
                telemetry.track_page_view(
                    name="main_app",
                    url=settings.frontend_url,
                    properties={
                        "environment": settings.environment,
                        "authenticated": auth_state.is_authenticated(),
                    },
                )
            except Exception as e:
                logger.error(f"Failed to track page view: {e}")

        # Load event handler
        app.load(fn=track_app_load)

    return app


def main():
    """Main application entry point."""
    try:
        # Set Gradio environment variables for containerized environments
        import os

        os.environ["GRADIO_SHARE"] = "False"
        os.environ["GRADIO_SERVER_NAME"] = "0.0.0.0"

        # Patch gradio_client to fix schema parsing bug in version 4.44.1
        try:
            from gradio_client import utils as gradio_utils

            original_json_schema_to_python_type = gradio_utils._json_schema_to_python_type

            def patched_json_schema_to_python_type(schema, defs=None):
                # Handle case where schema is bool instead of dict
                if isinstance(schema, bool):
                    return "bool"
                if not isinstance(schema, dict):
                    return str(type(schema).__name__)
                return original_json_schema_to_python_type(schema, defs)

            gradio_utils._json_schema_to_python_type = patched_json_schema_to_python_type
            logger.info("Applied gradio_client patch for schema parsing bug")
        except Exception as e:
            logger.warning(f"Could not apply gradio_client patch: {e}")

        logger.info("Starting Azure DevOps AI Agent frontend...")
        logger.info(f"Environment: {settings.environment}")
        logger.info(f"Frontend URL: {settings.frontend_url}")
        logger.info(f"Backend URL: {settings.backend_url}")
        logger.info(f"Telemetry enabled: {settings.enable_telemetry}")

        # Track application startup
        telemetry.track_event(
            "app_startup",
            properties={"environment": settings.environment, "version": "1.0.0"},
        )

        # Create and launch the interface
        app = create_main_interface()

        # Launch configuration
        launch_kwargs = {
            "server_name": "0.0.0.0",  # Allow external connections
            "server_port": 7860,
            "show_api": False,  # Hide API docs in production
            "share": False,  # Disable sharing for containerized environments
            "inbrowser": settings.environment == "development",  # Only open browser in dev
            "favicon_path": None,  # Could add custom favicon
            "ssl_verify": settings.require_https,
            "auth": None,  # We handle auth internally
            "max_threads": 10,
            "show_error": settings.debug,
        }

        logger.info(f"Launching Gradio app on port {launch_kwargs['server_port']}")

        # Try to launch the app, handling potential API generation errors
        try:
            app.launch(**launch_kwargs)
        except TypeError as e:
            if "argument of type 'bool' is not iterable" in str(e):
                logger.warning(f"Gradio API generation error (known issue with Gradio 4.44.1): {e}")
                logger.info("Attempting to launch with minimal configuration...")

                # Launch with absolute minimal configuration
                minimal_kwargs = {
                    "server_name": "0.0.0.0",
                    "server_port": 7860,
                    "share": False,
                    "show_api": False,
                    "quiet": True,  # Suppress most output
                    "show_error": False,  # Disable error display that might trigger API generation
                    "max_threads": 10,
                }
                app.launch(**minimal_kwargs)
            else:
                raise  # Re-raise if it's a different TypeError

    except KeyboardInterrupt:
        logger.info("Application stopped by user")
        telemetry.track_event("app_shutdown", properties={"reason": "user_interrupt"})
    except Exception as e:
        logger.error(f"Application startup failed: {e}")
        telemetry.track_exception(e, properties={"stage": "startup"})
        sys.exit(1)


if __name__ == "__main__":
    main()
