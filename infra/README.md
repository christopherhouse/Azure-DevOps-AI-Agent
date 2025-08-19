# Infrastructure Documentation

This directory contains Infrastructure as Code (IaC) templates for deploying the Azure DevOps AI Agent solution to Azure.

## Overview

The infrastructure is defined using Azure Bicep templates and follows Azure Well-Architected Framework principles. The deployment creates all necessary Azure resources for hosting the containerized application.

## Architecture Components

### Core Services
- **Azure Container Apps**: Hosts frontend and backend applications
- **Azure Container Registry**: Stores container images
- **Azure OpenAI**: Provides AI capabilities via GPT-4
- **Azure Key Vault**: Secures secrets and certificates
- **Application Insights**: Monitors application performance
- **Log Analytics**: Centralized logging and analytics

### Supporting Services
- **Managed Identity**: Secure access to Azure resources
- **Resource Groups**: Logical resource organization
- **RBAC**: Role-based access control

## Environments

### Development (dev)
- **Purpose**: Development and testing
- **Cost Optimization**: Smaller SKUs, fewer replicas
- **Features**: Basic monitoring, 30-day log retention

### Production (prod)
- **Purpose**: Production workloads
- **High Availability**: Zone redundancy, multiple replicas
- **Features**: Enhanced monitoring, 90-day log retention, premium SKUs

## File Structure

```
infra/
├── main.bicep              # Main infrastructure template
├── parameters/
│   ├── dev.bicepparam     # Development environment parameters
│   └── prod.bicepparam    # Production environment parameters
├── modules/               # Custom Bicep modules (if needed)
└── README.md             # This file
```

## Prerequisites

### Required Tools
- **Azure CLI**: Version 2.50.0 or later
- **Bicep CLI**: Version 0.20.0 or later
- **PowerShell**: Version 7.0 or later (optional)

### Required Permissions
- **Subscription Contributor**: To create and manage resources
- **User Access Administrator**: To assign RBAC roles

### Required Information
- Azure DevOps organization URL
- Microsoft Entra ID tenant ID and client ID
- Custom domain name (for production)

## Deployment Instructions

### 1. Prepare Environment

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "Your Subscription Name"

# Install/update Bicep
az bicep install
az bicep upgrade
```

### 2. Validate Templates

```bash
# Validate development environment
az deployment group validate \
  --resource-group rg-azure-devops-agent-dev \
  --template-file main.bicep \
  --parameters parameters/dev.bicepparam

# Validate production environment
az deployment group validate \
  --resource-group rg-azure-devops-agent-prod \
  --template-file main.bicep \
  --parameters parameters/prod.bicepparam
```

### 3. Deploy to Development

```bash
# Create resource group
az group create \
  --name rg-azure-devops-agent-dev \
  --location eastus \
  --tags environment=dev project=azure-devops-agent

# Deploy infrastructure
az deployment group create \
  --resource-group rg-azure-devops-agent-dev \
  --template-file main.bicep \
  --parameters parameters/dev.bicepparam \
  --name "infra-deployment-$(date +%Y%m%d-%H%M%S)"
```

### 4. Deploy to Production

```bash
# Create resource group
az group create \
  --name rg-azure-devops-agent-prod \
  --location eastus \
  --tags environment=prod project=azure-devops-agent

# Deploy infrastructure
az deployment group create \
  --resource-group rg-azure-devops-agent-prod \
  --template-file main.bicep \
  --parameters parameters/prod.bicepparam \
  --name "infra-deployment-$(date +%Y%m%d-%H%M%S)"
```

## Post-Deployment Configuration

### 1. Configure Secrets in Key Vault

```bash
# Set Azure OpenAI API key
az keyvault secret set \
  --vault-name azdo-ai-agent-dev-kv \
  --name azure-openai-key \
  --value "your-openai-api-key"

# Set Entra ID client secret
az keyvault secret set \
  --vault-name azdo-ai-agent-dev-kv \
  --name entra-client-secret \
  --value "your-entra-client-secret"

# Set Azure DevOps PAT (if needed)
az keyvault secret set \
  --vault-name azdo-ai-agent-dev-kv \
  --name azure-devops-pat \
  --value "your-azure-devops-pat"
