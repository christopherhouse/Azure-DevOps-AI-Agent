"""Chat and AI models."""

from datetime import datetime
from typing import Any

from pydantic import BaseModel, ConfigDict, Field


class ChatMessage(BaseModel):
    """Chat message model."""

    id: str | None = Field(default=None, description="Message ID")
    role: str = Field(description="Message role (user, assistant, system)")
    content: str = Field(description="Message content")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="Message timestamp")
    metadata: dict[str, Any] | None = Field(default=None, description="Additional metadata")


class ChatRequest(BaseModel):
    """Chat request model."""

    model_config = ConfigDict(
        json_schema_extra={
            "example": {
                "message": "Create a new project called 'My Project'",
                "context": {"organization": "myorg"},
            }
        }
    )

    message: str = Field(description="User message")
    conversation_id: str | None = Field(default=None, description="Conversation ID")
    context: dict[str, Any] | None = Field(default=None, description="Context information")


class ChatResponse(BaseModel):
    """Chat response model."""

    model_config = ConfigDict(
        json_schema_extra={
            "example": {
                "message": "I'll create a new project called 'My Project' for you.",
                "conversation_id": "conv-123",
                "suggestions": [
                    "Create a repository for this project",
                    "Set up a basic sprint plan",
                ],
            }
        }
    )

    message: str = Field(description="AI response message")
    conversation_id: str = Field(description="Conversation ID")
    suggestions: list[str] | None = Field(default=None, description="Suggested follow-up actions")
    metadata: dict[str, Any] | None = Field(default=None, description="Response metadata")


class Conversation(BaseModel):
    """Conversation model."""

    model_config = ConfigDict(from_attributes=True)

    id: str = Field(description="Conversation ID")
    user_id: str = Field(description="User ID")
    title: str | None = Field(default=None, description="Conversation title")
    created_at: datetime = Field(default_factory=datetime.utcnow, description="Creation timestamp")
    updated_at: datetime = Field(
        default_factory=datetime.utcnow, description="Last update timestamp"
    )
    messages: list[ChatMessage] = Field(default_factory=list, description="Conversation messages")
