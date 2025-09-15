"""Test configuration and fixtures."""

import os

import pytest
from fastapi import Request
from fastapi.testclient import TestClient
from fastapi_azure_auth.user import User

# Set up test environment variables before importing the app
os.environ["AZURE_TENANT_ID"] = "mock-tenant-id"
os.environ["AZURE_CLIENT_ID"] = "mock-client-id"
os.environ["AZURE_CLIENT_SECRET"] = "mock-client-secret"
os.environ["JWT_SECRET_KEY"] = "mock-jwt-secret-key-32-chars-long-minimum"
os.environ["JWT_ALGORITHM"] = "HS256"
os.environ["JWT_EXPIRE_MINUTES"] = "60"
os.environ["ENVIRONMENT"] = "test"
os.environ["DEBUG"] = "true"

# Import app after setting environment variables
from app.main import app
from app.core.dependencies import azure_scheme


@pytest.fixture
def client():
    """Test client fixture with mocked Azure authentication."""
    
    async def mock_azure_auth(request: Request) -> User:
        """Mock Azure AD authentication for testing."""
        user = User(
            claims={},
            preferred_username="testuser@example.com",
            roles=["User"],
            aud="mock-client-id",
            tid="mock-tenant-id",
            access_token="mock-access-token",
            is_guest=False,
            iat=1537231048,
            nbf=1537231048,
            exp=1537234948,
            iss="https://login.microsoftonline.com/mock-tenant-id/v2.0",
            aio="mock-aio",
            sub="mock-user-123",
            oid="mock-user-123",
            uti="mock-uti",
            rh="mock-rh",
            ver="2.0"
        )
        request.state.user = user
        return user
    
    # Override the azure_scheme dependency with our mock
    app.dependency_overrides[azure_scheme] = mock_azure_auth
    
    client = TestClient(app)
    yield client
    
    # Clean up the override after test
    app.dependency_overrides.clear()


@pytest.fixture
def unauthenticated_client():
    """Test client fixture without authentication."""
    client = TestClient(app)
    yield client


@pytest.fixture
def auth_headers():
    """Get authentication headers for tests."""
    # Since we're mocking the auth, we can use any Bearer token
    return {"Authorization": "Bearer mock-token"}
