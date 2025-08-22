// Main infrastructure deployment template for Azure DevOps AI Agent
// This template uses Azure Verified Modules (AVM) for all supported resource types

targetScope = 'resourceGroup'

// Parameters
@description('Environment name (dev, prod)')
@allowed(['dev', 'prod'])
param environment string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Application name prefix')
param appNamePrefix string = 'azdo-ai-agent'

@description('Container Apps Environment name')
param containerAppsEnvironmentName string = '${appNamePrefix}-${environment}-env'

@description('Azure OpenAI resource name')
param openAIName string = '${appNamePrefix}-${environment}-openai'

@description('Container Registry name')
param containerRegistryName string = replace('${appNamePrefix}${environment}acr', '-', '')

@description('Key Vault name')
param keyVaultName string = '${appNamePrefix}-${environment}-kv'

@description('Application Insights name')
param applicationInsightsName string = '${appNamePrefix}-${environment}-ai'

@description('Log Analytics Workspace name')
param logAnalyticsName string = '${appNamePrefix}-${environment}-la'

@description('Backend Managed Identity name')
param backendManagedIdentityName string = '${appNamePrefix}-${environment}-be-mi'

@description('Frontend Managed Identity name')
param frontendManagedIdentityName string = '${appNamePrefix}-${environment}-fe-mi'

@description('Azure DevOps organization URL')
param azureDevOpsOrganization string

@description('Microsoft Entra ID tenant ID')
param entraIdTenantId string

@description('Microsoft Entra ID client ID')
param entraIdClientId string

@description('Microsoft Entra ID client secret')
@secure()
param entraIdClientSecret string = 'MOCK-CLIENT-SECRET-REPLACE-WITH-REAL-VALUE'

@description('Azure OpenAI API key')
@secure()
param azureOpenAIKey string = 'MOCK-OPENAI-KEY-REPLACE-WITH-REAL-VALUE'

@description('Tags to apply to all resources')
param tags object = {
  Environment: environment
  Application: 'Azure DevOps AI Agent'
  CreatedBy: 'Bicep'
}

// Variables
var resourceNames = {
  containerAppsEnvironment: containerAppsEnvironmentName
  openAI: openAIName
  containerRegistry: containerRegistryName
  keyVault: keyVaultName
  applicationInsights: applicationInsightsName
  logAnalytics: logAnalyticsName
  backendManagedIdentity: backendManagedIdentityName
  frontendManagedIdentity: frontendManagedIdentityName
}

var environmentConfig = {
  dev: {
    acrSku: 'Standard'
    openAISku: 'S0'
  }
  prod: {
    acrSku: 'Standard'
    openAISku: 'S0'
  }
}

var config = environmentConfig[environment]

// Backend Managed Identity using AVM
module backendManagedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.0' = {
  name: 'backend-managed-identity-${deployment().name}'
  params: {
    name: resourceNames.backendManagedIdentity
    location: location
    tags: tags
  }
}

// Frontend Managed Identity using AVM
module frontendManagedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.0' = {
  name: 'frontend-managed-identity-${deployment().name}'
  params: {
    name: resourceNames.frontendManagedIdentity
    location: location
    tags: tags
  }
}

// Log Analytics Workspace using AVM
module logAnalytics 'br/public:avm/res/operational-insights/workspace:0.11.1' = {
  name: 'log-analytics-workspace-${deployment().name}'
  params: {
    name: resourceNames.logAnalytics
    location: location
    tags: tags
    dataRetention: environment == 'prod' ? 90 : 30
  }
}

// Application Insights using AVM
module applicationInsights 'br/public:avm/res/insights/component:0.6.0' = {
  name: 'application-insights-${deployment().name}'
  params: {
    name: resourceNames.applicationInsights
    location: location
    tags: tags
    kind: 'web'
    workspaceResourceId: logAnalytics.outputs.resourceId
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    diagnosticSettings: [
      {
        name: 'default'
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
          }
        ]
        workspaceResourceId: logAnalytics.outputs.resourceId
      }
    ]
  }
}

