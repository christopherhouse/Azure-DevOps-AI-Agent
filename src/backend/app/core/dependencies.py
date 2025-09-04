"""FastAPI dependencies."""

from fastapi import Depends
from fastapi_azure_auth import SingleTenantAzureAuthorizationCodeBearer
from fastapi_azure_auth.user import User

from app.core.config import settings

# Azure AD authentication scheme (auto_error=True by default)
azure_scheme = SingleTenantAzureAuthorizationCodeBearer(
    app_client_id=settings.azure_client_id,
    tenant_id=settings.azure_tenant_id,
    scopes={f"api://{settings.azure_client_id}/user_impersonation": "user_impersonation"},
)

# Optional authentication scheme (auto_error=False)
optional_azure_scheme = SingleTenantAzureAuthorizationCodeBearer(
    app_client_id=settings.azure_client_id,
    tenant_id=settings.azure_tenant_id,
    scopes={f"api://{settings.azure_client_id}/user_impersonation": "user_impersonation"},
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
