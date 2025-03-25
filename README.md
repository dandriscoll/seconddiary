# Second Diary

A secure personal diary application built with ASP.NET Core and React, with Microsoft identity integration.

## Overview

Second Diary is a modern web application that allows users to:
- Securely log in using Microsoft Identity
- Create diary entries with thoughts and optional context
- View and manage personal diary entries
- Experience a responsive UI that adapts to both desktop and mobile devices
- Automatically adjust to system light/dark theme preferences

## Tech Stack

- **Backend**: ASP.NET Core 9.0 API
- **Frontend**: React with TypeScript
- **Authentication**: Microsoft Identity / Azure AD
- **Database**: Azure Cosmos DB
- **Styling**: LESS CSS preprocessor

## Local Development Setup

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v16 or higher)
- [npm](https://www.npmjs.com/) (v8 or higher)
- [Visual Studio Code](https://code.visualstudio.com/)

### Configuration

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/seconddiary.git
   cd seconddiary
   ```

2. Set up app configuration:
   - Create an `appsettings.Development.json` file in the SecondDiary.API directory
   - Configure the following settings:

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
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id"
     },
     "CosmosDb": {
       "EndpointUrl": "your-cosmos-db-url",
       "PrimaryKey": "your-primary-key",
       "DatabaseName": "SecondDiary",
       "ContainerName": "DiaryEntries"
     },
     "Authentication": {
       "Microsoft": {
         "ClientId": "your-microsoft-client-id",
         "ClientSecret": "your-microsoft-client-secret"
       }
     }
   }
   ```

3. Install frontend dependencies:
   ```
   cd SecondDiary.API/ClientApp
   npm install
   ```

### Azure AD App Registration

1. **Create an App Registration in Azure**:
   - Navigate to the [Azure Portal](https://portal.azure.com)
   - Go to "Azure Active Directory" > "App registrations" > "New registration"
   - Enter a name for your application (e.g., "SecondDiary-Dev")
   - For "Supported account types", select "Personal Microsoft accounts"
   - Set the Redirect URI: Select "Single-page application (SPA)" from the platform dropdown
   - Enter `https://localhost:5001/` as the redirect URI, *changing the port to the one chosen for you by dotnet run
   - Click "Register"

2. **Configure Authentication Settings**:
   - In your app registration, go to "Authentication"
   - Under the "Single-page application" platform, add additional redirect URIs:
     - `https://localhost:5001/authentication/login-callback`
     - `https://localhost:5001/silent-refresh.html`
   - Ensure "Access tokens" and "ID tokens" are selected under the "Implicit grant and hybrid flows" section
   - Save your changes

3. **Get Application (Client) ID and Tenant ID**:
   - From the app registration Overview page, copy the "Application (client) ID" and "Directory (tenant) ID"
   - Add these values to your `appsettings.Development.json` file in the AzureAd section

4. **Create a Client Secret (if needed for server-side API calls)**:
   - Go to "Certificates & secrets" > "Client secrets" > "New client secret"
   - Add a description and select an expiration period
   - Copy the secret value immediately (you won't be able to see it again)
   - Add this value to your `appsettings.Development.json` file in the Authentication:Microsoft:ClientSecret section

### Running the Application

#### From Visual Studio:
1. Open the solution file `SecondDiary.sln`
2. Set `SecondDiary.API` as the startup project
3. Press F5 to run the application

#### From Command Line:
1. Navigate to the API project:
   ```
   cd SecondDiary.API
   ```
2. Run the application:
   ```
   dotnet run
   ```
3. The application will be available at `https://localhost:5001`

## Publishing to Azure

### Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- Azure Subscription
- Azure Cosmos DB instance
- Azure App Service plan

### Deployment Steps

1. **Create Azure Resources**:

   ```bash
   # Login to Azure
   az login

   # Create Resource Group
   az group create --name SecondDiaryResourceGroup --location eastus

   # Create App Service Plan
   az appservice plan create --name SecondDiaryPlan --resource-group SecondDiaryResourceGroup --sku F1

   # Create Web App
   az webapp create --name SecondDiary --resource-group SecondDiaryResourceGroup --plan SecondDiaryPlan
   ```

2. **Configure App Settings**:

   ```bash
   # Add application settings from your local configuration
   az webapp config appsettings set --name SecondDiary --resource-group SecondDiaryResourceGroup --settings "AzureAd:TenantId=your-tenant-id" "AzureAd:ClientId=your-client-id" "CosmosDb:EndpointUrl=your-cosmos-db-url" "CosmosDb:PrimaryKey=your-primary-key"
   ```

3. **Publish the Application**:

   From Visual Studio:
   - Right-click on the SecondDiary.API project
   - Select "Publish"
   - Choose "Azure" as the target
   - Select your existing App Service

   From Command Line:
   ```bash
   # Publish the app
   dotnet publish SecondDiary.API/SecondDiary.API.csproj -c Release -o ./publish

   # Deploy to Azure
   az webapp deployment source config-zip --name SecondDiary --resource-group SecondDiaryResourceGroup --src ./publish.zip
   ```

4. **Configure Authentication**:
   - In the Azure Portal, navigate to your App Service
   - Go to "Authentication" under Settings
   - Add Microsoft identity provider
   - Configure the provider with your client ID and secret

5. **Verify Deployment**:
   - Navigate to `https://seconddiary.azurewebsites.net` (replace with your actual app name)
   - Ensure you can log in and use the application

## Troubleshooting

- **Build Issues**: Ensure all NuGet packages are restored and npm packages are installed
- **Authentication Issues**: Verify your Microsoft Identity configuration and redirect URLs
- **CORS Issues**: Ensure your Azure App Service allows the correct origins
- **Database Issues**: Check your Cosmos DB connection string and permissions

## License

This project is licensed under the MIT License - see the LICENSE file for details.
