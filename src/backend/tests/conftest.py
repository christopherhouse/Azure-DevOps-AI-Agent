"""Test configuration and fixtures."""

import os

import pytest
from fastapi.testclient import TestClient

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


@pytest.fixture
def client():
    """Test client fixture."""
    return TestClient(app)


@pytest.fixture
def auth_token(client):
    """Get authentication token for tests."""
    response = client.post("/api/auth/token", json={"azure_token": "mock-token"})
    assert response.status_code == 200
    return response.json()["access_token"]


@pytest.fixture
def auth_headers(auth_token):
    """Get authentication headers for tests."""
    return {"Authorization": f"Bearer {auth_token}"}
