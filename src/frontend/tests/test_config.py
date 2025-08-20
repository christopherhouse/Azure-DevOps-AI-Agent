"""Tests for the frontend configuration module."""

import os
from unittest.mock import patch

import pytest

from config import Settings


class TestSettings:
    """Test cases for Settings configuration."""

    def test_default_settings(self):
        """Test default settings values."""
        # Patch required environment variables
        with patch.dict(
            os.environ,
            {"AZURE_TENANT_ID": "test-tenant-id", "AZURE_CLIENT_ID": "test-client-id"},
        ):
            settings = Settings()

            assert settings.environment == "development"
            assert settings.debug is True
            assert settings.frontend_url == "http://localhost:7860"
            assert settings.backend_url == "http://localhost:8000"
            assert settings.azure_tenant_id == "test-tenant-id"
            assert settings.azure_client_id == "test-client-id"

    def test_environment_override(self):
        """Test environment variable overrides."""
        with patch.dict(
            os.environ,
            {
                "AZURE_TENANT_ID": "test-tenant-id",
                "AZURE_CLIENT_ID": "test-client-id",
                "ENVIRONMENT": "production",
                "DEBUG": "false",
                "FRONTEND_URL": "https://myapp.com",
                "BACKEND_URL": "https://api.myapp.com",
            },
        ):
            settings = Settings()

            assert settings.environment == "production"
            assert settings.debug is False
            assert settings.frontend_url == "https://myapp.com"
            assert settings.backend_url == "https://api.myapp.com"

    def test_authority_url_property(self):
        """Test authority URL generation."""
        with patch.dict(
            os.environ,
            {"AZURE_TENANT_ID": "test-tenant-id", "AZURE_CLIENT_ID": "test-client-id"},
        ):
            settings = Settings()
            expected = "https://login.microsoftonline.com/test-tenant-id"
            assert settings.authority_url == expected

    def test_custom_authority_url(self):
        """Test custom authority URL override."""
        with patch.dict(
            os.environ,
            {
                "AZURE_TENANT_ID": "test-tenant-id",
                "AZURE_CLIENT_ID": "test-client-id",
                "AZURE_AUTHORITY": "https://custom.authority.com",
            },
        ):
            settings = Settings()
            assert settings.authority_url == "https://custom.authority.com"

    def test_redirect_uri_property(self):
        """Test redirect URI generation."""
        with patch.dict(
            os.environ,
            {
                "AZURE_TENANT_ID": "test-tenant-id",
                "AZURE_CLIENT_ID": "test-client-id",
                "FRONTEND_URL": "https://myapp.com",
            },
        ):
            settings = Settings()
            expected = "https://myapp.com/auth/callback"
            assert settings.redirect_uri == expected

    def test_scopes_list_property(self):
        """Test scopes list parsing."""
        with patch.dict(
            os.environ,
            {
                "AZURE_TENANT_ID": "test-tenant-id",
                "AZURE_CLIENT_ID": "test-client-id",
                "AZURE_SCOPES": "openid, profile, User.Read",
            },
        ):
            settings = Settings()
            expected = ["openid", "profile", "User.Read"]
            assert settings.scopes_list == expected

    def test_cors_origins_list_property(self):
        """Test CORS origins list parsing."""
        with patch.dict(
            os.environ,
            {
                "AZURE_TENANT_ID": "test-tenant-id",
                "AZURE_CLIENT_ID": "test-client-id",
                "CORS_ORIGINS": "http://localhost:3000, https://myapp.com",
            },
        ):
            settings = Settings()
            expected = ["http://localhost:3000", "https://myapp.com"]
            assert settings.cors_origins_list == expected

    def test_missing_required_env_vars(self):
        """Test that missing required environment variables raise an error."""
        with patch.dict(os.environ, {}, clear=True):
            with pytest.raises((RuntimeError, ValueError)):
                Settings()

    def test_application_insights_config(self):
        """Test Application Insights configuration."""
        with patch.dict(
            os.environ,
            {
                "AZURE_TENANT_ID": "test-tenant-id",
                "AZURE_CLIENT_ID": "test-client-id",
                "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=test-key",
                "ENABLE_TELEMETRY": "true",
            },
        ):
            settings = Settings()

            assert settings.applicationinsights_connection_string == "InstrumentationKey=test-key"
            assert settings.enable_telemetry is True
