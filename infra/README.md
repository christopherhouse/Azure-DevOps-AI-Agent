# Azure DevOps AI Agent Infrastructure

This directory contains the Infrastructure as Code (IaC) for the Azure DevOps AI Agent using Bicep templates and Azure Verified Modules (AVM).

## Overview

The infrastructure deployment uses **Azure Verified Modules (AVM)** exclusively for all supported Azure resource types. AVM provides Microsoft-verified, tested, and supported Bicep modules that follow consistent patterns and best practices.

## Architecture

### Core Components

- **Azure Container Apps Environment**: Hosts the frontend and backend applications
- **Azure Container Registry**: Stores container images with managed identity authentication
- **Azure OpenAI Service**: Provides GPT-4 AI capabilities for the agent
- **Azure Key Vault**: Securely stores secrets and certificates
- **Application Insights**: Application performance monitoring and telemetry
- **Log Analytics Workspace**: Centralized logging and analytics
- **User-Assigned Managed Identity**: Secure authentication between Azure services

### Network Security

- Private registry authentication using managed identity
- Key Vault integration for secret management
- Role-based access control (RBAC) implemented through Azure Verified Modules
- Diagnostic settings configured for all resources with category groups

## File Structure

```
infra/
├── main.bicep                          # Main deployment template using AVM modules
├── main-original.bicep                 # Original template (backup)
├── parameters/
│   ├── main.dev.bicepparam            # Development environment parameters
│   └── main.prod.bicepparam           # Production environment parameters
└── README.md                          # This file
```

## Azure Verified Modules Used

| Resource Type | AVM Module | Version |
|---------------|------------|---------|
| Managed Identity | `avm/res/managed-identity/user-assigned-identity` | 0.4.0 |
| Log Analytics | `avm/res/operational-insights/workspace` | 0.11.1 |
| Application Insights | `avm/res/insights/component` | 0.6.0 |
| Container Registry | `avm/res/container-registry/registry` | 0.9.1 |
| Key Vault | `avm/res/key-vault/vault` | 0.12.1 |
| Azure OpenAI | `avm/res/cognitive-services/account` | 0.10.1 |
| Container Apps Environment | `avm/res/app/managed-environment` | 0.10.0 |
| Container Apps | `avm/res/app/container-app` | 0.12.0 |

All modules use the latest stable versions and are configured with:
- Diagnostic settings using category groups (recommended best practice)
- Proper resource tagging
- Managed identity authentication
- RBAC assignments for secure access

## RBAC Implementation

This infrastructure follows Azure Verified Modules (AVM) best practices by implementing role-based access control (RBAC) through the built-in `roleAssignments` parameter available in AVM modules, rather than creating standalone role assignment resources.

### Role Assignment Strategy

| Resource | Managed Identity | Role | Purpose |
|----------|------------------|------|---------|
| Container Registry | Backend MI | AcrPull | Pull container images for backend app |
| Container Registry | Frontend MI | AcrPull | Pull container images for frontend app |
| Key Vault | Backend MI | Key Vault Secrets User | Access secrets for backend configuration |
| Key Vault | Frontend MI | Key Vault Secrets User | Access secrets for frontend configuration |
| Azure OpenAI | AI Services MI | Cognitive Services User | Access OpenAI services |

### Benefits of AVM RBAC Approach

- **Consolidated Management**: Role assignments are managed within the resource modules themselves
- **Consistent Patterns**: Follows Microsoft's recommended AVM standards
- **Reduced Complexity**: Eliminates standalone role assignment resources
- **Better Maintainability**: Single source of truth for resource and its permissions
- **Cleaner Code**: More readable and maintainable infrastructure templates

## Deployment

### Prerequisites

- Azure CLI with Bicep extension
- Azure subscription with appropriate permissions
- Resource group created for the target environment

### Local Validation

```bash
# Lint the main template
az bicep lint --file main.bicep

# Build the template to validate syntax
az bicep build --file main.bicep

# Validate parameter files
az bicep build-params --file parameters/main.dev.bicepparam
az bicep build-params --file parameters/main.prod.bicepparam
```

### Development Environment

```bash
# Create resource group
az group create \
  --name rg-azure-devops-agent-dev \
  --location eastus2 \
  --tags environment=dev project=azure-devops-agent

# Deploy to development
az deployment group create \
  --resource-group rg-azure-devops-agent-dev \
  --template-file main.bicep \
  --parameters parameters/main.dev.bicepparam
```

### Production Environment

```bash
# Create resource group
az group create \
  --name rg-azure-devops-agent-prod \
  --location eastus2 \
  --tags environment=prod project=azure-devops-agent

# Deploy to production
az deployment group create \
  --resource-group rg-azure-devops-agent-prod \
  --template-file main.bicep \
  --parameters parameters/main.prod.bicepparam
```

## CI/CD Integration

The GitHub Actions workflow (`.github/workflows/infrastructure.yml`) is configured to:

1. **Validate**: Lint and validate templates against target resource groups
2. **Security Scan**: Run Checkov security analysis on generated ARM templates
3. **Deploy Dev**: Deploy to development environment automatically on main branch changes
4. **Deploy Prod**: Deploy to production with manual approval and blue-green deployment strategy

## Configuration

### Environment-Specific Settings

| Setting | Development | Production |
|---------|-------------|------------|
| SKU Tier | Standard | Premium |
| Min Replicas | 1 | 2 |
| Max Replicas | 3 | 10 |
| CPU | 0.5 | 1.0 |
| Memory | 1Gi | 2Gi |
| Log Retention | 30 days | 90 days |
| Zone Redundancy | Disabled | Enabled |
| Purge Protection | Disabled | Enabled |

