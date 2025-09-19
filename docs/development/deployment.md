# Deployment Guide

This guide covers deploying the Azure DevOps AI Agent to Azure Container Apps using the automated CI/CD pipeline.

## Overview

The project uses GitHub Actions workflows to automate deployment to both development and production environments. The infrastructure is deployed using Bicep templates, and applications are deployed as containers to Azure Container Apps.

## Prerequisites

### 1. Azure Resources

- Azure Subscription with appropriate permissions
- Resource groups for each environment:
  - `rg-azure-devops-agent-dev` (development)
  - `rg-azure-devops-agent-prod` (production)

### 2. Microsoft Entra ID Configuration

- App registrations for frontend and backend applications
- Service principal for GitHub Actions authentication
- Tenant IDs and client IDs documented

### 3. GitHub Configuration

- **Repository secrets**: Azure authentication credentials
- **Environment secrets**: Application-specific secrets for each environment

**⚠️ Important**: See [Deployment Secrets Configuration](../deployment-secrets-configuration.md) for detailed secret setup instructions.

## Deployment Architecture

```
┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
│   GitHub Actions │   │   Azure Bicep   │   │ Container Apps  │
│                 │   │                 │   │                 │
│ 1. Build Images │──►│ 2. Deploy Infra │──►│ 3. Deploy Apps  │
│ 2. Run Tests    │   │    - Key Vault  │   │    - Backend    │
│ 3. Security Scan│   │    - Container  │   │    - Frontend   │
└─────────────────┘   │      Registry   │   └─────────────────┘
                      │    - OpenAI     │
                      └─────────────────┘
```

## Deployment Process

### Automatic Deployment

1. **Trigger**: Push to `main` branch or manual workflow dispatch
2. **CI Pipeline**: Build, test, and publish container images
3. **Development Deployment**: Automatic deployment to dev environment
4. **Production Deployment**: Automatic deployment to prod environment

### Manual Deployment

You can manually trigger deployments using GitHub Actions:

1. Go to **Actions** tab in GitHub repository
2. Select **CI - Build, Test, and Publish** workflow
3. Click **Run workflow**
4. Choose the branch and confirm

## Environment Configuration

### Development Environment

- **Purpose**: Testing and development
- **URL**: `https://dev-azure-devops-agent.azurecontainerapps.io`
- **Resources**: Lower-tier SKUs for cost optimization
- **Auto-deployment**: Enabled on main branch pushes

### Production Environment

- **Purpose**: Live production workload
- **URL**: `https://azure-devops-agent.azurecontainerapps.io`
- **Resources**: Production-grade SKUs with high availability
- **Auto-deployment**: Enabled with production safeguards

## Secrets Configuration

**Critical**: The deployment requires proper secret configuration to work correctly.

### Environment Secrets (Recommended)

For each environment (`dev`, `prod`), configure these in GitHub → Settings → Environments:

- `FRONTEND_CLIENT_ID`
- `BACKEND_CLIENT_ID`
- `AZURE_TENANT_ID`
- `BACKEND_CLIENT_SECRET`

**Note**: `AZURE_OPENAI_KEY` is no longer needed as a GitHub secret. The deployment workflow automatically retrieves the API key from the deployed Azure OpenAI resource.

**📖 Full Guide**: [Deployment Secrets Configuration](../deployment-secrets-configuration.md)

### Repository Secrets (Alternative)

Alternative approach using repository secrets with environment suffixes:

- `FRONTEND_CLIENT_ID_DEV` / `FRONTEND_CLIENT_ID_PROD`
- `BACKEND_CLIENT_ID_DEV` / `BACKEND_CLIENT_ID_PROD`
- etc.

**📖 Alternative Guide**: [Deployment Secrets Alternatives](../deployment-secrets-alternatives.md)

## Monitoring and Verification

### Deployment Success

1. **Check workflow logs** for successful completion
2. **Verify application health**:
   - Backend: `https://{backend-url}/health`
   - Frontend: `https://{frontend-url}` (should load login page)
3. **Check Azure resources** are created and configured correctly

### Troubleshooting

#### Secret Configuration Errors

```
❌ Error: FRONTEND_CLIENT_ID environment secret is not configured
```

**Solution**: Configure missing environment secrets as described in the secrets configuration guide.

#### Infrastructure Deployment Failures

1. Check Bicep template validation
2. Verify Azure permissions
3. Check resource naming conflicts

#### Container App Deployment Failures

1. Verify container images exist in registry
2. Check managed identity permissions
3. Review application logs in Azure Portal

## Infrastructure Management

### Bicep Templates

- **Main template**: `infra/main.bicep`
- **Parameters**: `infra/parameters/main.{environment}.bicepparam`
- **Modules**: Azure Verified Modules (AVM) for best practices

### Resource Naming Convention

```
azdo-ai-agent-{environment}-{resource-type}
```

Examples:
- `azdo-ai-agent-dev-backend` (Container App)
- `azdo-ai-agent-prod-kv` (Key Vault)
- `azdo-ai-agent-dev-env` (Container Apps Environment)

### Cost Optimization

- **Development**: Minimal replicas and lower SKUs
- **Production**: Optimized for performance and availability
- **Auto-scaling**: Configured based on CPU and memory usage

## Security

### Container Security

- Container images scanned with Trivy
- Base images updated regularly
- Minimal attack surface

### Network Security

- Private networking between services
- HTTPS-only communication
- Managed identities for service authentication

### Secrets Management

- Azure Key Vault for runtime secrets
- GitHub environment secrets for deployment-time secrets
- No secrets in container images or source code

## Rollback Strategy

### Automatic Rollback

- Health checks monitor deployment success
- Failed deployments trigger automatic rollback
- Previous container images maintained for quick recovery

### Manual Rollback

1. Navigate to Azure Portal → Container Apps
2. Select the application
3. Go to **Revisions**
4. Activate previous working revision

## Next Steps

1. **Configure secrets** following the [secrets configuration guide](../deployment-secrets-configuration.md)
2. **Test deployment** by pushing to main branch
3. **Monitor applications** using Application Insights
4. **Set up alerts** for production monitoring

## Related Documentation

- [Secrets Configuration Guide](../deployment-secrets-configuration.md)
- [Infrastructure Documentation](../../infra/README.md)
- [Development Setup](setup.md)
- [Security Guide](security.md)