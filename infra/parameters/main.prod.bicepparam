// Production environment parameters for Azure DevOps AI Agent
using '../main.bicep'

// Environment configuration
param environment = 'prod'
param location = 'eastus2'
param appNamePrefix = 'azdo-ai-agent'

// Azure DevOps configuration
param azureDevOpsOrganization = 'https://dev.azure.com/your-prod-org'

// Microsoft Entra ID configuration
param entraIdTenantId = 'your-tenant-id-here'
param entraIdClientId = 'your-client-id-here'

// Resource naming
param containerAppsEnvironmentName = '${appNamePrefix}-${environment}-env'
param openAIName = '${appNamePrefix}-${environment}-openai'
param containerRegistryName = replace('${appNamePrefix}${environment}acr', '-', '')
param keyVaultName = '${appNamePrefix}-${environment}-kv'
param applicationInsightsName = '${appNamePrefix}-${environment}-ai'
param logAnalyticsName = '${appNamePrefix}-${environment}-la'
param backendManagedIdentityName = '${appNamePrefix}-${environment}-be-mi'
param frontendManagedIdentityName = '${appNamePrefix}-${environment}-fe-mi'

// Resource tags
param tags = {
  Environment: 'Production'
  Application: 'Azure DevOps AI Agent'
  Owner: 'Product Team'
  CostCenter: 'Engineering'
  CreatedBy: 'Bicep'
  Project: 'Azure DevOps AI Agent'
  BusinessUnit: 'Platform'
  DataClassification: 'Internal'
}

// Key Vault configuration
param enablePurgeProtection = true
param enableSoftDelete = true
param softDeleteRetentionInDays = 90
