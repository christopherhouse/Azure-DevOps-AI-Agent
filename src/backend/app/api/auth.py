"""Authentication endpoints."""

import logging

from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.security import HTTPBearer
from pydantic import BaseModel, Field

from app.core.dependencies import get_current_user
from app.models.auth import Token, User
from app.services.auth_service import auth_service

logger = logging.getLogger(__name__)
router = APIRouter()
security = HTTPBearer()


class LoginRequest(BaseModel):
    """Login request model."""

    azure_token: str = Field(description="Azure Entra ID token")


@router.post("/token", response_model=Token)
async def login(request: LoginRequest):
    """
    Authenticate with Azure Entra ID token and get JWT.

    This endpoint accepts an Azure Entra ID token and returns a JWT
    for subsequent API calls.
    """
    try:
        token = await auth_service.authenticate_user(request.azure_token)
        return token
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Authentication failed: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Authentication service error",
        ) from e


@router.get("/me", response_model=User)
async def get_current_user_info(current_user: User = Depends(get_current_user)):
    """
    Get current user information.

    Returns information about the currently authenticated user.
    """
    return current_user


@router.post("/refresh", response_model=Token)
async def refresh_token(current_user: User = Depends(get_current_user)):
    """
    Refresh JWT token.

    Generate a new JWT token for the current user.
    """
    try:
        user_data = {
            "sub": current_user.id,
            "email": current_user.email,
            "name": current_user.name,
            "preferred_username": current_user.preferred_username,
        }
        token = auth_service.create_access_token(user_data)
        return token
    except Exception as e:
        logger.error(f"Token refresh failed: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Token refresh failed",
        ) from e
