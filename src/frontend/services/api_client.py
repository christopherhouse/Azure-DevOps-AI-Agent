"""API client for backend communication."""

import logging
from typing import Any

import httpx
from httpx import AsyncClient, HTTPStatusError

from config import settings

logger = logging.getLogger(__name__)


class APIError(Exception):
    """API communication errors."""

    pass


class BackendAPIClient:
    """Client for communicating with the FastAPI backend."""

    def __init__(self, access_token: str | None = None):
        """Initialize the API client.

        Args:
            access_token: Bearer token for authentication
        """
        self.base_url = settings.backend_url.rstrip("/")
        self.access_token = access_token
        self.timeout = httpx.Timeout(30.0)

    def _get_headers(self) -> dict[str, str]:
        """Get HTTP headers for API requests.

        Returns:
            Headers dictionary with authentication and content type
        """
        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json",
            "User-Agent": "Azure-DevOps-AI-Agent-Frontend/1.0",
        }

        if self.access_token:
            headers["Authorization"] = f"Bearer {self.access_token}"

        return headers

    async def send_chat_message(
        self,
        message: str,
        conversation_id: str | None = None,
        context: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        """Send a chat message to the AI agent.

        Args:
            message: User message text
            conversation_id: Optional conversation identifier
            context: Optional context information

        Returns:
            AI response with message and metadata
        """
        try:
            payload = {
                "message": message,
                "conversation_id": conversation_id,
                "context": context or {},
            }

            async with AsyncClient(timeout=self.timeout) as client:
                response = await client.post(
                    f"{self.base_url}/chat/message",
                    json=payload,
                    headers=self._get_headers(),
                )
                response.raise_for_status()

                result = response.json()
                logger.info(
                    f"Chat message sent successfully, got response: {len(result.get('data', {}).get('response', ''))} chars"
                )
                return result

        except HTTPStatusError as e:
            logger.error(
                f"HTTP error sending chat message: {e.response.status_code} - {e.response.text}"
            )
            raise APIError(f"Chat API error: {e.response.status_code}") from e
        except Exception as e:
            logger.error(f"Error sending chat message: {e}")
            raise APIError(f"Failed to send chat message: {e}") from e

    async def get_conversations(self, limit: int = 20) -> list[dict[str, Any]]:
        """Get user's conversation history.

        Args:
            limit: Maximum number of conversations to retrieve

        Returns:
            List of conversation objects
        """
        try:
            params = {"limit": limit}

            async with AsyncClient(timeout=self.timeout) as client:
                response = await client.get(
                    f"{self.base_url}/chat/conversations",
                    params=params,
                    headers=self._get_headers(),
                )
                response.raise_for_status()

                result = response.json()
                conversations = result.get("data", [])
                logger.info(f"Retrieved {len(conversations)} conversations")
                return conversations

        except HTTPStatusError as e:
            logger.error(f"HTTP error getting conversations: {e.response.status_code}")
            raise APIError(f"Conversations API error: {e.response.status_code}") from e
        except Exception as e:
            logger.error(f"Error getting conversations: {e}")
            raise APIError(f"Failed to get conversations: {e}") from e

    async def get_conversation(self, conversation_id: str) -> dict[str, Any]:
        """Get a specific conversation with its message history.

        Args:
            conversation_id: Conversation identifier

        Returns:
            Conversation object with messages
        """
        try:
            async with AsyncClient(timeout=self.timeout) as client:
                response = await client.get(
                    f"{self.base_url}/chat/conversations/{conversation_id}",
                    headers=self._get_headers(),
                )
                response.raise_for_status()

                result = response.json()
                logger.info(f"Retrieved conversation {conversation_id}")
                return result.get("data", {})

        except HTTPStatusError as e:
            logger.error(f"HTTP error getting conversation: {e.response.status_code}")
            raise APIError(f"Conversation API error: {e.response.status_code}") from e
        except Exception as e:
            logger.error(f"Error getting conversation: {e}")
            raise APIError(f"Failed to get conversation: {e}") from e

    async def health_check(self) -> dict[str, Any]:
        """Check backend API health status.

        Returns:
            Health status information
        """
        try:
            async with AsyncClient(timeout=httpx.Timeout(5.0)) as client:
                response = await client.get(
                    f"{self.base_url}/health", headers={"Accept": "application/json"}
                )
                response.raise_for_status()

                result = response.json()
                logger.info("Backend health check successful")
                return result

        except HTTPStatusError as e:
            logger.error(f"Backend health check failed: {e.response.status_code}")
            raise APIError(f"Backend health check failed: {e.response.status_code}") from e
        except Exception as e:
            logger.error(f"Backend health check error: {e}")
            raise APIError(f"Backend health check failed: {e}") from e

    async def get_user_profile(self) -> dict[str, Any]:
        """Get authenticated user's profile information.

        Returns:
            User profile data
        """
        try:
            async with AsyncClient(timeout=self.timeout) as client:
                response = await client.get(
                    f"{self.base_url}/auth/profile", headers=self._get_headers()
                )
                response.raise_for_status()

                result = response.json()
                logger.info("Retrieved user profile")
                return result.get("data", {})

        except HTTPStatusError as e:
            logger.error(f"HTTP error getting user profile: {e.response.status_code}")
            raise APIError(f"Profile API error: {e.response.status_code}") from e
        except Exception as e:
            logger.error(f"Error getting user profile: {e}")
            raise APIError(f"Failed to get user profile: {e}") from e

    def set_access_token(self, access_token: str) -> None:
        """Update the access token for API requests.

        Args:
            access_token: New bearer token
        """
        self.access_token = access_token
        logger.info("Access token updated")

    def clear_access_token(self) -> None:
        """Clear the access token."""
        self.access_token = None
        logger.info("Access token cleared")
