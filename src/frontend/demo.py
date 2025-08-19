"""Simple test script to validate the Gradio interface components work."""

import os
import sys
from pathlib import Path

# Add the current directory to Python path for imports
sys.path.insert(0, str(Path(__file__).parent))

import gradio as gr

# Mock the environment variables for testing
os.environ.update(
    {
        "AZURE_TENANT_ID": "test-tenant-id",
        "AZURE_CLIENT_ID": "test-client-id",
        "AZURE_CLIENT_SECRET": "test-secret",
        "ENVIRONMENT": "development",
        "DEBUG": "true",
        "ENABLE_TELEMETRY": "false",
    }
)


def create_demo_interface():
    """Create a demo interface showing our UI components."""

    with gr.Blocks(
        title="Azure DevOps AI Agent - Demo",
        theme=gr.themes.Soft(
            primary_hue="blue", secondary_hue="gray", neutral_hue="gray"
        ),
        css="""
        .demo-header {
            background: linear-gradient(135deg, #0078d4 0%, #106ebe 100%);
            color: white;
            padding: 2rem;
            border-radius: 12px;
            margin-bottom: 2rem;
            box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
            text-align: center;
        }
        .demo-section {
            background: white;
            border-radius: 12px;
            padding: 1.5rem;
            margin: 1rem 0;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
            border: 1px solid #e1e5e9;
        }
        """,
    ) as demo:
        # Header
        with gr.Row():
            with gr.Column():
                gr.HTML("""
                <div class="demo-header">
                    <h1>🤖 Azure DevOps AI Agent</h1>
                    <p>Frontend Demo - Minimal Gradio UI Implementation</p>
                </div>
                """)

        # Login Interface Demo
        with gr.Column(elem_classes=["demo-section"]):
            gr.Markdown("## 🔐 Authentication Interface")
            gr.Markdown("This is what users see before authentication:")

            with gr.Row():
                with gr.Column(scale=1):
                    gr.Markdown("### Login Form")
                    gr.Button("Sign in with Microsoft", variant="primary", size="lg")
                    gr.Markdown(
                        "*Status: Ready to authenticate*", elem_classes=["status-info"]
                    )

                with gr.Column(scale=2):
                    gr.Markdown("### Features")
                    gr.Markdown("""
                    - ✅ Microsoft Entra ID integration
                    - ✅ OAuth 2.0 flow with PKCE
                    - ✅ Secure token exchange
                    - ✅ Session management
                    - ✅ Automatic token refresh
                    """)

        # Chat Interface Demo
        with gr.Column(elem_classes=["demo-section"]):
            gr.Markdown("## 💬 Chat Interface")
            gr.Markdown("This is the main interface after authentication:")

            # Mock chat
            gr.Chatbot(
                value=[
                    (
                        "Hello! Can you help me create a new Azure DevOps project?",
                        "I'd be happy to help you create a new Azure DevOps project! To get started, I'll need a few details:\n\n1. **Project Name**: What would you like to call your project?\n2. **Project Description**: A brief description of what this project is for\n3. **Process Template**: Which process would you prefer (Agile, Scrum, or Basic)?\n4. **Visibility**: Should this be a public or private project?\n\nOnce you provide these details, I can create the project for you using the Azure DevOps REST API.",
                    ),
                    (
                        "Let's create a project called 'WebApp Dashboard' for our new customer portal",
                        "Perfect! I'll create the 'WebApp Dashboard' project for you. Let me set this up:\n\n🔄 **Creating Azure DevOps Project...**\n\n✅ Project Name: WebApp Dashboard\n✅ Description: Customer portal web application\n✅ Process Template: Agile (default)\n✅ Visibility: Private\n\n**Project created successfully!** 🎉\n\nYour new project is ready at: `https://dev.azure.com/your-org/WebApp Dashboard`\n\nWould you like me to:\n- Create initial repositories?\n- Set up build pipelines?\n- Create some initial work items?",
                    ),
                ],
                label="Conversation with AI Agent",
                height=400,
            )

            with gr.Row():
                gr.Textbox(
                    placeholder="Ask me about Azure DevOps... (e.g., 'Show me all projects')",
                    label="Your message",
                    lines=2,
                    scale=4,
                )
                gr.Button("Send", variant="primary", scale=1)

            with gr.Row():
                gr.Button("Clear Chat", variant="secondary", size="sm")
                gr.Button("Export History", variant="secondary", size="sm")

        # Example Prompts Demo
        with gr.Column(elem_classes=["demo-section"]):
            gr.Markdown("## 💡 Example Capabilities")

            with gr.Row():
                with gr.Column():
                    gr.Markdown("### Project Management")
                    for example in [
                        "List all my Azure DevOps projects",
                        "Create a new project called 'MobileApp'",
                        "Show project settings for WebApp",
                        "Archive the old TestProject",
                    ]:
                        gr.Button(example, variant="secondary", size="sm")

                with gr.Column():
                    gr.Markdown("### Work Items")
                    for example in [
                        "Show me open work items",
                        "Create a user story for login feature",
                        "Update bug #123 status to resolved",
                        "Create a sprint planning board",
                    ]:
                        gr.Button(example, variant="secondary", size="sm")

                with gr.Column():
                    gr.Markdown("### Repositories & Pipelines")
                    for example in [
                        "List all repositories",
                        "Create a new Git repository",
                        "Show recent pipeline runs",
                        "Set up a build pipeline",
                    ]:
                        gr.Button(example, variant="secondary", size="sm")

        # Technical Features Demo
        with gr.Column(elem_classes=["demo-section"]):
            gr.Markdown("## ⚙️ Technical Implementation")

            with gr.Row():
                with gr.Column():
                    gr.Markdown("### Frontend Features")
                    gr.Markdown("""
                    - **Framework**: Gradio 5.x with modern theme
                    - **Authentication**: Microsoft MSAL integration
                    - **API Client**: Async HTTP with proper error handling
                    - **Telemetry**: Application Insights JavaScript SDK
                    - **Styling**: Custom CSS with Azure design system
                    - **Responsive**: Mobile-friendly responsive design
                    """)

                with gr.Column():
                    gr.Markdown("### Security & Configuration")
                    gr.Markdown("""
                    - **Environment Variables**: External configuration
                    - **Token Management**: Secure token storage and refresh
                    - **CORS**: Configurable origin restrictions
                    - **HTTPS**: Production SSL/TLS enforcement
                    - **Health Checks**: Built-in monitoring endpoints
                    - **Containerization**: Multi-stage Docker builds
                    """)

        # Status Demo
        with gr.Column(elem_classes=["demo-section"]):
            gr.Markdown("## 📊 Status & Monitoring")

            with gr.Row():
                with gr.Column():
                    gr.Markdown(
                        "✅ **Frontend Status**: Ready",
                        elem_classes=["status-connected"],
                    )
                    gr.Markdown(
                        "⚠️ **Backend Status**: Not Connected (Expected)",
                        elem_classes=["status-warning"],
                    )
                    gr.Markdown(
                        "🔒 **Authentication**: Demo Mode", elem_classes=["status-info"]
                    )

                with gr.Column():
                    gr.Markdown("### Telemetry Events")
                    gr.Code(
                        """
# Example telemetry tracking
- Page Views: main_app, login_page
- User Actions: login_attempt, send_message  
- API Calls: /chat/message, /projects
- Exceptions: auth_error, api_timeout
- Performance: load_time, response_time
                    """,
                        language="yaml",
                    )

        # Footer
        with gr.Row():
            gr.HTML("""
            <div style="text-align: center; padding: 2rem; color: #605e5c; border-top: 1px solid #e1e5e9; margin-top: 2rem;">
                <p>
                    🔒 Secured with Microsoft Entra ID | 
                    📊 Powered by Azure OpenAI | 
                    🛠️ Built with Gradio & FastAPI
                </p>
                <p>
                    <small>
                        This is a demonstration of the minimal Gradio UI implementation.<br>
                        <strong>Status:</strong> Frontend components implemented and tested ✅
                    </small>
                </p>
            </div>
            """)

    return demo


if __name__ == "__main__":
    demo = create_demo_interface()
    demo.launch(
        server_name="0.0.0.0",
        server_port=7860,
        share=False,
        show_api=False,
        show_error=True,
    )
