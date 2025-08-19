"""Authentication components for Gradio interface."""

import logging
from typing import Optional, Tuple, Dict, Any
from urllib.parse import urlparse, parse_qs

import gradio as gr

from services.auth_service import auth_service, AuthenticationError

logger = logging.getLogger(__name__)


class AuthState:
    """Manages authentication state for the Gradio application."""
    
    def __init__(self):
        self.authenticated = False
        self.user_info: Optional[Dict[str, Any]] = None
        self.access_token: Optional[str] = None
    
    def login(self, access_token: str, user_info: Dict[str, Any]) -> None:
        """Set authenticated state."""
        self.authenticated = True
        self.access_token = access_token
        self.user_info = user_info
        logger.info(f"User authenticated: {user_info.get('email')}")
    
    def logout(self) -> None:
        """Clear authenticated state."""
        if self.user_info:
            auth_service.logout(self.user_info.get("user_id", ""))
        
        self.authenticated = False
        self.access_token = None
        self.user_info = None
        logger.info("User logged out")
    
    def is_authenticated(self) -> bool:
        """Check if user is authenticated."""
        return self.authenticated and self.access_token is not None
    
    def get_user_display_name(self) -> str:
        """Get user display name."""
        if not self.user_info:
            return "Guest"
        
        name = self.user_info.get("name")
        if name:
            return name
        
        email = self.user_info.get("email")
        if email:
            return email.split("@")[0]
        
        return "User"


# Global auth state
auth_state = AuthState()


def create_login_interface() -> gr.Blocks:
    """Create the login interface for unauthenticated users.
    
    Returns:
        Gradio Blocks interface for login
    """
    with gr.Blocks(
        title="Azure DevOps AI Agent - Login",
        theme=gr.themes.Soft(),
        css=".login-container { max-width: 500px; margin: 0 auto; padding: 2rem; }"
    ) as login_interface:
        
        with gr.Column(elem_classes=["login-container"]):
            gr.Markdown("# 🤖 Azure DevOps AI Agent")
            gr.Markdown("### Please authenticate to continue")
            
            gr.Markdown(
                "This application helps you manage Azure DevOps projects, work items, "
                "repositories, and pipelines through natural language conversations."
            )
            
            # Login button
            login_btn = gr.Button(
                "Sign in with Microsoft",
                variant="primary",
                size="lg",
                scale=1
            )
            
            # Status messages
            status_msg = gr.Markdown(visible=False)
            
            # Callback URL input (hidden, used for auth callback)
            callback_url = gr.Textbox(visible=False)
            
            def initiate_login() -> Tuple[str, bool]:
                """Start the OAuth login flow."""
                try:
                    auth_url = auth_service.get_auth_url()
                    status = "Redirecting to Microsoft for authentication..."
                    return status, True
                except AuthenticationError as e:
                    logger.error(f"Login initiation failed: {e}")
                    return f"Login failed: {e}", True
                except Exception as e:
                    logger.error(f"Unexpected login error: {e}")
                    return "Login failed due to technical error. Please try again.", True
            
            def handle_auth_callback(url: str) -> Tuple[bool, str]:
                """Handle the OAuth callback and complete authentication.
                
                Args:
                    url: Callback URL with authorization code
                    
                Returns:
                    Tuple of (success, message)
                """
                try:
                    if not url:
                        return False, "No callback URL provided"
                    
                    # Parse the callback URL
                    parsed_url = urlparse(url)
                    query_params = parse_qs(parsed_url.query)
                    
                    code = query_params.get("code", [None])[0]
                    state = query_params.get("state", [None])[0]
                    error = query_params.get("error", [None])[0]
                    
                    if error:
                        error_desc = query_params.get("error_description", ["Unknown error"])[0]
                        logger.error(f"OAuth error: {error} - {error_desc}")
                        return False, f"Authentication failed: {error_desc}"
                    
                    if not code:
                        return False, "No authorization code received"
                    
                    # Exchange code for token
                    token_result = auth_service.exchange_code_for_token(code, state)
                    access_token = token_result.get("access_token")
                    
                    if not access_token:
                        return False, "Failed to obtain access token"
                    
                    # Get user information
                    user_info = auth_service.get_user_info(access_token)
                    
                    # Update auth state
                    auth_state.login(access_token, user_info)
                    
                    return True, f"Welcome, {auth_state.get_user_display_name()}!"
                    
                except AuthenticationError as e:
                    logger.error(f"Authentication callback failed: {e}")
                    return False, f"Authentication failed: {e}"
                except Exception as e:
                    logger.error(f"Unexpected callback error: {e}")
                    return False, "Authentication failed due to technical error"
            
            # Event handlers
            login_btn.click(
                fn=initiate_login,
                outputs=[status_msg, status_msg.visible]
            )
            
            callback_url.change(
                fn=handle_auth_callback,
                inputs=[callback_url],
                outputs=[status_msg, status_msg]  # This would trigger interface refresh in real app
            )
    
    return login_interface


def create_logout_component() -> gr.Row:
    """Create logout component for authenticated users.
    
    Returns:
        Gradio Row with user info and logout button
    """
    with gr.Row() as logout_row:
        with gr.Column(scale=3):
            user_display = gr.Markdown(
                f"👤 **{auth_state.get_user_display_name()}**"
            )
        
        with gr.Column(scale=1):
            logout_btn = gr.Button(
                "Logout",
                variant="secondary",
                size="sm"
            )
        
        def handle_logout() -> str:
            """Handle user logout."""
            try:
                auth_state.logout()
                return "Logged out successfully. Please refresh the page."
            except Exception as e:
                logger.error(f"Logout error: {e}")
                return "Logout failed. Please try refreshing the page."
        
        logout_btn.click(
            fn=handle_logout,
            outputs=[user_display]
        )
    
    return logout_row


def require_authentication(func):
    """Decorator to require authentication for Gradio functions.
    
    Args:
        func: Function to wrap with authentication check
        
    Returns:
        Wrapped function that checks authentication
    """
    def wrapper(*args, **kwargs):
        if not auth_state.is_authenticated():
            return "Authentication required. Please log in first."
        
        try:
            return func(*args, **kwargs)
        except Exception as e:
            logger.error(f"Function execution error: {e}")
            return f"Error: {e}"
    
    return wrapper