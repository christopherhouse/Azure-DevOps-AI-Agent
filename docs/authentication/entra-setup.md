# Microsoft Entra ID Setup Guide

This guide provides step-by-step instructions for configuring Microsoft Entra ID (formerly Azure Active Directory) authentication for the Azure DevOps AI Agent.

## Overview

The Azure DevOps AI Agent uses Microsoft Entra ID for secure authentication and authorization. This setup enables:

- **Single Sign-On (SSO)**: Users authenticate with their organization credentials
- **Role-Based Access Control (RBAC)**: Control access to Azure DevOps resources
- **Token Validation**: Secure API access with JWT tokens
- **Multi-Factor Authentication (MFA)**: Enhanced security compliance

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend API    │    │ Microsoft       │
│   (Gradio)      │    │   (FastAPI)      │    │ Entra ID        │
│                 │    │                  │    │                 │
│ 1. User Login   │───►│ 4. Validate Token│◄──►│ 2. Authenticate │
│ 5. Display UI   │◄───│ 6. API Response  │    │ 3. Issue Token  │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Prerequisites

- **Azure Subscription**: With Global Administrator or Application Administrator role
- **Azure DevOps Organization**: Administrative access
- **Domain**: Custom domain for production (optional for development)

## Step 1: Create Entra ID App Registration

### 1.1 Create New App Registration

