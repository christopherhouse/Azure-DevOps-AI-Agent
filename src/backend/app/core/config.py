"""Application configuration management."""

from typing import Optional
from pydantic import Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Application settings."""

    # Application
    app_name: str = "Azure DevOps AI Agent Backend"
    app_version: str = "1.0.0"
    debug: bool = Field(default=False, description="Enable debug mode")
    environment: str = Field(default="development", description="Environment name")

    # Server
    host: str = Field(default="0.0.0.0", description="Server host")
    port: int = Field(default=8000, description="Server port")

    # Authentication - Azure Entra ID
    azure_tenant_id: str = Field(description="Azure tenant ID")
    azure_client_id: str = Field(description="Azure client ID")
    azure_client_secret: Optional[str] = Field(
        default=None, description="Azure client secret"
    )

    # Azure DevOps
    azure_devops_organization: Optional[str] = Field(
        default=None, description="Azure DevOps organization"
    )
    azure_devops_pat: Optional[str] = Field(
        default=None, description="Azure DevOps Personal Access Token"
    )

    # OpenTelemetry / Application Insights
    applicationinsights_connection_string: Optional[str] = Field(
        default=None, description="Application Insights connection string"
    )
    otel_service_name: str = Field(
        default="azure-devops-ai-backend", description="OpenTelemetry service name"
    )
    otel_service_version: str = Field(
        default="1.0.0", description="OpenTelemetry service version"
    )

    # CORS
    allowed_origins: list[str] = Field(
        default=["http://localhost:3000", "http://localhost:7860"],
        description="Allowed CORS origins",
    )

    # Security
    jwt_secret_key: str = Field(description="JWT secret key")
    jwt_algorithm: str = Field(default="HS256", description="JWT algorithm")
    jwt_expire_minutes: int = Field(
        default=60, description="JWT expiration time in minutes"
    )

    class Config:
        """Pydantic configuration."""

        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = False


# Global settings instance
try:
    settings = Settings()  # type: ignore
except Exception:
    # Fallback for environments without required config
    settings = Settings(
        azure_tenant_id="mock-tenant",
        azure_client_id="mock-client",
        jwt_secret_key="mock-secret-key-32-chars-long",
    )
