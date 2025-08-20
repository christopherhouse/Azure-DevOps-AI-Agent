"""Work item models."""

from datetime import datetime
from enum import Enum

from pydantic import BaseModel, ConfigDict, Field, HttpUrl


class WorkItemType(str, Enum):
    """Work item type enum."""

    USER_STORY = "User Story"
    TASK = "Task"
    BUG = "Bug"
    EPIC = "Epic"
    FEATURE = "Feature"
    TEST_CASE = "Test Case"


class WorkItemState(str, Enum):
    """Work item state enum."""

    NEW = "New"
    ACTIVE = "Active"
    RESOLVED = "Resolved"
    CLOSED = "Closed"
    REMOVED = "Removed"


class WorkItemCreate(BaseModel):
    """Work item creation request model."""

    model_config = ConfigDict(
        json_schema_extra={
            "example": {
                "title": "Implement user login",
                "work_item_type": "User Story",
                "description": "As a user, I want to login to the system",
                "priority": 2,
                "tags": ["authentication", "frontend"],
            }
        }
    )

    title: str = Field(description="Work item title", min_length=1, max_length=255)
    work_item_type: WorkItemType = Field(description="Work item type")
    description: str | None = Field(
        default=None, description="Work item description"
    )
    assigned_to: str | None = Field(default=None, description="Assigned user email")
    priority: int | None = Field(default=2, description="Priority (1-4)", ge=1, le=4)
    tags: list[str] | None = Field(default=None, description="Work item tags")
    parent_id: int | None = Field(default=None, description="Parent work item ID")


class WorkItemUpdate(BaseModel):
    """Work item update request model."""

    model_config = ConfigDict(
        json_schema_extra={
            "example": {"state": "Active", "assigned_to": "user@example.com"}
        }
    )

    title: str | None = Field(
        default=None, description="Work item title", min_length=1, max_length=255
    )
    description: str | None = Field(
        default=None, description="Work item description"
    )
    assigned_to: str | None = Field(default=None, description="Assigned user email")
    state: WorkItemState | None = Field(default=None, description="Work item state")
    priority: int | None = Field(
        default=None, description="Priority (1-4)", ge=1, le=4
    )
    tags: list[str] | None = Field(default=None, description="Work item tags")


class WorkItem(BaseModel):
    """Work item model."""

    model_config = ConfigDict(
        from_attributes=True,
        json_schema_extra={
            "example": {
                "id": 123,
                "title": "Implement user login",
                "work_item_type": "User Story",
                "state": "New",
                "description": "As a user, I want to login to the system",
                "priority": 2,
                "tags": ["authentication", "frontend"],
            }
        },
    )

    id: int = Field(description="Work item ID")
    title: str = Field(description="Work item title")
    work_item_type: str = Field(description="Work item type")
    state: str = Field(description="Work item state")
    description: str | None = Field(
        default=None, description="Work item description"
    )
    assigned_to: str | None = Field(default=None, description="Assigned user")
    created_by: str | None = Field(default=None, description="Created by user")
    created_date: datetime | None = Field(default=None, description="Creation date")
    changed_date: datetime | None = Field(
        default=None, description="Last change date"
    )
    priority: int | None = Field(default=None, description="Priority")
    tags: list[str] | None = Field(default=None, description="Work item tags")
    url: HttpUrl | None = Field(default=None, description="Work item URL")
    project_id: str | None = Field(default=None, description="Project ID")
    parent_id: int | None = Field(default=None, description="Parent work item ID")


class WorkItemList(BaseModel):
    """Work item list response model."""

    model_config = ConfigDict(
        json_schema_extra={
            "example": {
                "work_items": [
                    {
                        "id": 123,
                        "title": "Implement user login",
                        "work_item_type": "User Story",
                        "state": "New",
                        "priority": 2,
                    }
                ],
                "count": 1,
            }
        }
    )

    work_items: list[WorkItem] = Field(description="List of work items")
    count: int = Field(description="Total count of work items")
