"""Tests for the authentication service."""

import pytest
from unittest.mock import Mock, patch, MagicMock

from services.auth_service import EntraIDAuthService, AuthenticationError


class TestEntraIDAuthService:
    """Test cases for EntraIDAuthService."""
    
    @pytest.fixture
    def auth_service(self):
        """Create an auth service instance for testing."""
        with patch('services.auth_service.settings') as mock_settings:
            mock_settings.azure_client_id = "test-client-id"
            mock_settings.azure_client_secret = "test-client-secret"
            mock_settings.authority_url = "https://login.microsoftonline.com/test-tenant"
            mock_settings.scopes_list = ["openid", "profile"]
            mock_settings.redirect_uri = "http://localhost:7860/auth/callback"
            
            with patch('services.auth_service.msal.ConfidentialClientApplication'):
                service = EntraIDAuthService()
                return service
    
    def test_initialization(self, auth_service):
        """Test service initialization."""
        assert auth_service is not None
        assert hasattr(auth_service, 'client_app')
        assert hasattr(auth_service, '_token_cache')
    
    def test_get_auth_url_success(self, auth_service):
        """Test successful auth URL generation."""
        mock_url = "https://login.microsoftonline.com/test-tenant/oauth2/v2.0/authorize?..."
        auth_service.client_app.get_authorization_request_url = Mock(return_value=mock_url)
        
        result = auth_service.get_auth_url("test-state")
        
        assert result == mock_url
        auth_service.client_app.get_authorization_request_url.assert_called_once()
    
    def test_get_auth_url_failure(self, auth_service):
        """Test auth URL generation failure."""
        auth_service.client_app.get_authorization_request_url = Mock(
            side_effect=Exception("MSAL error")
        )
        
        with pytest.raises(AuthenticationError) as exc_info:
            auth_service.get_auth_url()
        
        assert "Failed to generate auth URL" in str(exc_info.value)
    
    def test_exchange_code_for_token_success(self, auth_service):
        """Test successful token exchange."""
        mock_token_result = {
            "access_token": "test-access-token",
            "refresh_token": "test-refresh-token",
            "id_token": "test-id-token"
        }
        
        auth_service.client_app.acquire_token_by_authorization_code = Mock(
            return_value=mock_token_result
        )
        
        with patch.object(auth_service, '_extract_user_id', return_value="test-user-id"):
            result = auth_service.exchange_code_for_token("test-code", "test-state")
        
        assert result == mock_token_result
        assert "test-user-id" in auth_service._token_cache
    
    def test_exchange_code_for_token_failure(self, auth_service):
        """Test token exchange failure."""
        mock_error_result = {
            "error": "invalid_grant",
            "error_description": "Invalid authorization code"
        }
        
        auth_service.client_app.acquire_token_by_authorization_code = Mock(
            return_value=mock_error_result
        )
        
        with pytest.raises(AuthenticationError) as exc_info:
            auth_service.exchange_code_for_token("invalid-code")
        
        assert "Token exchange failed" in str(exc_info.value)
    
    @patch('services.auth_service.jwt.decode')
    def test_validate_token_success(self, mock_jwt_decode, auth_service):
        """Test successful token validation."""
        mock_payload = {
            "sub": "test-user-id",
            "aud": "test-client-id",
            "exp": 9999999999,  # Future expiration
            "email": "test@example.com"
        }
        
        mock_jwt_decode.return_value = mock_payload
        
        with patch('services.auth_service.settings.azure_client_id', "test-client-id"):
            result = auth_service.validate_token("test-token")
        
        assert result == mock_payload
    
    @patch('services.auth_service.jwt.decode')
    def test_validate_token_invalid_audience(self, mock_jwt_decode, auth_service):
        """Test token validation with invalid audience."""
        mock_payload = {
            "sub": "test-user-id",
            "aud": "wrong-client-id",
            "exp": 9999999999
        }
        
        mock_jwt_decode.return_value = mock_payload
        
        with patch('services.auth_service.settings.azure_client_id', "test-client-id"):
            with pytest.raises(AuthenticationError) as exc_info:
                auth_service.validate_token("test-token")
        
        assert "Invalid token audience" in str(exc_info.value)
    
    def test_get_user_info_success(self, auth_service):
        """Test successful user info extraction."""
        mock_token_payload = {
            "sub": "test-user-id",
            "email": "test@example.com",
            "name": "Test User",
            "given_name": "Test",
            "family_name": "User",
            "tid": "test-tenant-id",
            "roles": ["user"],
            "groups": ["group1"]
        }
        
        with patch.object(auth_service, 'validate_token', return_value=mock_token_payload):
            result = auth_service.get_user_info("test-token")
        
        assert result["user_id"] == "test-user-id"
        assert result["email"] == "test@example.com"
        assert result["name"] == "Test User"
        assert result["roles"] == ["user"]
        assert result["groups"] == ["group1"]
    
    def test_refresh_token_success(self, auth_service):
        """Test successful token refresh."""
        # Set up cached token
        auth_service._token_cache["test-user"] = {
            "refresh_token": "test-refresh-token"
        }
        
        new_token_result = {
            "access_token": "new-access-token",
            "refresh_token": "new-refresh-token"
        }
        
        auth_service.client_app.acquire_token_by_refresh_token = Mock(
            return_value=new_token_result
        )
        
        result = auth_service.refresh_token("test-user")
        
        assert result == new_token_result
        assert auth_service._token_cache["test-user"] == new_token_result
    
    def test_refresh_token_no_cached_token(self, auth_service):
        """Test token refresh with no cached token."""
        result = auth_service.refresh_token("nonexistent-user")
        assert result is None
    
    def test_logout(self, auth_service):
        """Test user logout."""
        # Set up cached token
        auth_service._token_cache["test-user"] = {"access_token": "token"}
        
        auth_service.logout("test-user")
        
        assert "test-user" not in auth_service._token_cache
    
    @patch('services.auth_service.jwt.decode')
    def test_extract_user_id_success(self, mock_jwt_decode, auth_service):
        """Test successful user ID extraction."""
        mock_payload = {"sub": "test-user-id"}
        mock_jwt_decode.return_value = mock_payload
        
        result = auth_service._extract_user_id("test-token")
        
        assert result == "test-user-id"
    
    @patch('services.auth_service.jwt.decode')
    def test_extract_user_id_failure(self, mock_jwt_decode, auth_service):
        """Test user ID extraction failure."""
        mock_jwt_decode.side_effect = Exception("Invalid token")
        
        result = auth_service._extract_user_id("invalid-token")
        
        assert result is None