"""FastAPI dependencies."""

from fastapi import Depends
from fastapi_azure_auth import SingleTenantAzureAuthorizationCodeBearer
from fastapi_azure_auth.user import User

from app.core.config import settings

# Azure AD authentication scheme (auto_error=True by default)
azure_scheme = SingleTenantAzureAuthorizationCodeBearer(
    app_client_id=settings.azure_client_id,
    tenant_id=settings.azure_tenant_id,
    scopes={f"{settings.azure_client_id}/Api.All": "Api.All"},
)

# Optional authentication scheme (auto_error=False)
optional_azure_scheme = SingleTenantAzureAuthorizationCodeBearer(
    app_client_id=settings.azure_client_id,
    tenant_id=settings.azure_tenant_id,
    scopes={f"{settings.azure_client_id}/Api.All": "Api.All"},
    auto_error=False,
)


async def get_current_user(user: User = Depends(azure_scheme)) -> User:
    """Get current authenticated user from Azure AD."""
    return user


async def get_current_active_user(current_user: User = Depends(get_current_user)) -> User:
    """Get current active user."""
    # Add any additional checks for user status if needed
    return current_user


async def get_optional_user(user: User | None = Depends(optional_azure_scheme)) -> User | None:
    """Get current user if authenticated, otherwise None."""
    return user


async def get_mock_user() -> User:
    """Get a mock user for when authentication is disabled via feature flag."""
    return User(
        claims={},
        preferred_username="mock-user@example.com",
        roles=["User"],
        aud="mock-client-id",
        tid="mock-tenant-id",
        access_token="mock-access-token",  # nosec B106
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
        ver="2.0",
    )
