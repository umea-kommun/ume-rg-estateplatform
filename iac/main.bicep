// ------------ Parameters ------------
@allowed([
  'dev'
  'test'
  'prod'
])
@description('The current environment.')
param environment string

@description('The company prefix to use in resource names.')
param companyPrefix string

@description('The purpose of this deployment to use in resource names.')
param purpose string

@description('The default location of resources.')
param location string

@description('Azure Devops Organization.')
param organization string

@description('Azure Devops Project.')
param project string

@description('Personal Access Token for Azure DevOps.')
param personalAccessToken string

@description('The current date and time in UTC format.')
param dateNowUtc string = utcNow()

// ------------ Variables ------------
var resourceGroupType = 'rg'
var resourceGroupTags = {
  Environment: environment
  LastUpdated: dateNowUtc
}
var resourceGroupName = '${companyPrefix}-${resourceGroupType}-${purpose}-${environment}'

var databasePurposes = {
  estateservice: 'estateservice'
}
var roleDefinitionIds = {
  cognitiveServicesOpenAIUser: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
  foundryUser: '53ca6127-db72-4b80-b1b0-d745d6d5456d'
}

// ------------ Functions ------------
func getKeyVaultAssignee(principalType string, principalId string) object => {
  principalId: principalId
  principalType: principalType
}

// ------------ Dependencies ------------
// Resource Group - General
resource dependency_resourceGroup_general 'Microsoft.Resources/resourceGroups@2025-04-01' existing = {
  name: '${companyPrefix}-${resourceGroupType}-general-${environment}'
}

// Application Insights - General
resource dependency_applicationInsights_general 'Microsoft.Insights/components@2020-02-02' existing = {
  scope: dependency_resourceGroup_general
  name: '${companyPrefix}-appi-general-${environment}'
}

// AI Foundry - General (centralized AI resource, deployed by ume-rg-general)
resource dependency_aiFoundry_general 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  scope: dependency_resourceGroup_general
  name: '${companyPrefix}-aif-general-${environment}'
}

// ------------ Resources ------------
targetScope = 'subscription'

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
  tags: resourceGroupTags
}

// Default Role Assignments
module defaultRoleAssignments 'br/ume:umea.roleassignments.turkos.defaults:v2.0' = {
  scope: resourceGroup
  name: 'defaultRoleAssignments'
  params: {
    environment: environment
  }
}

// Role Assignment - Cognitive Services OpenAI User: estateservice (on the centralized AI Foundry resource group)
module roleAssignment_csoaiu_estateservice 'br/ume:microsoft.authorization.roleassignments:v2.2' = {
  scope: dependency_resourceGroup_general
  name: 'estateplatform_roleAssignment_csoaiu_estateservice'
  params: {
    roleDefinitionId: roleDefinitionIds.cognitiveServicesOpenAIUser
    assignees: [
      {
        principalId: app_estateservice.outputs.principalId
        principalType: 'ServicePrincipal'
      }
      {
        principalId: app_estateservice.outputs.?stageDeploymentSlot.principalId!
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

// Role Assignment - Foundry User: estateservice (on the centralized AI Foundry resource group)
module roleAssignment_fu_estateservice 'br/ume:microsoft.authorization.roleassignments:v2.2' = {
  scope: dependency_resourceGroup_general
  name: 'estateplatform_roleAssignment_fu_estateservice'
  params: {
    roleDefinitionId: roleDefinitionIds.foundryUser
    assignees: [
      {
        principalId: app_estateservice.outputs.principalId
        principalType: 'ServicePrincipal'
      }
      {
        principalId: app_estateservice.outputs.?stageDeploymentSlot.principalId!
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

// Key Vault
module keyVault 'br/ume:microsoft.keyvault.vaults:v2.1' = {
  scope: resourceGroup
  name: 'keyVault'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: purpose
    dateNowUtc: dateNowUtc

    permissions: [
      {
        role: 'Key Vault User'
        assignees: [
          getKeyVaultAssignee('ServicePrincipal', app_estateservice.outputs.principalId)
          getKeyVaultAssignee('ServicePrincipal', app_estateservice.outputs.?stageDeploymentSlot.principalId!)
        ]
      }
    ]
  }
}

// Static Web App - EstatePlatform
module staticWebApp_estateplatform 'br/ume:microsoft.web.staticsites:v2.3' = {
  scope: resourceGroup
  name: 'staticWebApp_estateplatform'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    location: 'westeurope' // Remove when available in swedencentral. Static Web Apps in Sweden at https://azure.microsoft.com/en-us/explore/global-infrastructure/products-by-region/table
    purpose: purpose
    dateNowUtc: dateNowUtc
  }
}

// App Service Plan
module appServicePlan 'br/ume:microsoft.web.serverfarms:v2.3' = {
  scope: resourceGroup
  name: 'appServicePlan'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: purpose
    dateNowUtc: dateNowUtc

    skuName: environment == 'prod' ? 'P0V4' : 'P0V4' // TODO: Prod should use P1V4, it is currently unavailable - 2026-03-18
  }
}

// App Service - EstateService
module app_estateservice 'br/ume:microsoft.web.sites:v2.3' = {
  scope: resourceGroup
  name: 'app_estateservice'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: 'estateservice'
    dateNowUtc: dateNowUtc

    appServicePlanId: appServicePlan.outputs.id
    withStageDeploymentSlot: true
  }
}

// Storage Account - Image Cache
module storageAccount 'br/ume:microsoft.storage.storageaccounts:v2.1' = {
  scope: resourceGroup
  name: 'storageAccount'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: 'estateplatform'
    dateNowUtc: dateNowUtc

    containers: [
      {
        purpose: 'imagecache'
      }
    ]
    contributors: [
      {
        principalId: app_estateservice.outputs.principalId
        principalType: 'ServicePrincipal'
      }
      {
        principalId: app_estateservice.outputs.?stageDeploymentSlot.principalId!
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

// SQL Server
module sqlServer 'br/ume:microsoft.sql.servers:v2.1' = {
  scope: resourceGroup
  name: 'sqlServer'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: purpose
    dateNowUtc: dateNowUtc

    databases: [
      {
        purpose: databasePurposes.estateservice
        users: [
          app_estateservice.outputs.name
          app_estateservice.outputs.?stageDeploymentSlot.principalName!
        ]
      }
    ]
  }
}

// Library variables
module libraryVariables 'library-variable-group.bicep' = {
  scope: resourceGroup
  name: 'libraryVariables'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    variableGroupPurpose: purpose
    dateNowUtc: dateNowUtc

    sqlServerName: sqlServer.outputs.name
    estateserviceSqldbName: sqlServer.outputs.databases.estateservice.name

    openaiEndpoint: dependency_aiFoundry_general.properties.endpoint
    applicationInsightsConnectionString: dependency_applicationInsights_general.properties.ConnectionString
    estateplatformName: staticWebApp_estateplatform.outputs.name

    personalAccessToken: personalAccessToken
    organization: organization
    project: project
  }
}

// ------------ Outputs ------------
output sqlConfiguration object = {
  serverName: sqlServer.outputs.name
  serverPrincipalId: sqlServer.outputs.principalId
  serverFullyQualifiedDomainName: sqlServer.outputs.fullyQualifiedDomainName
  databases: sqlServer.outputs.databases
}
