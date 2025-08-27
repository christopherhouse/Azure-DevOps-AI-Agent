# Deployment Secrets Configuration Guide

This guide explains how to configure GitHub secrets for the Azure DevOps AI Agent deployment workflow after the recent fix for issue #166.

## Overview

The deployment workflow has been updated to properly use environment secrets instead of falling back to placeholder values. This ensures that the correct frontend and backend client IDs are passed to the Bicep infrastructure template during deployment.

## ⚠️ Important Change

**Before**: The workflow would silently use placeholder values ('your-frontend-client-id-here', 'your-backend-client-id-here') when secrets were missing.

**After**: The workflow will now fail with clear error messages if environment secrets are not configured, ensuring you know immediately if secrets are missing.

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

## 🚀 Verification

After configuring the secrets, you can verify the deployment works by:

1. **Triggering a deployment**: Push to main branch or manually trigger the workflow
2. **Check workflow logs**: The deployment should show actual values instead of placeholders
3. **Verify infrastructure**: Check that Key Vault in Azure contains the correct client IDs

### Expected Log Output

✅ **Success**: 
```
Deploying infrastructure to dev environment...
✅ All required secrets validated
🏗️ Container Apps Environment: azdo-ai-agent-dev-env
🔑 Frontend Identity Client ID: 12345678-1234-1234-1234-123456789012
```

❌ **Failure (missing secret)**:
```
❌ Error: FRONTEND_CLIENT_ID environment secret is not configured
```

## 🔄 Troubleshooting

### "Environment secret is not configured" Error

**Cause**: The specified secret is not set in the GitHub environment.

**Solution**:
1. Check the environment name matches exactly (`dev` or `prod`)
2. Verify the secret name is spelled correctly
3. Ensure the secret has a value (not empty)

### "Deployment failed with placeholder values"

**Cause**: Using old cached workflow or secrets not configured.

**Solution**:
1. Ensure you're using the latest workflow version
2. Re-run the workflow after configuring all required secrets

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