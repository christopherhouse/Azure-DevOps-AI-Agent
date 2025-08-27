# Deployment Secrets Configuration Guide

This guide explains how to configure GitHub secrets for the Azure DevOps AI Agent deployment workflow after the recent fix for issue #170.

## Overview

The deployment workflow has been updated to support **flexible secret configuration** with automatic fallback between environment secrets and repository secrets. This ensures deployments work regardless of which secret configuration approach is used.

## ✨ Flexible Secret Configuration

The deployment workflow now supports **two approaches** for secret configuration:

1. **Environment Secrets** (Primary) - Provides better separation between environments
2. **Repository Secrets with Environment Suffixes** (Fallback) - Alternative when environment secrets are not available

The workflow automatically tries environment secrets first and falls back to repository secrets with environment suffixes if environment secrets are not configured. This provides maximum flexibility while maintaining security best practices.

## 🔑 Required Environment Secrets

For each environment (`dev`, `prod`), you need to configure these secrets in GitHub:

### How to Configure Environment Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** → **Environments**
3. Select your environment (`dev` or `prod`)
4. Click **Add Environment secret**
5. Add each of the following secrets:

| Secret Name | Description | Where to Find |
|-------------|-------------|---------------|
| `FRONTEND_CLIENT_ID` | Microsoft Entra ID Application (client) ID for the frontend app | Azure Portal → Entra ID → App registrations → [Your Frontend App] → Overview |
| `BACKEND_CLIENT_ID` | Microsoft Entra ID Application (client) ID for the backend app | Azure Portal → Entra ID → App registrations → [Your Backend App] → Overview |
| `AZURE_TENANT_ID` | Microsoft Entra ID Tenant ID | Azure Portal → Entra ID → Properties → Tenant ID |
| `BACKEND_CLIENT_SECRET` | Microsoft Entra ID Client Secret for the backend app | Azure Portal → Entra ID → App registrations → [Your Backend App] → Certificates & secrets |
| `AZURE_OPENAI_KEY` | Azure OpenAI Service API Key | Azure Portal → [Your OpenAI Resource] → Keys and Endpoint |

### Example Values

```
FRONTEND_CLIENT_ID: 12345678-1234-1234-1234-123456789012
BACKEND_CLIENT_ID: 87654321-4321-4321-4321-210987654321
AZURE_TENANT_ID: 11111111-2222-3333-4444-555555555555
BACKEND_CLIENT_SECRET: ABC123def456...
AZURE_OPENAI_KEY: sk-...
```

## 🔄 Alternative: Repository Secrets with Environment Suffixes

If you prefer to use repository secrets instead of environment secrets, or if environment secrets are not available in your GitHub plan, you can configure repository secrets with environment suffixes. The workflow will automatically detect and use these secrets when environment secrets are not available.

### How to Configure Repository Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each of the following secrets with environment suffixes:

| Secret Name | Description | Used For |
|-------------|-------------|----------|
| `FRONTEND_CLIENT_ID_DEV` | Microsoft Entra ID Application (client) ID for frontend | Development deployments |
| `FRONTEND_CLIENT_ID_PROD` | Microsoft Entra ID Application (client) ID for frontend | Production deployments |
| `BACKEND_CLIENT_ID_DEV` | Microsoft Entra ID Application (client) ID for backend | Development deployments |
| `BACKEND_CLIENT_ID_PROD` | Microsoft Entra ID Application (client) ID for backend | Production deployments |
| `AZURE_TENANT_ID_DEV` | Microsoft Entra ID Tenant ID | Development deployments |
| `AZURE_TENANT_ID_PROD` | Microsoft Entra ID Tenant ID | Production deployments |
| `BACKEND_CLIENT_SECRET_DEV` | Microsoft Entra ID Client Secret for backend | Development deployments |
| `BACKEND_CLIENT_SECRET_PROD` | Microsoft Entra ID Client Secret for backend | Production deployments |
| `AZURE_OPENAI_KEY_DEV` | Azure OpenAI Service API Key | Development deployments |
| `AZURE_OPENAI_KEY_PROD` | Azure OpenAI Service API Key | Production deployments |

