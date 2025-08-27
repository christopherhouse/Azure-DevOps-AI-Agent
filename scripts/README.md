# Container App Deployment Script

This directory contains the Azure Container App deployment script that replaces the `azure/container-apps-deploy-action` GitHub Action.

## Scripts

### `deploy-containerapp.sh`

A comprehensive bash script that creates or updates Azure Container Apps using the Azure CLI. Features include:

- **Multi-revision support**: Automatically creates new revisions for existing apps
- **Traffic management**: Routes 100% traffic to latest revision in multi-revision mode
- **Intelligent deployment**: Checks if app exists and chooses create vs update operation
- **Rich console output**: Colorful output with emojis for improved readability
- **Comprehensive validation**: Validates all parameters before deployment
- **Flexible configuration**: Supports all common container app configurations

#### Key Features

- ✨ **Beautiful output** with colors and emojis
- 🔄 **Multi-revision deployments** by default
- 🚦 **Automatic traffic management** routes 100% traffic to latest revision
- 🧹 **Old revision cleanup** deactivates unused revisions automatically
- 🔑 **Key Vault secret references** for secure configuration
- 👤 **User-assigned managed identities** for authentication
- 🛡️ **Parameter validation** and error handling
- 📊 **Deployment summary** before execution
- 🌐 **Automatic URL retrieval** after deployment
- 📝 **Verbose mode** for debugging

#### Usage

```bash
./scripts/deploy-containerapp.sh \
  --environment "my-containerapp-env" \
  --resource-group "my-rg" \
  --app-name "my-app" \
  --image "myregistry.azurecr.io/app:v1.0" \
  --target-port 8000 \
  --registry-identity system \
  --managed-identity "12345678-1234-1234-1234-123456789abc" \
  --env-var "API_VERSION=1.0" \
  --secret-ref "DATABASE_PASSWORD=database-password" \
  --verbose
```

#### Required Parameters

- `--environment, -e`: Container Apps Environment name or resource ID
- `--resource-group, -g`: Resource group name  
- `--app-name, -n`: Container app name
- `--image, -i`: Container image URL
- `--target-port, -p`: Application port for ingress traffic

#### Optional Parameters

- `--ingress`: Ingress type (external/internal, default: external)
- `--registry-server`: Container registry server
- `--registry-identity`: Managed identity for registry auth
- `--managed-identity`: User-assigned managed identity for the container app
- `--env-var`: Environment variables (can be used multiple times)
- `--secret`: Inline secrets (can be used multiple times)
- `--secret-ref`: Key Vault secret references (can be used multiple times)
- `--cpu`: CPU allocation (default: 1.0)
- `--memory`: Memory allocation (default: 2Gi)
- `--min-replicas`: Minimum replicas (default: 1)
- `--max-replicas`: Maximum replicas (default: 10)
- `--revisions-mode`: multiple (default) or single
- `--verbose, -v`: Enable verbose output

## Integration with GitHub Actions

The script is integrated into the deployment workflow at `.github/workflows/deploy.yml` and replaces the previous `azure/container-apps-deploy-action` usage.

### Before (using GitHub Action)

```yaml
- name: Deploy Backend Container App
  uses: azure/container-apps-deploy-action@v1
  with:
    acrName: ${{ steps.extract-acr-name.outputs.acr-name }}
    containerAppName: ${{ env.BACKEND_APP_NAME }}
    resourceGroup: ${{ env.AZURE_RESOURCE_GROUP }}
    imageToDeploy: ${{ steps.deploy-infra.outputs.containerRegistryLoginServer }}/backend:${{ env.RELEASE_TAG }}
    targetPort: 8000
    ingress: external
    environmentVariables: |
      AZURE_OPENAI_ENDPOINT=secretref:azure-openai-endpoint
      AZURE_OPENAI_KEY=secretref:azure-openai-key
      # ... more env vars
```

### After (using custom script)

```yaml
- name: Deploy Backend Container App
  run: |
    ./scripts/deploy-containerapp.sh \
      --environment "${{ steps.deploy-infra.outputs.containerAppsEnvironmentName }}" \
      --resource-group "${{ env.AZURE_RESOURCE_GROUP }}" \
      --app-name "${{ env.BACKEND_APP_NAME }}" \
      --image "${{ steps.deploy-infra.outputs.containerRegistryLoginServer }}/backend:${{ env.RELEASE_TAG }}" \
      --target-port 8000 \
      --registry-identity system \
      --managed-identity "${{ steps.deploy-infra.outputs.backendManagedIdentityClientId }}" \
      --revisions-mode multiple \
      --env-var "AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4" \
      --secret-ref "AZURE_OPENAI_ENDPOINT=azure-openai-endpoint" \
      --secret-ref "AZURE_OPENAI_KEY=azure-openai-key" \
      --verbose
```

## Benefits

1. **Better control**: Direct Azure CLI usage provides more control over deployment
2. **Multi-revision support**: Built-in support for Azure Container Apps revisions with automatic traffic management
3. **Secure configuration**: Key Vault integration for secrets management
4. **Identity-based security**: User-assigned managed identities for each app
5. **Traffic routing**: Automatically routes 100% traffic to new deployments
6. **Improved debugging**: Verbose output and better error messages
7. **Consistency**: Uses the same Azure CLI tools as infrastructure deployment
8. **Maintainability**: Pure bash script that's easy to modify and extend
9. **No external dependencies**: Removes dependency on third-party GitHub Action

## Testing

To test the script locally:

```bash
# View help
./scripts/deploy-containerapp.sh --help

# Test with dry-run (will fail at Azure CLI execution, but validates script logic)
./scripts/deploy-containerapp.sh \
  --environment test-env \
  --resource-group test-rg \
  --app-name test-app \
  --image nginx:latest \
  --target-port 80 \
  --verbose
```