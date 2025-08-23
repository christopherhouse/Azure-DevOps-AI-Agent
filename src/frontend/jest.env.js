// Set up environment variables for Jest tests
process.env.NODE_ENV = 'test';
process.env.NEXT_PUBLIC_AZURE_TENANT_ID = 'test-tenant-id';
process.env.NEXT_PUBLIC_AZURE_CLIENT_ID = 'test-client-id';
process.env.NEXT_PUBLIC_AZURE_AUTHORITY = 'https://login.microsoftonline.com/test-tenant-id';
process.env.NEXT_PUBLIC_AZURE_REDIRECT_URI = 'http://localhost:3000/auth/callback';
process.env.NEXT_PUBLIC_AZURE_SCOPES = 'openid,profile,User.Read';
process.env.NEXT_PUBLIC_FRONTEND_URL = 'http://localhost:3000';
process.env.NEXT_PUBLIC_BACKEND_URL = 'http://localhost:8000';
process.env.NEXT_PUBLIC_ENVIRONMENT = 'test';
process.env.NEXT_PUBLIC_DEBUG = 'true';
process.env.NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING = 'test-connection-string';
process.env.NEXT_PUBLIC_ENABLE_TELEMETRY = 'false';
process.env.NEXT_PUBLIC_SESSION_TIMEOUT = '3600';
process.env.NEXT_PUBLIC_REQUIRE_HTTPS = 'false';