```

### 2. Update Container Images

```bash
# Get Container Registry login server
ACR_LOGIN_SERVER=$(az acr show \
  --name azdoaiagentdevacr \
  --resource-group rg-azure-devops-agent-dev \
  --query loginServer \
  --output tsv)

# Update backend container app
az containerapp update \
  --resource-group rg-azure-devops-agent-dev \
  --name azdo-ai-agent-dev-backend \
  --image ${ACR_LOGIN_SERVER}/backend:latest

# Update frontend container app
az containerapp update \
  --resource-group rg-azure-devops-agent-dev \
  --name azdo-ai-agent-dev-frontend \
  --image ${ACR_LOGIN_SERVER}/frontend:latest
```

### 3. Configure DNS (Production Only)

```bash
# Get Container App FQDN
FRONTEND_FQDN=$(az containerapp show \
  --resource-group rg-azure-devops-agent-prod \
  --name azdo-ai-agent-prod-frontend \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

echo "Configure CNAME record:"
echo "Name: azdo-ai-agent (or your preferred subdomain)"
echo "Value: ${FRONTEND_FQDN}"
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

## Monitoring and Observability

### Application Insights

- **Development**: Basic monitoring, 30-day retention
- **Production**: Advanced monitoring, 90-day retention
- **Custom Metrics**: Application-specific performance indicators
- **Alerts**: Configured for critical performance thresholds

### Log Analytics

- **Structured Logging**: JSON-formatted application logs
- **Query Examples**: Common Kusto queries for troubleshooting
- **Dashboards**: Pre-built monitoring dashboards

### Health Checks

```bash
# Check backend health
curl https://azdo-ai-agent-dev-backend.proudsand-12345.eastus.azurecontainerapps.io/health

# Check frontend accessibility
curl https://azdo-ai-agent-dev-frontend.proudsand-12345.eastus.azurecontainerapps.io
```

## Security Configuration

### Network Security
- **HTTPS Only**: All traffic encrypted in transit
- **Private Networking**: Internal communication between services
- **Azure Front Door**: (Optional) DDoS protection and WAF

### Identity and Access
- **Managed Identity**: Used for all Azure resource access
- **RBAC**: Principle of least privilege
- **Key Vault**: Centralized secret management

### Compliance
- **Data Encryption**: At rest and in transit
- **Audit Logging**: All resource access logged
- **Backup**: Automated backup of critical data

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

### Cost Management
```bash
# Monitor costs by resource group
az consumption usage list \
  --start-date 2024-01-01 \
  --end-date 2024-01-31 \
  --include-additional-properties \
  --query "[?resourceGroup=='rg-azure-devops-agent-dev']"
```

## Troubleshooting

### Common Issues

1. **Deployment Failures**
   ```bash
   # Check deployment status
   az deployment group show \
     --resource-group rg-azure-devops-agent-dev \
     --name your-deployment-name
   ```

2. **Container App Issues**
   ```bash
   # Check container app logs
   az containerapp logs show \
     --resource-group rg-azure-devops-agent-dev \
     --name azdo-ai-agent-dev-backend \
     --follow
   ```

3. **Access Issues**
   ```bash
   # Check managed identity permissions
   az role assignment list \
     --assignee $(az identity show \
       --resource-group rg-azure-devops-agent-dev \
       --name azdo-ai-agent-dev-mi \
       --query principalId \
       --output tsv)
   ```

### Support Resources

- [Azure Container Apps Documentation](https://docs.microsoft.com/en-us/azure/container-apps/)
- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure OpenAI Service Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)
- [Azure DevOps REST API](https://docs.microsoft.com/en-us/rest/api/azure/devops/)

## Maintenance

### Regular Tasks
- **Security Updates**: Monthly review of security recommendations
- **Cost Review**: Quarterly cost optimization review
- **Performance Review**: Monthly performance analysis
- **Backup Verification**: Weekly backup validation

### Upgrade Procedures
- **Infrastructure Updates**: Use blue-green deployment pattern
- **Container Updates**: Automated via CI/CD pipeline
- **Service Updates**: Planned maintenance windows