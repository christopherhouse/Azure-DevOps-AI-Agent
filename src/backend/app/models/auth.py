"""Authentication models."""

from typing import Optional
from pydantic import BaseModel, Field, ConfigDict


class User(BaseModel):
    """User model."""

    model_config = ConfigDict(from_attributes=True)

    id: str = Field(description="User ID from Azure Entra ID")
    email: str = Field(description="User email")
    name: str = Field(description="User display name")
    preferred_username: Optional[str] = Field(
        default=None, description="Preferred username"
    )


class Token(BaseModel):
    """Token response model."""

    access_token: str = Field(description="JWT access token")
    token_type: str = Field(default="bearer", description="Token type")
    expires_in: int = Field(description="Token expiration time in seconds")


class TokenData(BaseModel):
    """Token data model."""

    user_id: Optional[str] = Field(default=None, description="User ID")
    scopes: list[str] = Field(default_factory=list, description="Token scopes")
