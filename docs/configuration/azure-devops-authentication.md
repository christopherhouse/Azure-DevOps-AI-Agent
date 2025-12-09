# Azure DevOps Authentication Configuration

This guide explains how to configure authentication for the Azure DevOps API Service, which supports two authentication methods:

1. **Azure Identity (DefaultAzureCredential)** - For production deployments with managed identity
2. **Personal Access Token (PAT)** - For local development and debugging

## Configuration Options

The authentication method is controlled through the `AzureDevOps` section in your `appsettings.json` file:

```json
{
  "AzureDevOps": {
    "Organization": "your-organization-name",
    "Pat": "your-personal-access-token",
    "UsePat": false
  }
}
```

### Configuration Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Organization` | string | No | Azure DevOps organization name (can be passed at runtime) |
| `Pat` | string | Conditional* | Personal Access Token for Azure DevOps API access |
| `UsePat` | bool | No | Set to `true` to use PAT authentication, `false` for Azure Identity (default: `false`) |

*Required when `UsePat` is `true`

## Authentication Methods

### 1. Azure Identity (DefaultAzureCredential)

**Recommended for:** Production deployments, Azure-hosted environments

This is the default authentication method that uses Azure managed identity or other Azure credential sources through the DefaultAzureCredential chain.

#### Configuration Example

```json
{
  "AzureDevOps": {
    "Organization": "your-org",
    "UsePat": false
  },
  "ManagedIdentityClientId": "your-managed-identity-client-id"
}
```

#### Features
- Uses Azure managed identity for secure, credential-free authentication
- Supports both system-assigned and user-assigned managed identities
- No secrets to manage or rotate
- Automatically refreshes tokens

#### Prerequisites
- Azure managed identity must be configured
- Managed identity must have appropriate permissions to Azure DevOps
- For user-assigned managed identity, provide `ManagedIdentityClientId`

### 2. Personal Access Token (PAT)

**Recommended for:** Local development, debugging, testing

Use a Personal Access Token for direct authentication to Azure DevOps APIs.

#### Configuration Example

```json
{
  "AzureDevOps": {
    "Organization": "your-org",
    "Pat": "your-personal-access-token-here",
    "UsePat": true
  }
}
```

#### Features
- Simple setup for local development
- No Azure identity configuration required
- Direct authentication to Azure DevOps
- Works from any environment (local, CI/CD, etc.)

#### Prerequisites
- Valid Azure DevOps Personal Access Token
- PAT must have appropriate scopes for the operations you need

## Creating an Azure DevOps PAT

1. Sign in to your Azure DevOps organization
2. Navigate to **User Settings** → **Personal access tokens**
3. Click **+ New Token**
4. Configure the token:
   - **Name**: Give it a descriptive name (e.g., "Local Development")
   - **Organization**: Select your organization or "All accessible organizations"
   - **Expiration**: Choose an appropriate expiration date
   - **Scopes**: Select the required scopes:
     - **Project and Team**: Read, write, & manage
     - **Work Items**: Read, write, & manage
     - **Code**: Read
     - (Add other scopes as needed for your use case)
5. Click **Create**
6. **Important**: Copy the token immediately - you won't be able to see it again

## Environment-Specific Configuration

### Local Development

For local development, use environment variables or user secrets:

#### Using appsettings.Development.json

```json
{
  "AzureDevOps": {
    "Organization": "your-org",
    "Pat": "your-pat-here",
    "UsePat": true
  }
}
```

#### Using Environment Variables

```bash
# Linux/macOS
export AzureDevOps__UsePat=true
export AzureDevOps__Pat="your-pat-here"
export AzureDevOps__Organization="your-org"

# Windows (PowerShell)
$env:AzureDevOps__UsePat="true"
$env:AzureDevOps__Pat="your-pat-here"
$env:AzureDevOps__Organization="your-org"
```

#### Using .NET User Secrets

```bash
cd src/backend/dotnet/AzureDevOpsAI.Backend
dotnet user-secrets set "AzureDevOps:UsePat" "true"
dotnet user-secrets set "AzureDevOps:Pat" "your-pat-here"
dotnet user-secrets set "AzureDevOps:Organization" "your-org"
```

### Production/Azure Deployment

For production, use Azure managed identity:

```json
{
  "AzureDevOps": {
    "Organization": "your-org",
    "UsePat": false
  },
  "ManagedIdentityClientId": "your-managed-identity-client-id"
}
```

Store the managed identity client ID in:
- Azure App Configuration
- Azure Key Vault
- Environment variables in Azure Container Apps/App Service

## Security Best Practices

### For PAT Authentication

1. **Never commit PATs to source control**
   - Use `.gitignore` to exclude configuration files with secrets
   - Use environment variables or user secrets for local development
   - Use Azure Key Vault for deployed environments

2. **Use minimal scopes**
   - Only grant the permissions your application needs
   - Review and adjust scopes as requirements change

3. **Set appropriate expiration**
   - Use short-lived tokens for temporary access
   - Set up token rotation for long-term use

4. **Secure storage**
   - Store PATs in secure secret management systems
   - Use Azure Key Vault in production
   - Use .NET User Secrets in development

### For Azure Identity

1. **Use managed identity when possible**
   - Eliminates secret management overhead
   - Provides automatic credential rotation
   - Reduces attack surface

2. **Follow least privilege principle**
   - Grant only necessary permissions to the managed identity
   - Use Azure RBAC for fine-grained access control

3. **Use user-assigned managed identity for production**
   - Provides better control and reusability
   - Easier to manage across multiple resources

## Troubleshooting

### PAT Authentication Issues

**Problem**: "Personal Access Token cannot be null or empty when UsePat is true"
- **Solution**: Ensure `AzureDevOps:Pat` is configured when `UsePat` is `true`

**Problem**: API calls return 401 Unauthorized
- **Solution**: 
  - Verify the PAT is valid and not expired
  - Check that the PAT has the required scopes
  - Ensure the PAT is for the correct organization

### Azure Identity Issues

**Problem**: "DefaultAzureCredential failed to retrieve a token"
- **Solution**:
  - Verify managed identity is configured
  - Check that the managed identity has Azure DevOps permissions
  - For local development, ensure Azure CLI is logged in: `az login`

**Problem**: Token audience mismatch
- **Solution**: Ensure your managed identity has access to the Azure DevOps resource

## Switching Between Authentication Methods

You can switch between authentication methods by changing the `UsePat` configuration:

### Switch to PAT Authentication

```json
{
  "AzureDevOps": {
    "UsePat": true,
    "Pat": "your-pat-here"
  }
}
```

### Switch to Azure Identity

```json
{
  "AzureDevOps": {
    "UsePat": false
  }
}
```

No code changes are required - the service automatically adapts based on the configuration.

## Additional Resources

- [Azure DevOps Personal Access Tokens Documentation](https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate)
- [Azure Identity DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [Azure Managed Identities](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
