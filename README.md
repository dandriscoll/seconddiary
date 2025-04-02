# Second Diary

A secure diary application with AI-powered recommendations.

## Features

- Encrypted diary entries
- User authentication with Azure AD
- AI-powered recommendations based on diary entries
- Daily email digests with personalized insights
- Responsive design for desktop and mobile devices
- Light and dark theme support

## Overview

Second Diary is a modern web application that allows users to:
- Securely log in using Microsoft Identity
- Create diary entries with thoughts and optional context
- View and manage personal diary entries
- Receive AI-generated insights based on journal content
- Experience a responsive UI that adapts to both desktop and mobile devices
- Automatically adjust to system light/dark theme preferences

## Tech Stack

- ASP.NET Core 9.0 API
- React with TypeScript
- Azure AD authentication
- Azure Cosmos DB
- Azure OpenAI Service
- Azure Communication Services

## Configuration

Create an `appsettings.Development.json` file based on the template with the following settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-domain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "CosmosDb": {
    "EndpointUrl": "your-cosmos-db-url",
    "PrimaryKey": "your-primary-key",
    "DatabaseName": "SecondDiary",
    "ContainerName": "DiaryEntries"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "your-deployment-name",
    "ModelName": "gpt-4",
    "ApiVersion": "2023-12-01-preview"
  },
  "CommunicationService": {
    "ConnectionString": "your-communication-service-connection-string",
    "SenderEmail": "donotreply@your-domain.com",
    "SenderName": "Second Diary Insights"
  },
  "FeatureFlags": {
    "DiaryAnalysis": true,
    "EmailNotifications": false
  },
  "AllowedHosts": "*",
  "CORS": {
    "AllowedOrigins": ["https://localhost:3000", "https://yourdomain.com"]
  }
}
```

## API Endpoints

### Authentication
- `GET /authentication/login` - Initiate login process
- `GET /authentication/logout` - Logout current user

### Diary Entries
- `GET /api/diaries` - Get all diary entries for the authenticated user
- `GET /api/diaries/{id}` - Get a specific diary entry
- `POST /api/diaries` - Create a new diary entry
- `PUT /api/diaries/{id}` - Update an existing diary entry
- `DELETE /api/diaries/{id}` - Delete a diary entry

### Insights
- `GET /api/insights` - Get AI-generated insights based on recent entries
- `GET /api/insights/preferences` - Get insight preferences
- `PUT /api/insights/preferences` - Update insight preferences

### User Profile
- `GET /api/profile` - Get current user's profile
- `PUT /api/profile` - Update profile settings

## Local Development Setup

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18 or higher)
- [npm](https://www.npmjs.com/) (v9 or higher)
- [Visual Studio Code](https://code.visualstudio.com/) or Visual Studio 2022

### Setting Up Local Environment

1. Clone the repository
   ```
   git clone https://github.com/yourusername/seconddiary.git
   cd seconddiary
   ```

2. Configure appsettings.Development.json as shown in the Configuration section

3. Install frontend dependencies:
   ```
   cd SecondDiary.API/ClientApp
   npm install
   ```

4. Run the application:
   ```
   cd SecondDiary.API
   dotnet run
   ```

5. The application will be available at `https://localhost:7126`

## Service Setup Instructions

### Azure AD Setup

1. Create an App Registration in Azure:
   - Navigate to the [Azure Portal](https://portal.azure.com)
   - Go to "Azure Active Directory" > "App registrations" > "New registration"
   - Enter a name for your application (e.g., "SecondDiary-Dev")
   - For "Supported account types", select "Accounts in this organizational directory only"
   - Set the Redirect URI: Select "Single-page application (SPA)" 
   - Enter `https://localhost:7126/authentication/login-callback` as the redirect URI
   - Click "Register"

2. Configure Authentication Settings:
   - In your app registration, go to "Authentication"
   - Add additional redirect URIs:
     - `https://localhost:7126/`
     - `https://localhost:7126/silent-refresh.html`
   - Ensure "Access tokens" and "ID tokens" are selected
   - Save your changes

3. Copy the "Application (client) ID" and "Directory (tenant) ID" to your appsettings.Development.json

### Azure OpenAI Setup

1. Create an Azure OpenAI resource:
   - Navigate to the Azure portal and search for "Azure OpenAI"
   - Click "Create" and complete the required fields
   - Select an appropriate region and pricing tier

2. Create a model deployment:
   - Navigate to Azure AI Studio (https://oai.azure.com/)
   - Select your OpenAI resource
   - Go to "Deployments" and click "Create new deployment"
   - Choose a model (e.g., GPT-4, GPT-3.5-Turbo)
   - Give your deployment a name (e.g., "diary-insights")
   - Configure your model version and other parameters

3. Get your configuration details from the "Keys and Endpoint" section in your Azure OpenAI resource:
   - Endpoint
   - API Key
   - Deployment Name

### Azure Cosmos DB Setup

1. Create a Cosmos DB account:
   - In the Azure Portal, search for "Cosmos DB"
   - Click "Create" and select "Core (SQL)" API
   - Configure account with appropriate settings
   - Create a database named "SecondDiary"
   - Create a container named "DiaryEntries" with partition key "/userId"

2. Get your connection string and primary key from the "Keys" section

### Azure Communication Services Setup

1. Create a Communication Service resource:
   - In the Azure portal, search for "Communication Services"
   - Click "Create" and configure as needed

2. Set up email services:
   - Configure a verified domain or use Azure managed domains
   - Set up your sender email address

3. Get your connection string from the "Keys" section for use in your application

## Deploying to Azure

### Prerequisites
- Azure subscription
- Azure CLI installed

### Deployment Steps

1. Create Azure resources:
   ```
   az login
   az group create --name SecondDiaryResourceGroup --location eastus
   az appservice plan create --name SecondDiaryPlan --resource-group SecondDiaryResourceGroup --sku F1
   az webapp create --name SecondDiary --resource-group SecondDiaryResourceGroup --plan SecondDiaryPlan --runtime "DOTNET|9.0"
   ```

2. Configure app settings (replace with your actual values):
   ```
   az webapp config appsettings set --name SecondDiary --resource-group SecondDiaryResourceGroup --settings "AzureAd:TenantId=your-tenant-id" "AzureAd:ClientId=your-client-id" "CosmosDb:EndpointUrl=your-cosmos-db-url" "CosmosDb:PrimaryKey=your-primary-key"
   ```

3. Publish the application:
   ```
   dotnet publish SecondDiary.API/SecondDiary.API.csproj -c Release -o ./publish
   az webapp deployment source config-zip --name SecondDiary --resource-group SecondDiaryResourceGroup --src ./publish.zip
   ```

## Troubleshooting

- **Authentication Issues**: Verify your Azure AD configuration and redirect URLs
- **Database Connection**: Check your Cosmos DB connection string and permissions
- **AI Integration**: Ensure your Azure OpenAI API keys and deployment names are correct
- **Email Notifications**: Verify your Communication Services connection string and sender email