1. **Navigate to Azure Portal**
   - Go to [Azure Portal](https://portal.azure.com)
   - Search for "Microsoft Entra ID" (or "Azure Active Directory")

2. **Create App Registration**
   ```bash
   # Using Azure CLI
   az ad app create \
     --display-name "Azure DevOps AI Agent" \
     --sign-in-audience "AzureADMyOrg" \
     --web-redirect-uris "http://localhost:7860/auth/callback" "https://your-domain.com/auth/callback"
   ```

   Or via Azure Portal:
   - Go to **App registrations** → **New registration**
   - **Name**: Azure DevOps AI Agent
   - **Supported account types**: Accounts in this organizational directory only
   - **Redirect URI**: Web → `http://localhost:7860/auth/callback`

### 1.2 Configure Authentication

1. **Add Redirect URIs**
   ```json
   {
     "web": {
       "redirectUris": [
         "http://localhost:7860/auth/callback",
         "https://your-domain.com/auth/callback",
         "https://your-app.azurecontainerapps.io/auth/callback"
       ],
       "implicitGrantSettings": {
         "enableAccessTokenIssuance": false,
         "enableIdTokenIssuance": true
       }
     },
     "spa": {
       "redirectUris": [
         "http://localhost:7860",
         "https://your-domain.com",
         "https://your-app.azurecontainerapps.io"
       ]
     }
   }
   ```

2. **Configure Token Configuration**
   - **Access tokens**: Enabled
   - **ID tokens**: Enabled
   - **Allow public client flows**: No

### 1.3 Create Client Secret

```bash
# Using Azure CLI
az ad app credential reset \
  --id <app-id> \
  --display-name "Azure DevOps AI Agent Secret" \
  --years 2
```

Or via Azure Portal:
- Go to **Certificates & secrets** → **New client secret**
- **Description**: Azure DevOps AI Agent Secret
- **Expires**: 24 months (recommended)
- **Copy the secret value** (shown only once)

## Step 2: Configure API Permissions

### 2.1 Microsoft Graph Permissions

Add the following Microsoft Graph permissions:

```bash
# Using Azure CLI - Add Microsoft Graph permissions
az ad app permission add \
  --id <app-id> \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions \
    e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope \
    37f7f235-527c-4136-accd-4a02d197296e=Scope \
    14dad69e-099b-42c9-810b-d002981feec1=Scope
```

**Required permissions:**
- `User.Read` (Delegated): Read user profile
- `openid` (Delegated): Sign in and read user profile  
- `profile` (Delegated): View users' basic profile
- `email` (Delegated): View users' email address

### 2.2 Azure DevOps Permissions

Add Azure DevOps permissions:

```bash
# Azure DevOps API permissions
az ad app permission add \
  --id <app-id> \
  --api 499b84ac-1321-427f-aa17-267ca6975798 \
  --api-permissions \
    ee69721e-6c3a-468f-a9ec-302d16a4c599=Scope
```

**Required permissions:**
- `user_impersonation` (Delegated): Access Azure DevOps Services

### 2.3 Grant Admin Consent

```bash
# Grant admin consent for all permissions
az ad app permission admin-consent --id <app-id>
```

Or via Azure Portal:
- Go to **API permissions** → **Grant admin consent for [Organization]**

## Step 3: Configure Token Claims

### 3.1 Optional Claims Configuration

```bash
# Configure optional claims
az ad app update \
  --id <app-id> \
  --optional-claims @optional-claims.json
```

**optional-claims.json:**
```json
{
  "idToken": [
    {
      "name": "email",
      "source": null,
      "essential": false
    },
    {
      "name": "given_name",
      "source": null,
      "essential": false
    },
    {
      "name": "family_name",
      "source": null,
      "essential": false
    },
    {
      "name": "upn",
      "source": null,
      "essential": false
    }
  ],
  "accessToken": [
    {
      "name": "email",
      "source": null,
      "essential": false
    },
    {
      "name": "groups",
      "source": null,
      "essential": false
    }
  ]
}
```

### 3.2 Group Claims (Optional)

If using group-based access control:

1. **Enable Group Claims**
   - Go to **Token configuration** → **Add groups claim**
   - Select **Security groups** and **Groups assigned to the application**
   - Include group claims in **ID tokens** and **Access tokens**

## Step 4: Environment Configuration

### 4.1 Application Environment Variables

```bash
# Entra ID Configuration
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret

# Authentication URLs
AZURE_AUTHORITY=https://login.microsoftonline.com/your-tenant-id
AZURE_REDIRECT_URI=http://localhost:7860/auth/callback

# Scopes
AZURE_SCOPES=openid profile User.Read https://app.vssps.visualstudio.com/user_impersonation

# Application URLs
FRONTEND_URL=http://localhost:7860
BACKEND_URL=http://localhost:8000
```

### 4.2 Production Configuration

For production deployment:

```bash
# Production Entra ID Configuration
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret  # Store in Key Vault

# Production URLs
AZURE_REDIRECT_URI=https://your-domain.com/auth/callback
FRONTEND_URL=https://your-domain.com
BACKEND_URL=https://api.your-domain.com

# Security Settings
REQUIRE_HTTPS=true
SESSION_COOKIE_SECURE=true
SESSION_COOKIE_SAMESITE=Lax
```

## Step 5: Application Integration

### 5.1 Frontend Authentication (Gradio)

```python
# frontend/auth.py
import msal
import streamlit as st
from typing import Optional, Dict, Any

class EntraIDAuth:
    def __init__(self, client_id: str, client_secret: str, authority: str):
        self.client_id = client_id
        self.client_secret = client_secret
        self.authority = authority
        self.app = msal.ConfidentialClientApplication(
            client_id=client_id,
            client_credential=client_secret,
            authority=authority
        )
    
    def get_auth_url(self, scopes: list[str], redirect_uri: str) -> str:
        """Generate authentication URL."""
        auth_url = self.app.get_authorization_request_url(
            scopes=scopes,
            redirect_uri=redirect_uri,
            state="random-state-string"  # In production, use secure random
        )
        return auth_url
    
    def get_token_from_code(self, code: str, scopes: list[str], redirect_uri: str) -> Optional[Dict[str, Any]]:
        """Exchange authorization code for tokens."""
        result = self.app.acquire_token_by_authorization_code(
            code=code,
            scopes=scopes,
            redirect_uri=redirect_uri
        )
        return result if "access_token" in result else None
```

### 5.2 Backend Token Validation (FastAPI)

```python
# backend/auth.py
from fastapi import HTTPException, Depends, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
import jwt
from jwt import PyJWKClient
import os
from typing import Optional, Dict, Any

security = HTTPBearer()

class EntraIDValidator:
    def __init__(self, tenant_id: str, client_id: str):
        self.tenant_id = tenant_id
        self.client_id = client_id
        self.issuer = f"https://login.microsoftonline.com/{tenant_id}/v2.0"
        self.jwks_uri = f"https://login.microsoftonline.com/{tenant_id}/discovery/v2.0/keys"
        self.jwks_client = PyJWKClient(self.jwks_uri)
    
    async def validate_token(self, token: str) -> Dict[str, Any]:
        """Validate JWT token from Entra ID."""
        try:
            # Get signing key
            signing_key = self.jwks_client.get_signing_key_from_jwt(token)
            
            # Decode and validate token
            payload = jwt.decode(
                token,
                signing_key.key,
                algorithms=["RS256"],
                audience=self.client_id,
                issuer=self.issuer
            )
            
            return payload
            
        except jwt.ExpiredSignatureError:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Token has expired"
            )
        except jwt.InvalidTokenError:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Invalid token"
            )

# Dependency for protected routes
async def get_current_user(
    credentials: HTTPAuthorizationCredentials = Depends(security)
) -> Dict[str, Any]:
    validator = EntraIDValidator(
        tenant_id=os.getenv("AZURE_TENANT_ID"),
        client_id=os.getenv("AZURE_CLIENT_ID")
    )
    return await validator.validate_token(credentials.credentials)
```

## Step 6: Testing Authentication

### 6.1 Test Authentication Flow

```python
# tests/test_auth.py
import pytest
from unittest.mock import Mock, patch
from backend.auth import EntraIDValidator

@pytest.mark.asyncio
async def test_valid_token_validation():
    """Test validation of valid Entra ID token."""
    validator = EntraIDValidator("test-tenant", "test-client")
    
    # Mock JWT validation
    with patch("jwt.decode") as mock_decode:
        mock_decode.return_value = {
            "sub": "user-id",
            "email": "user@example.com",
            "name": "Test User"
        }
        
        result = await validator.validate_token("valid-token")
        assert result["email"] == "user@example.com"

@pytest.mark.asyncio
async def test_expired_token_rejection():
    """Test rejection of expired token."""
    validator = EntraIDValidator("test-tenant", "test-client")
    
    with patch("jwt.decode") as mock_decode:
        mock_decode.side_effect = jwt.ExpiredSignatureError()
        
        with pytest.raises(HTTPException) as exc:
            await validator.validate_token("expired-token")
        
        assert exc.value.status_code == 401
        assert "expired" in exc.value.detail.lower()
```

### 6.2 Manual Testing Checklist

1. **Authentication Flow**
   - [ ] User can access login page
   - [ ] Redirect to Entra ID works
   - [ ] User can authenticate successfully
   - [ ] Callback handling works correctly
   - [ ] Tokens are received and stored

2. **Authorization**
   - [ ] Protected API endpoints require authentication
   - [ ] Invalid tokens are rejected
   - [ ] Expired tokens are handled gracefully
   - [ ] User information is extracted correctly

3. **Security**
   - [ ] HTTPS is enforced in production
   - [ ] Cookies are secure and HTTP-only
   - [ ] State parameter prevents CSRF
   - [ ] Logout clears all session data

## Step 7: Production Considerations

### 7.1 Security Best Practices

1. **Certificate Validation**
   - Always validate SSL certificates
   - Use certificate pinning for added security

2. **Token Storage**
   - Store tokens securely (encrypted cookies/session storage)
   - Implement token refresh logic
   - Clear tokens on logout

3. **Session Management**
   - Implement secure session handling
   - Use secure, HTTP-only cookies
   - Implement session timeout

### 7.2 Monitoring and Logging

```python
# Add authentication monitoring
import logging
from opencensus.ext.azure.log_exporter import AzureLogHandler

# Configure logging
logger = logging.getLogger(__name__)
logger.addHandler(AzureLogHandler(
    connection_string=os.getenv("APPLICATIONINSIGHTS_CONNECTION_STRING")
))

async def validate_token(self, token: str) -> Dict[str, Any]:
    try:
        payload = jwt.decode(...)
        logger.info(f"Successful authentication for user: {payload.get('email')}")
        return payload
    except jwt.ExpiredSignatureError:
        logger.warning("Authentication failed: Token expired")
        raise HTTPException(...)
```

### 7.3 High Availability

1. **Token Caching**
   - Cache JWKS keys with appropriate TTL
   - Implement fallback authentication methods

2. **Error Handling**
   - Graceful degradation for auth service outages
   - Clear error messages for users

## Troubleshooting

### Common Issues

1. **"AADSTS50011: The reply URL specified in the request does not match"**
   - Verify redirect URIs in app registration
   - Check for trailing slashes or HTTP vs HTTPS

2. **"AADSTS70001: Application not found"**
   - Verify client ID is correct
   - Ensure app registration exists in correct tenant

3. **"Invalid audience"**
   - Check audience claim in token validation
   - Verify client ID matches expected audience

4. **"Token signature verification failed"**
   - Ensure correct JWKS endpoint
   - Check for clock skew issues

### Debug Token Issues

```python
# Decode token without verification for debugging
import jwt
import json

def debug_token(token: str):
    """Debug JWT token without verification."""
    header = jwt.get_unverified_header(token)
    payload = jwt.decode(token, options={"verify_signature": False})
    
    print("Header:", json.dumps(header, indent=2))
    print("Payload:", json.dumps(payload, indent=2))
    print("Audience:", payload.get("aud"))
    print("Issuer:", payload.get("iss"))
    print("Expiry:", payload.get("exp"))
```

## Resources

- [Microsoft Entra ID Documentation](https://docs.microsoft.com/en-us/azure/active-directory/)
- [MSAL Python Documentation](https://msal-python.readthedocs.io/)
- [Azure DevOps OAuth Documentation](https://docs.microsoft.com/en-us/azure/devops/integrate/get-started/authentication/oauth)
- [JWT Token Validation](https://jwt.io/)
- [OpenID Connect Specification](https://openid.net/connect/)