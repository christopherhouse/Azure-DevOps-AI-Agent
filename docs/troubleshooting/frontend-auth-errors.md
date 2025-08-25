# Frontend Configuration Troubleshooting

## Azure Authentication Error: AADSTS900144

### Problem
You might encounter the following error when attempting to login:

```
AADSTS900144: The request body must contain the following parameter: 'client_id'
```

### Cause
This error occurs when the required Azure environment variables are not properly configured in your frontend environment. The MSAL (Microsoft Authentication Library) receives an empty or undefined client ID, causing the authentication request to fail.

### Solution
Ensure the following environment variables are set in your frontend environment:

#### Required Environment Variables
- `NEXT_PUBLIC_AZURE_TENANT_ID` - Your Azure tenant ID
- `NEXT_PUBLIC_AZURE_CLIENT_ID` - Your Azure application client ID

#### Setup Steps
1. Copy the environment template:
   ```bash
   cp src/frontend/.env.example src/frontend/.env.local
   ```

2. Update the required values in `.env.local`:
   ```bash
   # Required Azure Environment Variables
   NEXT_PUBLIC_AZURE_TENANT_ID=your-actual-tenant-id
   NEXT_PUBLIC_AZURE_CLIENT_ID=your-actual-client-id
   ```

3. Restart your development server:
   ```bash
   cd src/frontend
   npm run dev
   ```

### Validation
The frontend configuration now includes validation that will throw a clear error message if required environment variables are missing:

```
Required environment variable NEXT_PUBLIC_AZURE_TENANT_ID is not set
```

This replaces the cryptic Azure authentication error with a clear indication of what needs to be configured.

### Getting Azure Credentials
See the [Entra ID Setup Guide](../authentication/entra-setup.md) for instructions on creating an Azure app registration and obtaining the required tenant and client IDs.