"""Chat interface components for the Gradio application."""

import logging
from typing import List, Tuple, Optional
import asyncio
from datetime import datetime

import gradio as gr

from services.api_client import BackendAPIClient, APIError
from components.auth import auth_state, require_authentication

logger = logging.getLogger(__name__)


class ChatMessage:
    """Represents a chat message."""

    def __init__(self, role: str, content: str, timestamp: Optional[datetime] = None):
        self.role = role  # 'user' or 'assistant'
        self.content = content
        self.timestamp = timestamp or datetime.now()

    def to_gradio_format(self) -> Tuple[str, str]:
        """Convert to Gradio chat format.

        Returns:
            Tuple of (user_message, bot_response) or (None, bot_response)
        """
        if self.role == "user":
            return (self.content, None)
        else:
            return (None, self.content)


class ChatSession:
    """Manages a chat session with message history."""

    def __init__(self):
        self.messages: List[ChatMessage] = []
        self.conversation_id: Optional[str] = None
        self.api_client: Optional[BackendAPIClient] = None

    def initialize_api_client(self) -> None:
        """Initialize API client with current auth token."""
        if auth_state.is_authenticated():
            self.api_client = BackendAPIClient(auth_state.access_token)
        else:
            self.api_client = None

    def add_message(self, role: str, content: str) -> None:
        """Add a message to the session."""
        message = ChatMessage(role, content)
        self.messages.append(message)
        logger.info(f"Added {role} message to chat session")

    def get_gradio_history(self) -> List[Tuple[str, str]]:
        """Get message history in Gradio chat format.

        Returns:
            List of (user_message, bot_response) tuples
        """
        history = []
        current_pair = [None, None]  # [user_msg, bot_msg]

        for message in self.messages:
            if message.role == "user":
                if current_pair[0] is not None:
                    # Previous user message without bot response
                    history.append((current_pair[0], ""))
                current_pair[0] = message.content
                current_pair[1] = None
            else:  # assistant
                current_pair[1] = message.content
                history.append((current_pair[0], current_pair[1]))
                current_pair = [None, None]

        # Handle case where last message is from user
        if current_pair[0] is not None and current_pair[1] is None:
            history.append((current_pair[0], ""))

        return history

    async def send_message(self, user_message: str) -> str:
        """Send a message and get AI response.

        Args:
            user_message: User's input message

        Returns:
            AI assistant's response
        """
        if not self.api_client:
            return "❌ Error: Not authenticated. Please log in first."

        try:
            # Add user message to history
            self.add_message("user", user_message)

            # Send to backend API
            response = await self.api_client.send_chat_message(
                message=user_message,
                conversation_id=self.conversation_id,
                context={
                    "user_id": auth_state.user_info.get("user_id")
                    if auth_state.user_info
                    else None
                },
            )

            # Extract response data
            response_data = response.get("data", {})
            ai_response = response_data.get(
                "response", "Sorry, I couldn't process your request."
            )
            self.conversation_id = response_data.get("conversation_id")

            # Add AI response to history
            self.add_message("assistant", ai_response)

            logger.info("Successfully processed chat message")
            return ai_response

        except APIError as e:
            error_msg = f"❌ API Error: {e}"
            self.add_message("assistant", error_msg)
            logger.error(f"API error in chat: {e}")
            return error_msg
        except Exception as e:
            error_msg = f"❌ Unexpected error: {e}"
            self.add_message("assistant", error_msg)
            logger.error(f"Unexpected error in chat: {e}")
            return error_msg

    def clear_history(self) -> None:
        """Clear chat history."""
        self.messages.clear()
        self.conversation_id = None
        logger.info("Chat history cleared")


# Global chat session
chat_session = ChatSession()


