"""Authentication service for Microsoft Entra ID integration."""

import logging
from typing import Any

import jwt
import msal

from config import settings

logger = logging.getLogger(__name__)


class AuthenticationError(Exception):
    """Authentication related errors."""

    pass


class EntraIDAuthService:
    """Microsoft Entra ID authentication service."""

    def __init__(self):
        """Initialize the authentication service."""
        self.client_app = msal.ConfidentialClientApplication(
            client_id=settings.azure_client_id,
            client_credential=settings.azure_client_secret,
            authority=settings.authority_url,
        )
        self._token_cache = {}

    def get_auth_url(self, state: str | None = None) -> str:
        """Generate authentication URL for OAuth flow.

        Args:
            state: Optional state parameter for CSRF protection

        Returns:
            Authentication URL
        """
        try:
            auth_url = self.client_app.get_authorization_request_url(
                scopes=settings.scopes_list,
                state=state or "default-state",
                redirect_uri=settings.redirect_uri,
            )
            logger.info("Generated authentication URL")
            return auth_url
        except Exception as e:
            logger.error(f"Failed to generate auth URL: {e}")
            raise AuthenticationError(f"Failed to generate auth URL: {e}") from e

    def exchange_code_for_token(
        self, code: str, state: str | None = None
    ) -> dict[str, Any]:
        """Exchange authorization code for access token.

        Args:
            code: Authorization code from callback
            state: State parameter for validation

        Returns:
            Token response containing access token and user info
        """
        try:
            result = self.client_app.acquire_token_by_authorization_code(
                code=code,
                scopes=settings.scopes_list,
                redirect_uri=settings.redirect_uri,
            )

            if "access_token" not in result:
                error_desc = result.get("error_description", "Unknown error")
                logger.error(f"Failed to exchange code for token: {error_desc}")
                raise AuthenticationError(f"Token exchange failed: {error_desc}")

            # Store token in cache (in production, use secure storage)
            user_id = self._extract_user_id(result.get("access_token"))
            if user_id:
                self._token_cache[user_id] = result

            logger.info("Successfully exchanged code for token")
            return result

        except Exception as e:
            logger.error(f"Token exchange error: {e}")
            raise AuthenticationError(f"Token exchange failed: {e}") from e

    def refresh_token(self, user_id: str) -> dict[str, Any] | None:
        """Refresh access token using refresh token.

        Args:
            user_id: User identifier for token lookup

        Returns:
            Refreshed token response or None if failed
        """
        try:
            cached_token = self._token_cache.get(user_id)
            if not cached_token or "refresh_token" not in cached_token:
                logger.warning(f"No cached token found for user {user_id}")
                return None

            result = self.client_app.acquire_token_by_refresh_token(
                refresh_token=cached_token["refresh_token"], scopes=settings.scopes_list
            )

            if "access_token" in result:
                self._token_cache[user_id] = result
                logger.info(f"Successfully refreshed token for user {user_id}")
                return result
            else:
                logger.error(
                    f"Failed to refresh token: {result.get('error_description')}"
                )
                return None

        except Exception as e:
            logger.error(f"Token refresh error: {e}")
            return None

    def validate_token(self, access_token: str) -> dict[str, Any]:
        """Validate access token and extract user information.

        Args:
            access_token: JWT access token to validate

        Returns:
            Decoded token payload with user information
        """
        try:
            # Decode token without verification for now (in production, verify signature)
            decoded_token = jwt.decode(
                access_token, options={"verify_signature": False}
            )

            # Basic validation
            if decoded_token.get("aud") != settings.azure_client_id:
                raise AuthenticationError("Invalid token audience")

            logger.info("Token validation successful")
            return decoded_token

        except jwt.ExpiredSignatureError:
            logger.error("Token has expired")
            raise AuthenticationError("Token has expired") from None
        except jwt.InvalidTokenError as e:
            logger.error(f"Invalid token: {e}")
            raise AuthenticationError(f"Invalid token: {e}") from e
        except Exception as e:
            logger.error(f"Token validation error: {e}")
            raise AuthenticationError(f"Token validation failed: {e}") from e

    def get_user_info(self, access_token: str) -> dict[str, Any]:
        """Extract user information from access token.

        Args:
            access_token: Valid access token

        Returns:
            User information dictionary
        """
        try:
            decoded_token = self.validate_token(access_token)

            user_info = {
                "user_id": decoded_token.get("sub"),
                "email": decoded_token.get("email")
                or decoded_token.get("preferred_username"),
                "name": decoded_token.get("name"),
                "given_name": decoded_token.get("given_name"),
                "family_name": decoded_token.get("family_name"),
                "tenant_id": decoded_token.get("tid"),
                "roles": decoded_token.get("roles", []),
                "groups": decoded_token.get("groups", []),
            }

            logger.info(f"Retrieved user info for {user_info.get('email')}")
            return user_info

        except Exception as e:
            logger.error(f"Failed to get user info: {e}")
            raise AuthenticationError(f"Failed to get user info: {e}") from e

    def logout(self, user_id: str) -> None:
        """Logout user and clear cached tokens.

        Args:
            user_id: User identifier
        """
        try:
            if user_id in self._token_cache:
                del self._token_cache[user_id]
            logger.info(f"User {user_id} logged out successfully")
        except Exception as e:
            logger.error(f"Logout error: {e}")

    def _extract_user_id(self, access_token: str) -> str | None:
        """Extract user ID from access token.

        Args:
            access_token: JWT access token

        Returns:
            User ID or None if extraction fails
        """
        try:
            decoded_token = jwt.decode(
                access_token, options={"verify_signature": False}
            )
            return decoded_token.get("sub")
        except Exception:
            return None


# Global authentication service instance
# Note: This is created lazily to avoid issues during testing and import
_auth_service = None


def get_auth_service() -> EntraIDAuthService:
    """Get the global authentication service instance."""
    global _auth_service
    if _auth_service is None:
        _auth_service = EntraIDAuthService()
    return _auth_service
