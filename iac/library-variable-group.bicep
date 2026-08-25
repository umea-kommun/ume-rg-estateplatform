// ------------ Parameters ------------
@allowed([
  'dev'
  'test'
  'prod'
])
param environment string
param companyPrefix string
param location string = resourceGroup().location
param variableGroupPurpose string
param tags object = {}
param dateNowUtc string = utcNow()

param sqlServerName string
param estateserviceSqldbName string

param openaiEndpoint string
param applicationInsightsConnectionString string
param estateplatformName string

param personalAccessToken string
param organization string
param project string

// ------------ Variables ------------
var libraryVariables = [
  {
    name: 'application-insights-connection-string'
    value: applicationInsightsConnectionString
  }
  {
    name: 'pythagoras-api-key'
    value: ''
  }
  {
    name: 'pythagoras-api-url'
    value: ''
  }
  {
    name: 'estateservice-database-connection-string'
    value: 'Server=tcp:${sqlServerName}${az.environment().suffixes.sqlServerHostname},1433;Initial Catalog=${estateserviceSqldbName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication="Active Directory Default";'
  }
  {
    name: 'estateservice-api-key'
    value: ''
  }
  {
    name: 'estateplatform-deployment-token'
    value: staticWebApp_estateplatform.listSecrets().properties.apiKey
  }
  {
    name: 'openai-endpoint'
    value: openaiEndpoint
  }
]

// ------------ Resources ------------
// Static Web App - EstatePlatform
resource staticWebApp_estateplatform 'Microsoft.Web/staticSites@2025-03-01' existing = {
  name: estateplatformName
}

// Deployment Script - Library Variables
module libraryVariablesDeploymentScript 'br/ume:umea.deploymentscripts.libraryvariables:v2.4' = {
  name: 'libraryVariablesDeploymentScript'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    location: location
    variableGroupPurpose: variableGroupPurpose
    tags: tags
    dateNowUtc: dateNowUtc

    personalAccessToken: personalAccessToken
    organization: organization
    project: project
    libraryVariables: libraryVariables
  }
}
