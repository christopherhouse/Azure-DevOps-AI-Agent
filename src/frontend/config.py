"""Configuration management for the frontend application."""

from typing import Optional
from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""

    # Application configuration
    environment: str = Field(default="development", alias="ENVIRONMENT")
    debug: bool = Field(default=True, alias="DEBUG")
    frontend_url: str = Field(default="http://localhost:7860", alias="FRONTEND_URL")
    backend_url: str = Field(default="http://localhost:8000", alias="BACKEND_URL")

    # Microsoft Entra ID configuration
    azure_tenant_id: str = Field(..., alias="AZURE_TENANT_ID")
    azure_client_id: str = Field(..., alias="AZURE_CLIENT_ID")
    azure_client_secret: Optional[str] = Field(
        default=None, alias="AZURE_CLIENT_SECRET"
    )
    azure_authority: Optional[str] = Field(default=None, alias="AZURE_AUTHORITY")
    azure_redirect_uri: Optional[str] = Field(default=None, alias="AZURE_REDIRECT_URI")
    azure_scopes: str = Field(
        default="openid profile User.Read https://app.vssps.visualstudio.com/user_impersonation",
        alias="AZURE_SCOPES",
    )

    # Application Insights configuration
    applicationinsights_connection_string: Optional[str] = Field(
        default=None, alias="APPLICATIONINSIGHTS_CONNECTION_STRING"
    )
    enable_telemetry: bool = Field(default=True, alias="ENABLE_TELEMETRY")

    # Security configuration
    session_timeout: int = Field(default=3600, alias="SESSION_TIMEOUT")
    require_https: bool = Field(default=False, alias="REQUIRE_HTTPS")

    # CORS configuration
    cors_origins: str = Field(
        default="http://localhost:7860,http://127.0.0.1:7860", alias="CORS_ORIGINS"
    )

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",  # Ignore extra fields during testing
    )

    @property
    def authority_url(self) -> str:
        """Get the complete authority URL."""
        if self.azure_authority:
            return self.azure_authority
        return f"https://login.microsoftonline.com/{self.azure_tenant_id}"

    @property
    def redirect_uri(self) -> str:
        """Get the redirect URI for authentication."""
        if self.azure_redirect_uri:
            return self.azure_redirect_uri
        return f"{self.frontend_url}/auth/callback"

    @property
    def scopes_list(self) -> list[str]:
        """Get scopes as a list."""
        return [scope.strip() for scope in self.azure_scopes.split(",")]

    @property
    def cors_origins_list(self) -> list[str]:
        """Get CORS origins as a list."""
        return [origin.strip() for origin in self.cors_origins.split(",")]


# Global settings instance
try:
    settings = Settings()  # type: ignore
except Exception as e:
    # Fallback for environments without required config
    # This should only be used in development/testing environments
    # Try to load from test environment file first
    import os

    # Look for .env.test in the repository root (../../.env.test from src/frontend/)
    test_env_path = os.path.join(os.path.dirname(__file__), "..", "..", ".env.test")
    if os.path.exists(test_env_path):
        try:
            settings = Settings(_env_file=test_env_path)  # type: ignore
        except Exception:
            # If test env file doesn't work, provide helpful error
            raise RuntimeError(
                "Required configuration missing. Please set the following environment variables: "
                "AZURE_TENANT_ID, AZURE_CLIENT_ID. "
                "For testing purposes, copy .env.example to .env or .env.test with appropriate values. "
                f"Original error: {e}"
            )
    else:
        # In production, all required environment variables must be set
        raise RuntimeError(
            "Required configuration missing. Please set the following environment variables: "
            "AZURE_TENANT_ID, AZURE_CLIENT_ID. "
            "For testing purposes, create a .env file with these values. "
            f"Original error: {e}"
        )
