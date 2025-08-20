"""Azure DevOps project models."""

from datetime import datetime
from enum import Enum
from typing import Any

from pydantic import BaseModel, ConfigDict, Field, HttpUrl


class ProjectVisibility(str, Enum):
    """Project visibility enum."""

    PRIVATE = "private"
    PUBLIC = "public"


class ProjectState(str, Enum):
    """Project state enum."""

    CREATING = "creating"
    CREATED = "created"
    DELETING = "deleting"
    DELETED = "deleted"


class ProjectCreate(BaseModel):
    """Project creation request model."""

    model_config = ConfigDict(
        json_schema_extra={
            "example": {
                "name": "My Project",
                "description": "A sample project for development",
                "visibility": "private",
                "source_control_type": "Git",
            }
        }
    )

    name: str = Field(description="Project name", min_length=1, max_length=64)
    description: str | None = Field(
        default=None, description="Project description", max_length=255
    )
    visibility: ProjectVisibility = Field(
        default=ProjectVisibility.PRIVATE, description="Project visibility"
    )
    source_control_type: str = Field(default="Git", description="Source control type")
    template_type_id: str | None = Field(
        default=None, description="Process template ID"
    )


class ProjectUpdate(BaseModel):
    """Project update request model."""

    model_config = ConfigDict(
        json_schema_extra={"example": {"description": "Updated project description"}}
    )

    name: str | None = Field(
        default=None, description="Project name", min_length=1, max_length=64
    )
    description: str | None = Field(
        default=None, description="Project description", max_length=255
    )


class Project(BaseModel):
    """Project model."""

    model_config = ConfigDict(
        from_attributes=True,
        json_schema_extra={
            "example": {
                "id": "project-123",
                "name": "My Project",
                "description": "A sample project",
                "state": "created",
                "visibility": "private",
                "revision": 1,
            }
        },
    )

    id: str = Field(description="Project ID")
    name: str = Field(description="Project name")
    description: str | None = Field(default=None, description="Project description")
    url: HttpUrl | None = Field(default=None, description="Project URL")
    state: ProjectState = Field(description="Project state")
    visibility: ProjectVisibility = Field(description="Project visibility")
    revision: int | None = Field(default=None, description="Project revision")
    last_update_time: datetime | None = Field(
        default=None, description="Last update time"
    )
    capabilities: dict[str, Any] | None = Field(
        default=None, description="Project capabilities"
    )


class ProjectList(BaseModel):
    """Project list response model."""

    model_config = ConfigDict(
        json_schema_extra={
            "example": {
                "projects": [
                    {
                        "id": "project-123",
                        "name": "My Project",
                        "description": "A sample project",
                        "state": "created",
                        "visibility": "private",
                    }
                ],
                "count": 1,
            }
        }
    )

    projects: list[Project] = Field(description="List of projects")
    count: int = Field(description="Total count of projects")
