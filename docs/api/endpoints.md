# API Documentation

This document provides comprehensive documentation for the Azure DevOps AI Agent API endpoints.

## Base URL

- **Development**: `http://localhost:8000`
- **Production**: `https://api.azure-devops-agent.your-domain.com`

## Authentication

All API endpoints require authentication via Bearer token obtained from Microsoft Entra ID.

```http
Authorization: Bearer <your-jwt-token>
```

## Common Response Format

All API responses follow a consistent format:

```json
{
  "success": true,
  "data": {},
  "message": "Operation completed successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

Error responses:

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input parameters",
    "details": {}
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Health Check

### GET /health

Check API health status.

**Response:**
```json
{
  "success": true,
  "data": {
    "status": "healthy",
    "version": "1.0.0",
    "timestamp": "2024-01-15T10:30:00Z",
    "dependencies": {
      "azure_devops": "healthy",
      "azure_openai": "healthy",
      "database": "healthy"
    }
  }
}
```

## Authentication Endpoints

### POST /auth/token

Validate and exchange authentication token.

**Request Body:**
```json
{
  "token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6..."
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "user": {
      "id": "user-id",
      "email": "user@example.com",
      "name": "John Doe",
      "roles": ["user"]
    },
    "permissions": ["read:projects", "write:workitems"]
  }
}
```

### POST /auth/refresh

Refresh authentication token.

**Request Body:**
```json
{
  "refresh_token": "refresh-token-here"
}
```

## Chat/AI Endpoints

### POST /chat/message

Send a message to the AI agent for processing.

**Request Body:**
```json
{
  "message": "Create a new project called 'My Project'",
  "conversation_id": "optional-conversation-id",
  "context": {
    "organization": "your-org",
    "project": "optional-current-project"
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "response": "I'll help you create a new project called 'My Project'. Let me do that for you now.",
    "conversation_id": "conv-123",
    "actions_performed": [
      {
        "type": "create_project",
        "status": "completed",
        "result": {
          "project_id": "proj-456",
          "name": "My Project",
          "url": "https://dev.azure.com/your-org/My%20Project"
        }
      }
    ],
    "suggestions": [
      "Would you like to create some initial work items?",
      "Should I set up a repository for this project?"
    ]
  }
}
```

### GET /chat/conversations

Get user's conversation history.

**Query Parameters:**
- `limit` (optional): Number of conversations to return (default: 20)
- `offset` (optional): Offset for pagination (default: 0)

**Response:**
```json
{
  "success": true,
  "data": {
    "conversations": [
      {
        "id": "conv-123",
        "title": "Project Creation",
        "created_at": "2024-01-15T10:30:00Z",
        "updated_at": "2024-01-15T10:35:00Z",
        "message_count": 5
      }
    ],
    "total": 1,
    "has_more": false
  }
}
```

### GET /chat/conversations/{conversation_id}

Get specific conversation details.

**Response:**
```json
{
  "success": true,
  "data": {
    "conversation": {
      "id": "conv-123",
      "title": "Project Creation",
      "messages": [
        {
          "id": "msg-1",
          "type": "user",
          "content": "Create a new project called 'My Project'",
          "timestamp": "2024-01-15T10:30:00Z"
        },
        {
          "id": "msg-2",
          "type": "assistant",
          "content": "I'll help you create a new project...",
          "timestamp": "2024-01-15T10:30:15Z",
          "actions": [...]
        }
      ]
    }
  }
}
```

## Project Management Endpoints

### GET /projects

List Azure DevOps projects.

**Query Parameters:**
- `organization` (required): Azure DevOps organization name
- `skip` (optional): Number of projects to skip (default: 0)
- `top` (optional): Number of projects to return (default: 100)

**Response:**
```json
{
  "success": true,
  "data": {
    "projects": [
      {
        "id": "proj-123",
        "name": "Sample Project",
        "description": "A sample project",
        "url": "https://dev.azure.com/org/Sample%20Project",
        "state": "wellFormed",
        "visibility": "private"
      }
    ],
    "count": 1
  }
}
```

### POST /projects

Create a new Azure DevOps project.

**Request Body:**
```json
{
  "organization": "your-org",
  "name": "New Project",
  "description": "Project description",
  "capabilities": {
    "versioncontrol": {
      "sourceControlType": "Git"
    },
    "processTemplate": {
      "templateTypeId": "6b724908-ef14-45cf-84f8-768b5384da45"
    }
  },
  "visibility": "private"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "project": {
      "id": "proj-456",
      "name": "New Project",
      "url": "https://dev.azure.com/your-org/New%20Project",
      "state": "createPending"
    },
    "operation": {
      "id": "op-789",
      "status": "inProgress",
      "url": "https://dev.azure.com/your-org/_apis/operations/op-789"
    }
  }
}
```

### GET /projects/{project_id}

Get specific project details.

**Response:**
```json
{
  "success": true,
  "data": {
    "project": {
      "id": "proj-123",
      "name": "Sample Project",
      "description": "A sample project",
      "url": "https://dev.azure.com/org/Sample%20Project",
      "state": "wellFormed",
      "visibility": "private",
      "capabilities": {},
      "defaultTeam": {
        "id": "team-123",
        "name": "Sample Project Team",
        "url": "https://dev.azure.com/org/_apis/projects/proj-123/teams/team-123"
      }
    }
  }
}
```

## Work Item Management Endpoints

### GET /projects/{project_id}/workitems

List work items in a project.

**Query Parameters:**
- `organization` (required): Azure DevOps organization name
- `wiql` (optional): Work Item Query Language query
- `fields` (optional): Comma-separated list of fields to return
- `skip` (optional): Number of items to skip
- `top` (optional): Number of items to return (max 200)

**Response:**
```json
{
  "success": true,
  "data": {
    "workItems": [
      {
        "id": 1,
        "rev": 2,
        "fields": {
          "System.Id": 1,
          "System.Title": "Sample Task",
          "System.WorkItemType": "Task",
          "System.State": "New",
          "System.AssignedTo": {
            "displayName": "John Doe",
            "uniqueName": "john@example.com"
          }
        },
        "url": "https://dev.azure.com/org/proj/_apis/wit/workItems/1"
      }
    ],
    "count": 1
  }
}
```

### POST /projects/{project_id}/workitems

Create a new work item.

**Request Body:**
```json
{
  "organization": "your-org",
  "type": "Task",
  "fields": {
    "System.Title": "New Task",
    "System.Description": "Task description",
    "System.AssignedTo": "user@example.com",
    "Microsoft.VSTS.Common.Priority": 2
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "workItem": {
      "id": 123,
      "rev": 1,
      "fields": {
        "System.Id": 123,
        "System.Title": "New Task",
        "System.WorkItemType": "Task",
        "System.State": "New"
      },
      "url": "https://dev.azure.com/org/proj/_apis/wit/workItems/123"
    }
  }
}
```

### PATCH /workitems/{work_item_id}

Update an existing work item.

**Request Body:**
```json
{
  "organization": "your-org",
  "fields": {
    "System.State": "Active",
    "System.AssignedTo": "newuser@example.com"
  }
}
```

### DELETE /workitems/{work_item_id}

Delete a work item.

**Query Parameters:**
- `organization` (required): Azure DevOps organization name
- `destroy` (optional): Permanently delete (true) or move to recycle bin (false, default)

## Repository Management Endpoints

### GET /projects/{project_id}/repositories

List repositories in a project.

**Response:**
```json
{
  "success": true,
  "data": {
    "repositories": [
      {
        "id": "repo-123",
        "name": "sample-repo",
        "url": "https://dev.azure.com/org/proj/_apis/git/repositories/repo-123",
        "project": {
          "id": "proj-123",
          "name": "Sample Project"
        },
        "defaultBranch": "refs/heads/main",
        "size": 12345
      }
    ],
    "count": 1
  }
}
```

### POST /projects/{project_id}/repositories

Create a new repository.

**Request Body:**
```json
{
  "organization": "your-org",
  "name": "new-repo",
  "project": {
    "id": "proj-123"
  }
}
```

## Pipeline Management Endpoints

### GET /projects/{project_id}/pipelines

List pipelines in a project.

**Response:**
```json
{
  "success": true,
  "data": {
    "pipelines": [
      {
        "id": 1,
        "name": "CI Pipeline",
        "folder": "\\",
        "revision": 5,
        "url": "https://dev.azure.com/org/proj/_apis/pipelines/1"
      }
    ],
    "count": 1
  }
}
```

### POST /projects/{project_id}/pipelines/{pipeline_id}/runs

Trigger a pipeline run.

**Request Body:**
```json
{
  "organization": "your-org",
  "resources": {
    "repositories": {
      "self": {
        "refName": "refs/heads/main"
      }
    }
  },
  "variables": {
    "env": {
      "value": "production"
    }
  }
}
```

## Error Codes

| Code | Description |
|------|-------------|
| `AUTHENTICATION_FAILED` | Invalid or expired token |
| `AUTHORIZATION_FAILED` | Insufficient permissions |
| `VALIDATION_ERROR` | Invalid request parameters |
| `AZURE_DEVOPS_ERROR` | Azure DevOps API error |
| `AZURE_OPENAI_ERROR` | Azure OpenAI service error |
| `INTERNAL_ERROR` | Unexpected server error |
| `RATE_LIMIT_EXCEEDED` | Too many requests |
| `SERVICE_UNAVAILABLE` | Service temporarily unavailable |

## Rate Limiting

API endpoints are rate limited per user:

- **Chat endpoints**: 30 requests per minute
- **Read operations**: 100 requests per minute  
- **Write operations**: 20 requests per minute

Rate limit headers are included in responses:

```http
X-RateLimit-Limit: 30
X-RateLimit-Remaining: 25
X-RateLimit-Reset: 1642248600
```

## Webhooks (Future)

Support for webhooks to receive real-time updates about Azure DevOps events.

### POST /webhooks/azure-devops

Configure webhook for Azure DevOps events.

**Request Body:**
```json
{
  "organization": "your-org",
  "project": "proj-123",
  "events": [
    "workitem.created",
    "workitem.updated",
    "git.push"
  ],
  "url": "https://your-app.com/webhooks/azure-devops",
  "secret": "webhook-secret"
}
```

## SDKs and Examples

### Python Example

```python
import httpx
import asyncio