def create_chat_interface() -> gr.Blocks:
    """Create the main chat interface for authenticated users.

    Returns:
        Gradio Blocks interface for chat
    """
    with gr.Blocks(
        title="Azure DevOps AI Agent - Chat",
        theme=gr.themes.Soft(),
        css="""
        .chat-container { max-width: 1200px; margin: 0 auto; }
        .chat-header { background: linear-gradient(90deg, #0078d4, #106ebe); color: white; padding: 1rem; border-radius: 8px; margin-bottom: 1rem; }
        .example-prompts { background: #f8f9fa; padding: 1rem; border-radius: 8px; margin: 1rem 0; }
        .status-info { background: #e7f3ff; padding: 0.5rem; border-radius: 4px; margin: 0.5rem 0; }
        """,
    ) as chat_interface:
        with gr.Column(elem_classes=["chat-container"]):
            # Header section
            with gr.Row(elem_classes=["chat-header"]):
                gr.Markdown("# 🤖 Azure DevOps AI Agent")
                gr.Markdown(
                    "Ask me anything about your Azure DevOps projects, work items, repositories, and pipelines!"
                )

            # Status section
            with gr.Row():
                status_display = gr.Markdown(
                    "✅ **Connected** - Ready to help with Azure DevOps tasks",
                    elem_classes=["status-info"],
                )

            # Main chat interface
            with gr.Row():
                with gr.Column(scale=4):
                    # Chat history
                    chatbot = gr.Chatbot(
                        value=[],
                        label="Conversation",
                        height=500,
                        show_label=True,
                        show_copy_button=True,
                        bubble_full_width=False,
                    )

                    # Message input
                    with gr.Row():
                        msg_input = gr.Textbox(
                            placeholder="Ask me about Azure DevOps... (e.g., 'Create a new project called MyApp')",
                            label="Your message",
                            lines=2,
                            scale=4,
                        )
                        send_btn = gr.Button("Send", variant="primary", scale=1)

                    # Action buttons
                    with gr.Row():
                        clear_btn = gr.Button(
                            "Clear Chat", variant="secondary", size="sm"
                        )
                        refresh_btn = gr.Button(
                            "Refresh Status", variant="secondary", size="sm"
                        )

                with gr.Column(scale=1):
                    # Example prompts
                    with gr.Group():
                        gr.Markdown("### 💡 Example Prompts")
                        example_btns = []

                        examples = [
                            "List all my Azure DevOps projects",
                            "Create a new project called 'WebApp'",
                            "Show me open work items in project X",
                            "Create a user story for login feature",
                            "List repositories in my organization",
                            "Create a new Git repository",
                            "Show me recent pipeline runs",
                            "Help me set up a build pipeline",
                        ]

                        for example in examples:
                            btn = gr.Button(
                                example, variant="secondary", size="sm", scale=1
                            )
                            example_btns.append(btn)

            # Initialize chat session
            def initialize_chat() -> Tuple[str, List]:
                """Initialize chat session with authentication."""
                try:
                    chat_session.initialize_api_client()
                    if chat_session.api_client:
                        return "✅ **Ready** - Chat initialized successfully", []
                    else:
                        return "❌ **Error** - Authentication required", []
                except Exception as e:
                    logger.error(f"Chat initialization error: {e}")
                    return f"❌ **Error** - {e}", []

            @require_authentication
            def process_message(message: str, history: List) -> Tuple[str, List, str]:
                """Process user message and update chat.

                Args:
                    message: User input message
                    history: Current chat history

                Returns:
                    Tuple of (empty_input, updated_history, status)
                """
                if not message.strip():
                    return message, history, "Please enter a message"

                try:
                    # Run async function in event loop
                    loop = asyncio.new_event_loop()
                    asyncio.set_event_loop(loop)

                    try:
                        ai_response = loop.run_until_complete(
                            chat_session.send_message(message.strip())
                        )
                    finally:
                        loop.close()

                    # Update history
                    new_history = history + [(message, ai_response)]

                    return "", new_history, "✅ Message sent successfully"

                except Exception as e:
                    logger.error(f"Message processing error: {e}")
                    error_response = f"❌ Error processing message: {e}"
                    new_history = history + [(message, error_response)]
                    return "", new_history, "❌ Error occurred"

            def clear_chat() -> Tuple[List, str]:
                """Clear chat history."""
                chat_session.clear_history()
                return [], "✅ Chat cleared"

            def set_example_message(example: str) -> str:
                """Set example message in input field."""
                return example

            async def check_backend_status() -> str:
                """Check backend API status."""
                try:
                    if not chat_session.api_client:
                        chat_session.initialize_api_client()

                    if chat_session.api_client:
                        health = await chat_session.api_client.health_check()
                        if health.get("status") == "healthy":
                            return "✅ **Connected** - Backend API is healthy"
                        else:
                            return "⚠️ **Warning** - Backend API issues detected"
                    else:
                        return "❌ **Error** - Not authenticated"
                except Exception as e:
                    return f"❌ **Error** - Backend unavailable: {e}"

            def refresh_status() -> str:
                """Refresh connection status."""
                try:
                    loop = asyncio.new_event_loop()
                    asyncio.set_event_loop(loop)

                    try:
                        return loop.run_until_complete(check_backend_status())
                    finally:
                        loop.close()
                except Exception as e:
                    return f"❌ **Error** - Status check failed: {e}"

            # Event handlers
            chat_interface.load(fn=initialize_chat, outputs=[status_display, chatbot])

            send_btn.click(
                fn=process_message,
                inputs=[msg_input, chatbot],
                outputs=[msg_input, chatbot, status_display],
            )

            msg_input.submit(
                fn=process_message,
                inputs=[msg_input, chatbot],
                outputs=[msg_input, chatbot, status_display],
            )

            clear_btn.click(fn=clear_chat, outputs=[chatbot, status_display])

            refresh_btn.click(fn=refresh_status, outputs=[status_display])

            # Example button handlers
            for i, btn in enumerate(example_btns):
                btn.click(
                    fn=lambda ex=examples[i]: set_example_message(ex),
                    outputs=[msg_input],
                )

    return chat_interface