### Example Repository Secret Values

```
FRONTEND_CLIENT_ID_DEV: 12345678-1234-1234-1234-123456789012
FRONTEND_CLIENT_ID_PROD: 11111111-1111-1111-1111-111111111111
BACKEND_CLIENT_ID_DEV: 87654321-4321-4321-4321-210987654321
BACKEND_CLIENT_ID_PROD: 22222222-2222-2222-2222-222222222222
AZURE_TENANT_ID_DEV: 11111111-2222-3333-4444-555555555555
AZURE_TENANT_ID_PROD: 66666666-7777-8888-9999-000000000000
# ... and so on for other secrets
```

## 🚀 Verification

After configuring the secrets, you can verify the deployment works by:

1. **Triggering a deployment**: Push to main branch or manually trigger the workflow
2. **Check workflow logs**: The deployment should show actual values instead of placeholders
3. **Verify infrastructure**: Check that Key Vault in Azure contains the correct client IDs

### Expected Log Output

✅ **Success with environment secrets**: 
```
Deploying infrastructure to dev environment...
✅ All required secrets validated
🏗️ Container Apps Environment: azdo-ai-agent-dev-env
🔑 Frontend Identity Client ID: 12345678-1234-1234-1234-123456789012
```

✅ **Success with repository secrets fallback**: 
```
Deploying infrastructure to dev environment...
⚠️ Environment secrets not found, trying repository secrets with environment suffixes...
✅ All required secrets validated
🏗️ Container Apps Environment: azdo-ai-agent-dev-env
🔑 Frontend Identity Client ID: 12345678-1234-1234-1234-123456789012
```

❌ **Failure (missing secrets)**:
```
❌ Error: Frontend client ID is not configured
💡 Configure either:
   - Environment secret: FRONTEND_CLIENT_ID in dev environment
   - Repository secret: FRONTEND_CLIENT_ID_DEV
📚 See docs/deployment-secrets-configuration.md for detailed instructions
```

## 🔄 Troubleshooting

### "Frontend/Backend client ID is not configured" Error

**Cause**: The required secrets are not configured in either environment secrets or repository secrets.

**Solution**:
1. **Option A**: Configure environment secrets (recommended)
   - Go to **Settings** → **Environments** → Select your environment
   - Add the required secrets (e.g., `FRONTEND_CLIENT_ID`, `BACKEND_CLIENT_ID`, etc.)
2. **Option B**: Configure repository secrets with environment suffixes
   - Go to **Settings** → **Secrets and variables** → **Actions**
   - Add secrets with environment suffixes (e.g., `FRONTEND_CLIENT_ID_DEV`, `FRONTEND_CLIENT_ID_PROD`)

### "Environment secrets not found, trying repository secrets..." Message

**Cause**: This is normal behavior when environment secrets are not configured.

**Action**: No action needed. The workflow is automatically falling back to repository secrets with environment suffixes.

### "Deployment failed with placeholder values"

**Cause**: Using an old cached workflow or secrets not configured properly.

**Solution**:
1. Ensure you're using the latest workflow version (should include fallback logic)
2. Re-run the workflow after configuring secrets using either approach
3. Check that secret names exactly match the expected format

### Different Secrets for Dev vs Prod

Yes! Each environment should have its own set of secrets:
- **Development**: Use development tenant and app registrations
- **Production**: Use production tenant and app registrations

This provides proper separation between environments.

## 🔄 Alternative Approach

If environment secrets don't work for your organization, see `docs/deployment-secrets-alternatives.md` for a repository secrets approach using environment suffixes (e.g., `FRONTEND_CLIENT_ID_DEV`, `FRONTEND_CLIENT_ID_PROD`).

## 📚 Related Documentation

- [GitHub Environments Documentation](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
- [Azure Entra ID App Registration Guide](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)

## 🔒 Security Best Practices

1. **Rotate secrets regularly** (especially client secrets)
2. **Use different tenants/apps** for dev and prod environments
3. **Limit app registration permissions** to minimum required
4. **Monitor secret usage** in Azure audit logs
5. **Never commit secrets** to source code