# Project Plugin Documentation

The Project Plugin is a Semantic Kernel plugin that enables AI-powered project creation in Azure DevOps organizations.

## Overview

The ProjectPlugin provides three main functions that can be called by the AI assistant:

1. **get_process_templates** - Retrieves available process templates for an organization
2. **create_project** - Creates a new project using specified parameters
3. **find_process_template** - Finds a process template by name or partial match

## Functions

### get_process_templates

Retrieves the list of available process templates for an Azure DevOps organization.

**Parameters:**
- `organization` (string): The Azure DevOps organization name
- `personalAccessToken` (string): Personal Access Token for authentication

**Returns:**
A formatted string listing all available process templates with their IDs, names, descriptions, and types.

**Example Usage:**
```
User: "What process templates are available for my organization?"
AI: "I'll get the available process templates for your organization. Please provide your organization name and Personal Access Token."
```

### create_project

Creates a new project in an Azure DevOps organization.

**Parameters:**
- `organization` (string): The Azure DevOps organization name
- `projectName` (string): The name of the project to create
- `description` (string, optional): Description of the project
- `processTemplateId` (string): The process template type ID (get from get_process_templates)
- `personalAccessToken` (string): Personal Access Token for authentication
- `visibility` (string, optional): Project visibility ("Private" or "Public", defaults to "Private")

**Returns:**
A success message with project details or an error message if creation fails.

**Example Usage:**
```
User: "Create a new project called 'MyApp' using the Agile template"
AI: "I'll help you create a new project. First, let me get the available process templates to find the Agile template ID..."
```

### find_process_template

Finds a process template by name or partial name match.

**Parameters:**
- `organization` (string): The Azure DevOps organization name
- `templateName` (string): The name or partial name of the process template
- `personalAccessToken` (string): Personal Access Token for authentication

**Returns:**
The template ID if found, or a list of matching templates, or available templates if no match.

## API Integration

The plugin integrates with the following Azure DevOps REST APIs:

- **GET** `https://dev.azure.com/{organization}/_apis/work/processes?api-version=7.1` - Get process templates
- **POST** `https://dev.azure.com/{organization}/_apis/projects?api-version=7.1` - Create project

## Authentication

All functions require a Personal Access Token (PAT) with appropriate permissions:
- **Project and Team (read, write, & manage)** - Required for project creation
- **Work Items (read)** - Required for reading process templates

## Error Handling

The plugin handles various error scenarios:
- Invalid or missing authentication tokens
- Network connectivity issues
- Azure DevOps API errors
- Invalid input parameters
- Missing process templates

## Implementation Details

- **Framework**: Built using Microsoft Semantic Kernel
- **HTTP Client**: Uses dependency-injected HttpClient for API calls
- **Logging**: Comprehensive logging for debugging and monitoring
- **JSON Serialization**: Uses System.Text.Json for API response parsing
- **Authentication**: Basic authentication with PAT tokens

## Testing

The plugin includes comprehensive unit tests covering:
- Successful API responses
- Error handling scenarios
- Input validation
- Authentication flows
- Template matching logic

Run tests with: `dotnet test`

## Usage in Chat Interface

When users request project creation, the AI will:

1. Ask for organization name and PAT token
2. Retrieve available process templates if needed
3. Allow user to select or specify a process template
4. Create the project with specified parameters
5. Provide confirmation and project details

The plugin seamlessly integrates with the conversational AI to provide a natural project creation experience.