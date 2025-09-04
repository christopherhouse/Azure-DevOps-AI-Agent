"""Application configuration management."""

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings."""

    # Application
    app_name: str = "Azure DevOps AI Agent Backend"
    app_version: str = "1.0.0"
    debug: bool = Field(default=False, description="Enable debug mode")
    environment: str = Field(default="development", description="Environment name")

    # Server
    host: str = Field(default="0.0.0.0", description="Server host")  # nosec B104
    port: int = Field(default=8000, description="Server port")

    # Authentication - Azure Entra ID
    azure_tenant_id: str = Field(description="Azure tenant ID")
    azure_client_id: str = Field(description="Azure client ID")
    azure_client_secret: str | None = Field(default=None, description="Azure client secret")

    # Azure DevOps
    azure_devops_organization: str | None = Field(
        default=None, description="Azure DevOps organization"
    )
    azure_devops_pat: str | None = Field(
        default=None, description="Azure DevOps Personal Access Token"
    )

    # OpenTelemetry / Application Insights
    applicationinsights_connection_string: str | None = Field(
        default=None, description="Application Insights connection string"
    )
    otel_service_name: str = Field(
        default="azure-devops-ai-backend", description="OpenTelemetry service name"
    )
    otel_service_version: str = Field(default="1.0.0", description="OpenTelemetry service version")

    # Security
    jwt_secret_key: str = Field(description="JWT secret key")
    jwt_algorithm: str = Field(default="HS256", description="JWT algorithm")
    jwt_expire_minutes: int = Field(default=60, description="JWT expiration time in minutes")

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",  # Ignore extra fields during testing
    )


# Global settings instance
try:
    settings = Settings()  # type: ignore
except Exception as e:
    # Fallback for environments without required config
    # This should only be used in development/testing environments
    # Try to load from test environment file first
    import os

    # Look for .env.test in the repository root (../../../../.env.test from src/backend/app/core/)
    test_env_path = os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".env.test")
    if os.path.exists(test_env_path):
        try:
            settings = Settings(_env_file=test_env_path)  # type: ignore
        except Exception as e2:
            # If test env file doesn't work, provide helpful error
            raise RuntimeError(
                "Required configuration missing. Please set the following environment variables: "
                "AZURE_TENANT_ID, AZURE_CLIENT_ID, JWT_SECRET_KEY. "
                "For testing purposes, copy .env.example to .env or .env.test with appropriate values. "
                f"Original error: {e}"
            ) from e2
    else:
        # In production, all required environment variables must be set
        raise RuntimeError(
            "Required configuration missing. Please set the following environment variables: "
            "AZURE_TENANT_ID, AZURE_CLIENT_ID, JWT_SECRET_KEY. "
            "For testing purposes, create a .env file with these values. "
            f"Original error: {e}"
        ) from e
