"""Authentication service."""

import logging
from datetime import UTC, datetime, timedelta
from typing import Any

import jwt
from fastapi import HTTPException, status
from jwt.exceptions import DecodeError, InvalidTokenError
from msal import ConfidentialClientApplication

from app.core.config import settings
from app.models.auth import Token, User

logger = logging.getLogger(__name__)


class AuthenticationService:
    """Authentication service for Azure Entra ID integration."""

    def __init__(self):
        """Initialize authentication service."""
        self.tenant_id = settings.azure_tenant_id
        self.client_id = settings.azure_client_id
        self.client_secret = settings.azure_client_secret
        self.jwt_secret = settings.jwt_secret_key
        self.jwt_algorithm = settings.jwt_algorithm
        self.jwt_expire_minutes = settings.jwt_expire_minutes

        # MSAL client will be initialized lazily
        self._msal_app = None

    @property
    def msal_app(self) -> ConfidentialClientApplication | None:
        """Get MSAL app instance, initialized lazily."""
        if self._msal_app is None and self.client_secret:
            try:
                # Only initialize if we have a real tenant ID (not mock)
                if self.tenant_id and not self.tenant_id.startswith("mock"):
                    self._msal_app = ConfidentialClientApplication(
                        client_id=self.client_id,
                        client_credential=self.client_secret,
                        authority=f"https://login.microsoftonline.com/{self.tenant_id}",
                    )
                else:
                    logger.warning(
                        "Mock tenant ID detected - MSAL authentication disabled"
                    )
            except Exception as e:
                logger.error(f"Failed to initialize MSAL: {e}")
        return self._msal_app

    async def validate_azure_token(self, token: str) -> dict[str, Any] | None:
        """Validate Azure Entra ID token."""
        try:
            # In a real implementation, you would validate the token against Azure
            # For now, we'll create a mock validation for development
            # TODO: Implement proper Azure token validation

            if self.tenant_id.startswith("mock"):
                # Mock validation for development
                return {
                    "sub": "mock-user-123",
                    "email": "mock@example.com",
                    "name": "Mock User",
                    "preferred_username": "mockuser",
                }

            # Decode without verification for development
            unverified_payload = jwt.decode(token, options={"verify_signature": False})

            # Extract user information
            user_info = {
                "sub": unverified_payload.get("sub"),
                "email": unverified_payload.get(
                    "email", unverified_payload.get("unique_name")
                ),
                "name": unverified_payload.get("name"),
                "preferred_username": unverified_payload.get("preferred_username"),
            }

            return user_info

        except Exception as e:
            logger.error(f"Token validation failed: {e}")
            return None

    def create_access_token(self, user_data: dict[str, Any]) -> Token:
        """Create a JWT access token."""
        expire = datetime.now(UTC) + timedelta(minutes=self.jwt_expire_minutes)

        to_encode = {
            "sub": user_data.get("sub"),
            "email": user_data.get("email"),
            "name": user_data.get("name"),
            "exp": expire,
            "iat": datetime.now(UTC),
            "iss": "azure-devops-ai-backend",
        }

        encoded_jwt = jwt.encode(
            to_encode, self.jwt_secret, algorithm=self.jwt_algorithm
        )

        return Token(
            access_token=encoded_jwt,
            token_type="bearer",  # nosec B106
            expires_in=self.jwt_expire_minutes * 60,
        )

    async def verify_token(self, token: str) -> User | None:
        """Verify JWT token and return user."""
        try:
            payload = jwt.decode(
                token, self.jwt_secret, algorithms=[self.jwt_algorithm]
            )

            user_id = payload.get("sub")
            if user_id is None:
                return None

            return User(
                id=user_id,
                email=payload.get("email", ""),
                name=payload.get("name", ""),
                preferred_username=payload.get("preferred_username"),
            )

        except (DecodeError, InvalidTokenError, ValueError, Exception) as e:
            logger.error(f"JWT verification failed: {e}")
            return None

    async def get_user_from_azure_token(self, azure_token: str) -> User | None:
        """Get user information from Azure token."""
        user_info = await self.validate_azure_token(azure_token)
        if not user_info:
            return None

        return User(
            id=user_info.get("sub", ""),
            email=user_info.get("email", ""),
            name=user_info.get("name", ""),
            preferred_username=user_info.get("preferred_username"),
        )

    async def authenticate_user(self, azure_token: str) -> Token | None:
        """Authenticate user with Azure token and return JWT."""
        user_info = await self.validate_azure_token(azure_token)
        if not user_info:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Invalid authentication token",
            )

        # Create internal JWT token
        token = self.create_access_token(user_info)
        return token


# Global authentication service instance
auth_service = AuthenticationService()