class AzureDevOpsAIClient:
    def __init__(self, base_url: str, token: str):
        self.base_url = base_url
        self.headers = {
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json"
        }
    
    async def send_message(self, message: str, organization: str):
        async with httpx.AsyncClient() as client:
            response = await client.post(
                f"{self.base_url}/chat/message",
                json={
                    "message": message,
                    "context": {"organization": organization}
                },
                headers=self.headers
            )
            return response.json()

# Usage
async def main():
    client = AzureDevOpsAIClient(
        "https://api.azure-devops-agent.com",
        "your-jwt-token"
    )
    
    result = await client.send_message(
        "Create a new project called 'My Project'",
        "your-org"
    )
    print(result)

asyncio.run(main())
```

### JavaScript Example

```javascript
class AzureDevOpsAIClient {
    constructor(baseUrl, token) {
        this.baseUrl = baseUrl;
        this.headers = {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        };
    }
    
    async sendMessage(message, organization) {
        const response = await fetch(`${this.baseUrl}/chat/message`, {
            method: 'POST',
            headers: this.headers,
            body: JSON.stringify({
                message: message,
                context: { organization: organization }
            })
        });
        
        return await response.json();
    }
}

// Usage
const client = new AzureDevOpsAIClient(
    'https://api.azure-devops-agent.com',
    'your-jwt-token'
);

client.sendMessage('Create a new project called "My Project"', 'your-org')
    .then(result => console.log(result));
```

## Testing

API endpoints can be tested using the interactive Swagger documentation available at `/docs` endpoint.

For automated testing, use the provided Postman collection or OpenAPI specification file.