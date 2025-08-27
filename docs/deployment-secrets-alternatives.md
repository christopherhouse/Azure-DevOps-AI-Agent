# Deployment Secrets Configuration Alternatives

This document outlines two approaches for configuring secrets for the Azure DevOps AI Agent deployment workflow.

## Current Implementation: Environment Secrets (Primary)

The current workflow has been updated to use GitHub Environment Secrets. This is the recommended approach as it provides better separation between development and production configurations.

### Required Environment Secrets

For each environment (`dev`, `prod`), configure these secrets in GitHub:

1. **Settings** → **Environments** → Select environment → **Environment secrets**
2. Add the following secrets:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `FRONTEND_CLIENT_ID` | Microsoft Entra ID Application (client) ID for frontend | `12345678-1234-1234-1234-123456789012` |
| `BACKEND_CLIENT_ID` | Microsoft Entra ID Application (client) ID for backend | `87654321-4321-4321-4321-210987654321` |
| `AZURE_TENANT_ID` | Microsoft Entra ID Tenant ID | `11111111-2222-3333-4444-555555555555` |
| `BACKEND_CLIENT_SECRET` | Microsoft Entra ID Client Secret for backend | `secret_value_here` |
| `AZURE_OPENAI_KEY` | Azure OpenAI Service API Key | `openai_key_here` |

### Benefits
- Clear separation between environments
- No risk of using wrong secrets for wrong environment
- Better security through environment-specific access control

## Alternative: Repository Secrets with Environment Suffixes

If environment secrets cannot be used, here's an alternative implementation using repository secrets with environment suffixes.

### Required Repository Secrets

In **Settings** → **Secrets and variables** → **Actions** → **Repository secrets**, add:

| Secret Name | Description | Used For |
|-------------|-------------|----------|
| `FRONTEND_CLIENT_ID_DEV` | Development frontend client ID | Development deployments |
| `FRONTEND_CLIENT_ID_PROD` | Production frontend client ID | Production deployments |
| `BACKEND_CLIENT_ID_DEV` | Development backend client ID | Development deployments |
| `BACKEND_CLIENT_ID_PROD` | Production backend client ID | Production deployments |
| `AZURE_TENANT_ID_DEV` | Development tenant ID | Development deployments |
| `AZURE_TENANT_ID_PROD` | Production tenant ID | Production deployments |
| `BACKEND_CLIENT_SECRET_DEV` | Development backend client secret | Development deployments |
| `BACKEND_CLIENT_SECRET_PROD` | Production backend client secret | Production deployments |
| `AZURE_OPENAI_KEY_DEV` | Development OpenAI key | Development deployments |
| `AZURE_OPENAI_KEY_PROD` | Production OpenAI key | Production deployments |

### Workflow Changes Required

If you prefer this approach, replace the "Deploy Infrastructure" step in `.github/workflows/deploy.yml` with:

```yaml
    - name: Deploy Infrastructure
      id: deploy-infra
      run: |
        echo "Deploying infrastructure to ${{ inputs.environment }} environment..."
        
        # Set environment suffix for secret names
        ENV_SUFFIX=$(echo "${{ inputs.environment }}" | tr '[:lower:]' '[:upper:]')
        
        # Set secure parameter values from repository secrets with environment suffix
        if [[ "${{ inputs.environment }}" == "dev" ]]; then
          BACKEND_CLIENT_SECRET="${{ secrets.BACKEND_CLIENT_SECRET_DEV }}"
          AZURE_OPENAI_KEY="${{ secrets.AZURE_OPENAI_KEY_DEV }}"
          FRONTEND_CLIENT_ID="${{ secrets.FRONTEND_CLIENT_ID_DEV }}"
          BACKEND_CLIENT_ID="${{ secrets.BACKEND_CLIENT_ID_DEV }}"
          AZURE_TENANT_ID="${{ secrets.AZURE_TENANT_ID_DEV }}"
        elif [[ "${{ inputs.environment }}" == "prod" ]]; then
          BACKEND_CLIENT_SECRET="${{ secrets.BACKEND_CLIENT_SECRET_PROD }}"
          AZURE_OPENAI_KEY="${{ secrets.AZURE_OPENAI_KEY_PROD }}"
          FRONTEND_CLIENT_ID="${{ secrets.FRONTEND_CLIENT_ID_PROD }}"
          BACKEND_CLIENT_ID="${{ secrets.BACKEND_CLIENT_ID_PROD }}"
          AZURE_TENANT_ID="${{ secrets.AZURE_TENANT_ID_PROD }}"
        else
          echo "❌ Error: Unsupported environment: ${{ inputs.environment }}"
          exit 1
        fi
        
        # Validate that required secrets are provided
        if [[ -z "$FRONTEND_CLIENT_ID" ]]; then
          echo "❌ Error: Frontend client ID secret is not configured for ${{ inputs.environment }} environment"
          exit 1
        fi
        if [[ -z "$BACKEND_CLIENT_ID" ]]; then
          echo "❌ Error: Backend client ID secret is not configured for ${{ inputs.environment }} environment"
          exit 1
        fi
        if [[ -z "$AZURE_TENANT_ID" ]]; then
          echo "❌ Error: Azure tenant ID secret is not configured for ${{ inputs.environment }} environment"
          exit 1
        fi
        if [[ -z "$BACKEND_CLIENT_SECRET" ]]; then
          echo "❌ Error: Backend client secret is not configured for ${{ inputs.environment }} environment"
          exit 1
        fi
        if [[ -z "$AZURE_OPENAI_KEY" ]]; then
          echo "❌ Error: Azure OpenAI key is not configured for ${{ inputs.environment }} environment"
          exit 1
        fi
        
        # Continue with deployment...
        deployment_output=$(az deployment group create \
          --resource-group ${{ env.AZURE_RESOURCE_GROUP }} \
          --template-file infra/main.bicep \
          --parameters infra/parameters/main.${{ inputs.environment }}.bicepparam \
          --parameters backendClientSecret="${BACKEND_CLIENT_SECRET}" \
          --parameters azureOpenAIKey="${AZURE_OPENAI_KEY}" \
          --parameters frontendClientId="${FRONTEND_CLIENT_ID}" \
          --parameters backendClientId="${BACKEND_CLIENT_ID}" \
          --parameters entraIdTenantId="${AZURE_TENANT_ID}" \
          --name "infra-deploy-$(echo ${{ github.sha }} | cut -c1-8)" \
          --query 'properties.outputs' \
          --output json)
        
        # ... rest of the step remains the same
```

## Recommendation

Use the **Environment Secrets** approach (primary implementation) as it provides better security separation and follows GitHub Actions best practices. Only use the repository secrets approach if environment secrets are not available in your GitHub plan or organization settings.