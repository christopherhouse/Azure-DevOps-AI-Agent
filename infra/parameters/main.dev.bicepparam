// Development environment parameters for Azure DevOps AI Agent
using '../main.bicep'

// Environment configuration
param environment = 'dev'
param location = 'eastus2'
param appNamePrefix = 'azdo-ai-agent'

// Azure DevOps configuration
param azureDevOpsOrganization = 'https://dev.azure.com/your-dev-org'

// Microsoft Entra ID configuration
param entraIdTenantId = 'your-tenant-id-here'
param entraIdClientId = 'your-client-id-here'

// Resource naming
param containerAppsEnvironmentName = '${appNamePrefix}-${environment}-env'
param openAIName = '${appNamePrefix}-${environment}-oai'
param containerRegistryName = replace('${appNamePrefix}${environment}acr', '-', '')
param keyVaultName = '${appNamePrefix}-${environment}-kv'
param applicationInsightsName = '${appNamePrefix}-${environment}-ai'
param logAnalyticsName = '${appNamePrefix}-${environment}-la'
param backendManagedIdentityName = '${appNamePrefix}-${environment}-be-mi'
param frontendManagedIdentityName = '${appNamePrefix}-${environment}-fe-mi'

// Resource tags
param tags = {
  Environment: 'Development'
  Application: 'Azure DevOps AI Agent'
  Owner: 'Development Team'
  CostCenter: 'Engineering'
  CreatedBy: 'Bicep'
  Project: 'Azure DevOps AI Agent'
}

// Key Vault configuration
param enablePurgeProtection = false
param enableSoftDelete = true
param softDeleteRetentionInDays = 90
