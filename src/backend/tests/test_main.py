"""Comprehensive tests for backend API."""


def test_health_check(client):
    """Test health check endpoint."""
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"
    assert "version" in data
    assert "environment" in data


def test_root_endpoint(client):
    """Test root endpoint."""
    response = client.get("/")
    assert response.status_code == 200
    data = response.json()
    assert "message" in data
    assert "version" in data


def test_authentication_endpoints_removed(unauthenticated_client):
    """Test that the old authentication endpoints are removed."""
    # Test that /api/auth/token no longer exists
    response = unauthenticated_client.post("/api/auth/token", json={"azure_token": "mock-token"})
    assert response.status_code == 404
    
    # Test that /api/auth/me no longer exists
    response = unauthenticated_client.get("/api/auth/me", headers={"Authorization": "Bearer mock-token"})
    assert response.status_code == 404


def test_authentication_required_endpoints(unauthenticated_client):
    """Test that protected endpoints require authentication."""
    # Test chat endpoint without auth header should fail
    response = unauthenticated_client.post("/api/chat/message", json={"message": "test"})
    assert response.status_code == 401  # Unauthorized


def test_chat_message(client, auth_headers):
    """Test chat message endpoint."""
    response = client.post(
        "/api/chat/message",
        json={
            "message": "Create a new project called Test Project",
            "context": {"organization": "test-org"},
        },
        headers=auth_headers,
    )
    assert response.status_code == 200
    data = response.json()
    assert "message" in data
    assert "conversation_id" in data
    assert "suggestions" in data
    assert data["metadata"]["user_id"] == "mock-user-123"


def test_chat_conversations(client, auth_headers):
    """Test get conversations endpoint."""
    response = client.get("/api/chat/conversations", headers=auth_headers)
    assert response.status_code == 200
    assert response.json() == []


def test_projects_list(client, auth_headers):
    """Test projects list endpoint."""
    response = client.get("/api/projects", headers=auth_headers)
    assert response.status_code == 200
    data = response.json()
    assert "projects" in data
    assert "count" in data
    assert data["projects"] == []
    assert data["count"] == 0


def test_projects_create(client, auth_headers):
    """Test project creation endpoint."""
    response = client.post(
        "/api/projects",
        json={
            "name": "Test Project",
            "description": "A test project",
            "visibility": "private",
        },
        headers=auth_headers,
    )
    assert response.status_code == 201
    data = response.json()
    assert data["name"] == "Test Project"
    assert data["description"] == "A test project"
    assert data["visibility"] == "private"
    assert data["state"] == "created"


def test_projects_validation(client, auth_headers):
    """Test project validation."""
    # Test missing required fields
    response = client.post("/api/projects", json={}, headers=auth_headers)
    assert response.status_code == 422

    # Test invalid data
    response = client.post(
        "/api/projects",
        json={
            "name": "",  # Empty name should fail
            "description": "A test project",
        },
        headers=auth_headers,
    )
    assert response.status_code == 422


def test_workitems_list(client, auth_headers):
    """Test work items list endpoint."""
    response = client.get("/api/test-project/workitems", headers=auth_headers)
    assert response.status_code == 200
    data = response.json()
    assert "work_items" in data
    assert "count" in data
    assert data["work_items"] == []
    assert data["count"] == 0


def test_workitems_create(client, auth_headers):
    """Test work item creation endpoint."""
    response = client.post(
        "/api/test-project/workitems",
        json={
            "title": "Test Work Item",
            "work_item_type": "User Story",
            "description": "A test work item",
            "priority": 2,
        },
        headers=auth_headers,
    )
    assert response.status_code == 201
    data = response.json()
    assert data["title"] == "Test Work Item"
    assert data["work_item_type"] == "User Story"
    assert data["description"] == "A test work item"
    assert data["priority"] == 2
    assert data["state"] == "New"


def test_cors_headers_not_set_by_backend(client):
    """Test that CORS headers are NOT set by the backend (handled by Azure Container Apps)."""
    # Test with a simple GET request
    response = client.get("/health")
    assert response.status_code == 200
    
    # Verify that common CORS headers are NOT set by the backend
    # Azure Container Apps will handle CORS instead
    assert "Access-Control-Allow-Origin" not in response.headers
    assert "Access-Control-Allow-Methods" not in response.headers
    assert "Access-Control-Allow-Headers" not in response.headers
    assert "Access-Control-Allow-Credentials" not in response.headers


def test_security_headers(client):
    """Test security headers are present."""
    response = client.get("/health")
    assert response.status_code == 200

    # Check security headers
    assert response.headers.get("X-Content-Type-Options") == "nosniff"
    assert response.headers.get("X-Frame-Options") == "DENY"
    assert response.headers.get("X-XSS-Protection") == "1; mode=block"
    assert "X-Process-Time" in response.headers


def test_error_handling(client, unauthenticated_client):
    """Test error handling."""
    # Test 404
    response = unauthenticated_client.get("/nonexistent")
    assert response.status_code == 404

    # Test validation error with chat endpoint
    response = client.post("/api/chat/message", json={}, headers={"Authorization": "Bearer mock-token"})
    assert response.status_code == 422
    data = response.json()
    assert "error" in data
    assert data["error"]["type"] == "validation_error"
