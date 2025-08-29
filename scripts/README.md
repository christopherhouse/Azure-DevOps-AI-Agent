# Container App Deployment Scripts

This directory contains Azure Container App deployment and configuration scripts that replace GitHub Actions with direct Azure CLI usage.

## Scripts

### `deploy-containerapp.sh`

A comprehensive bash script that creates or updates Azure Container Apps using the Azure CLI. Features include:

- **Multi-revision support**: Automatically creates new revisions for existing apps
- **Traffic management**: Routes 100% traffic to latest revision in multi-revision mode
- **Intelligent deployment**: Checks if app exists and chooses create vs update operation
- **Rich console output**: Colorful output with emojis for improved readability
- **Comprehensive validation**: Validates all parameters before deployment
- **Flexible configuration**: Supports all common container app configurations

### `configure-cors.sh`

A dedicated script for configuring Cross-Origin Resource Sharing (CORS) settings for Azure Container Apps. Features include:

- **CORS configuration**: Enable or update CORS settings for container apps
- **Multiple origins support**: Configure multiple allowed origins
- **Flexible methods**: Configure allowed HTTP methods
- **Credentials support**: Enable or disable credential sharing
- **Status checking**: Shows current CORS configuration after changes

#### Key Features

- ✨ **Beautiful output** with colors and emojis
- 🔄 **Multi-revision deployments** by default (deploy-containerapp.sh)
- 🚦 **Automatic traffic management** routes 100% traffic to latest revision (deploy-containerapp.sh)
- 🧹 **Old revision cleanup** deactivates unused revisions automatically (deploy-containerapp.sh)
- 🔑 **Key Vault secret references** for secure configuration with automatic deduplication (deploy-containerapp.sh)
- 👤 **User-assigned managed identities** for authentication (deploy-containerapp.sh)
- 🛡️ **Parameter validation** and error handling
- 📊 **Deployment summary** before execution
- 🌐 **Automatic URL retrieval** after deployment (deploy-containerapp.sh)
- 📝 **Verbose mode** for debugging
- 🌐 **CORS configuration** for cross-origin request support (configure-cors.sh)

#### Usage

##### Container App Deployment

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

##### CORS Configuration

```bash
./scripts/configure-cors.sh \
  --app-name "my-backend" \
  --resource-group "my-rg" \
  --allowed-origins "https://my-frontend.azurecontainerapps.io" \
  --verbose
```

#### Required Parameters

##### deploy-containerapp.sh

- `--environment, -e`: Container Apps Environment name or resource ID
- `--resource-group, -g`: Resource group name
- `--app-name, -n`: Container app name
- `--image, -i`: Container image URL
- `--target-port, -p`: Application port for ingress traffic

##### configure-cors.sh

- `--app-name, -n`: Container app name
- `--resource-group, -g`: Resource group name
- `--allowed-origins, -o`: Comma-separated list of allowed origins

#### Optional Parameters

##### deploy-containerapp.sh

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

##### configure-cors.sh

- `--allowed-methods, -m`: Comma-separated list of allowed methods (default: GET,POST,PUT,DELETE,OPTIONS)
- `--allow-credentials`: Allow credentials (default: true)
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
3. **Secure configuration**: Key Vault integration for secrets management with automatic deduplication
4. **Identity-based security**: User-assigned managed identities for each app
5. **Traffic routing**: Automatically routes 100% traffic to new deployments
6. **Improved debugging**: Verbose output and better error messages
7. **Consistency**: Uses the same Azure CLI tools as infrastructure deployment
8. **Maintainability**: Pure bash script that's easy to modify and extend
9. **No external dependencies**: Removes dependency on third-party GitHub Action

### Secret Deduplication

The script automatically handles scenarios where multiple environment variables reference the same Key Vault secret. For example:

```bash
--secret-ref "AZURE_TENANT_ID=entra-tenant-id" \
--secret-ref "NEXT_PUBLIC_AZURE_TENANT_ID=entra-tenant-id"
```

Instead of creating duplicate secret entries that would cause Azure CLI errors, the script intelligently deduplicates them to create only one Key Vault reference per unique secret name. This is particularly useful for frontend applications that need both server-side and client-side environment variables referencing the same secrets.

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