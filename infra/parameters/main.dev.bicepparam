// Development environment parameters for Azure DevOps AI Agent
using '../main.bicep'

// Environment configuration
param environment = 'dev'
param location = 'eastus'
param appNamePrefix = 'azdo-ai-agent'

// Azure DevOps configuration
param azureDevOpsOrganization = 'https://dev.azure.com/your-dev-org'

// Microsoft Entra ID configuration
param entraIdTenantId = 'your-tenant-id-here'
param entraIdClientId = 'your-client-id-here'

// Container images (will be updated by CI/CD)
param frontendImage = 'nginx:latest'
param backendImage = 'nginx:latest'

// Resource naming
param containerAppsEnvironmentName = '${appNamePrefix}-${environment}-env'
param openAIName = '${appNamePrefix}-${environment}-openai'
param containerRegistryName = replace('${appNamePrefix}${environment}acr', '-', '')
param keyVaultName = '${appNamePrefix}-${environment}-kv'
param applicationInsightsName = '${appNamePrefix}-${environment}-ai'
param logAnalyticsName = '${appNamePrefix}-${environment}-la'
param managedIdentityName = '${appNamePrefix}-${environment}-mi'

// Resource tags
param tags = {
  Environment: 'Development'
  Application: 'Azure DevOps AI Agent'
  Owner: 'Development Team'
  CostCenter: 'Engineering'
  CreatedBy: 'Bicep'
  Project: 'Azure DevOps AI Agent'
}