// Azure Container Registry using AVM
module containerRegistry 'br/public:avm/res/container-registry/registry:0.9.1' = {
  name: 'container-registry-${deployment().name}'
  params: {
    name: resourceNames.containerRegistry
    location: location
    tags: tags
    acrSku: config.acrSku == 'Premium' ? 'Premium' : 'Standard'
    acrAdminUserEnabled: false
    networkRuleSetDefaultAction: 'Allow'
    quarantinePolicyStatus: 'disabled'
    retentionPolicyStatus: 'enabled'
    retentionPolicyDays: 30
    trustPolicyStatus: 'disabled'
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Disabled'
    managedIdentities: {
      userAssignedResourceIds: [
        backendManagedIdentity.outputs.resourceId
        frontendManagedIdentity.outputs.resourceId
      ]
    }
    diagnosticSettings: [
      {
        name: 'default'
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
          }
        ]
        workspaceResourceId: logAnalytics.outputs.resourceId
      }
    ]
  }
}

// Key Vault using AVM
module keyVault 'br/public:avm/res/key-vault/vault:0.12.1' = {
  name: 'key-vault-${deployment().name}'
  params: {
    name: resourceNames.keyVault
    location: location
    tags: tags
    sku: 'standard'
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: environment == 'prod'
    softDeleteRetentionInDays: 90
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    secrets: [
      {
        name: 'azure-openai-endpoint'
        value: openAI.outputs.endpoint
      }
      {
        name: 'azure-openai-key'
        value: azureOpenAIKey
      }
      {
        name: 'entra-tenant-id'
        value: entraIdTenantId
      }
      {
        name: 'entra-client-id'
        value: entraIdClientId
      }
      {
        name: 'entra-client-secret'
        value: entraIdClientSecret
      }
      {
        name: 'app-insights-connection-string'
        value: applicationInsights.outputs.connectionString
      }
    ]
    diagnosticSettings: [
      {
        name: 'default'
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
          }
        ]
        workspaceResourceId: logAnalytics.outputs.resourceId
      }
    ]
  }
}

// Azure OpenAI Service using AVM
module openAI 'br/public:avm/res/cognitive-services/account:0.10.1' = {
  name: 'cognitive-services-account-${deployment().name}'
  params: {
    name: resourceNames.openAI
    location: location
    tags: tags
    kind: 'AIServices'
    sku: config.openAISku
    customSubDomainName: resourceNames.openAI
    networkAcls: {
      defaultAction: 'Allow'
    }
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    managedIdentities: {
      userAssignedResourceIds: [
        backendManagedIdentity.outputs.resourceId
        frontendManagedIdentity.outputs.resourceId
      ]
    }
    deployments: [
      {
        name: 'gpt-4o'
        model: {
          format: 'OpenAI'
          name: 'gpt-4o'
          version: '2024-08-06'
        }
        raiPolicyName: 'Microsoft.DefaultV2'
        sku: {
          name: 'Standard'
          capacity: 10
        }
        versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
      }
    ]
    diagnosticSettings: [
      {
        name: 'default'
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
          }
        ]
        workspaceResourceId: logAnalytics.outputs.resourceId
      }
    ]
  }
}

// Container Apps Environment using AVM
module containerAppsEnvironment 'br/public:avm/res/app/managed-environment:0.10.0' = {
  name: 'managed-environment-${deployment().name}'
  params: {
    name: resourceNames.containerAppsEnvironment
    location: location
    tags: tags
    logAnalyticsWorkspaceResourceId: logAnalytics.outputs.resourceId
    zoneRedundant: environment == 'prod'
    publicNetworkAccess: 'Enabled'
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

// RBAC Assignments for Backend Managed Identity
resource backendAcrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, backendManagedIdentity.name, 'AcrPull')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull
    principalId: backendManagedIdentity.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

resource backendKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, backendManagedIdentity.name, 'KeyVaultSecretsUser')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: backendManagedIdentity.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

resource backendCognitiveServicesUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, backendManagedIdentity.name, 'CognitiveServicesUser')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908') // Cognitive Services User
    principalId: backendManagedIdentity.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC Assignments for Frontend Managed Identity
resource frontendAcrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, frontendManagedIdentity.name, 'AcrPull')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull
    principalId: frontendManagedIdentity.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

resource frontendKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, frontendManagedIdentity.name, 'KeyVaultSecretsUser')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: frontendManagedIdentity.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output containerAppsEnvironmentName string = containerAppsEnvironment.outputs.name
output containerAppsEnvironmentId string = containerAppsEnvironment.outputs.resourceId
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer
output keyVaultName string = keyVault.outputs.name
output applicationInsightsConnectionString string = applicationInsights.outputs.connectionString
output backendManagedIdentityClientId string = backendManagedIdentity.outputs.clientId
output frontendManagedIdentityClientId string = frontendManagedIdentity.outputs.clientId
output openAIEndpoint string = openAI.outputs.endpoint