### Required Secrets

The following secrets are automatically populated by the deployment workflow from GitHub secrets:

- `azure-openai-key`: OpenAI service access key (from `AZURE_OPENAI_KEY`)
- `entra-tenant-id`: Microsoft Entra ID tenant ID (from `AZURE_TENANT_ID`) 
- `frontend-client-id`: Frontend application client ID (from `FRONTEND_CLIENT_ID`)
- `backend-client-id`: Backend application client ID (from `BACKEND_CLIENT_ID`)
- `backend-client-secret`: Backend application client secret (from `BACKEND_CLIENT_SECRET`)
- `app-insights-connection-string`: Application Insights connection string (auto-generated)

For local development or manual setup, you can set these secrets manually:

```bash
# Set Azure OpenAI API key
az keyvault secret set \
  --vault-name azdo-ai-agent-dev-kv \
  --name azure-openai-key \
  --value "your-openai-api-key"

# Set Entra ID tenant ID
az keyvault secret set \
  --vault-name azdo-ai-agent-dev-kv \
  --name entra-tenant-id \
  --value "your-tenant-id"

# Set frontend client ID
az keyvault secret set \
  --vault-name azdo-ai-agent-dev-kv \
  --name frontend-client-id \
  --value "your-frontend-client-id"

# Set backend client ID
az keyvault secret set \
  --vault-name azdo-ai-agent-dev-kv \
  --name backend-client-id \
  --value "your-backend-client-id"

# Set backend client secret
az keyvault secret set \
  --vault-name azdo-ai-agent-dev-kv \
  --name backend-client-secret \
  --value "your-backend-client-secret"
```

## Security Features

- **Managed Identity**: All inter-service authentication uses managed identities
- **RBAC**: Least-privilege access implemented through Azure Verified Modules (AVM) `roleAssignments` parameter:
  - `AcrPull`: Container registry access for backend and frontend managed identities
  - `Key Vault Secrets User`: Key vault secret access for backend and frontend managed identities
  - `Cognitive Services User`: OpenAI service access for AI services managed identity
- **AVM Integration**: Role assignments managed through AVM modules rather than standalone resources
- **Private Networking**: Optional private endpoints for enhanced security
- **Diagnostic Logging**: Comprehensive logging for security monitoring
- **Secret Management**: All sensitive data stored in Key Vault

## Monitoring & Observability

- **Application Insights**: Application performance monitoring
- **Log Analytics**: Centralized logging and query capabilities
- **Diagnostic Settings**: All resources configured with category groups for optimal log collection
- **Health Checks**: Container app health monitoring

```bash
# Check backend health
curl https://azdo-ai-agent-dev-backend.proudsand-12345.eastus2.azurecontainerapps.io/health

# Check frontend accessibility
curl https://azdo-ai-agent-dev-frontend.proudsand-12345.eastus2.azurecontainerapps.io
```

## Resource Naming Convention

Resources follow a consistent naming pattern:

```
{appNamePrefix}-{environment}-{resourceType}

Examples:
- azdo-ai-agent-dev-env (Container Apps Environment)
- azdo-ai-agent-prod-openai (Azure OpenAI)
- azdoaiagentdevacr (Container Registry, no hyphens)
```

## Cost Optimization

### Development Environment
- **Container Apps**: Consumption-based pricing
- **Azure OpenAI**: Standard pricing tier
- **Storage**: Standard redundancy
- **Estimated Cost**: $50-100/month

### Production Environment
- **Container Apps**: Reserved instances for predictable workloads
- **Azure OpenAI**: Standard pricing with commitment discounts
- **Storage**: Premium redundancy with backup
- **Estimated Cost**: $200-500/month

## Troubleshooting

### Common Issues

1. **Module Version Conflicts**: Ensure you're using the latest stable AVM module versions
2. **Parameter File Path**: Verify parameter files reference `../main.bicep` correctly
3. **RBAC Permissions**: Ensure deployment identity has sufficient permissions for role assignments
4. **Resource Naming**: Verify resource names meet Azure naming requirements and are globally unique

### Useful Commands

```bash
# Check AVM module versions
az bicep list-versions --module-path br/public:avm/res/app/container-app

# Validate deployment without executing
az deployment group validate \
  --resource-group <resource-group> \
  --template-file main.bicep \
  --parameters parameters/main.dev.bicepparam

# View deployment operations
az deployment group list --resource-group <resource-group>

# Check container app logs
az containerapp logs show \
  --resource-group rg-azure-devops-agent-dev \
  --name azdo-ai-agent-dev-backend \
  --follow

# Monitor costs by resource group
az consumption usage list \
  --start-date 2024-01-01 \
  --end-date 2024-01-31 \
  --include-additional-properties \
  --query "[?resourceGroup=='rg-azure-devops-agent-dev']"
```

## Contributing

When making infrastructure changes:

1. **Always use AVM modules** when available for any Azure resource type
2. Check for updated AVM module versions periodically
3. Test changes in development environment first
4. Update parameter files for both environments
5. Ensure diagnostic settings are configured for new resources
6. Update this documentation for any architectural changes

## Additional Resources

- [Azure Verified Modules (AVM)](https://aka.ms/AVM)
- [Bicep Registry Modules](https://github.com/Azure/bicep-registry-modules)
- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure DevOps REST API](https://docs.microsoft.com/en-us/rest/api/azure/devops/)