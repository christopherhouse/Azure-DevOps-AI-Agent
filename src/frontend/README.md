# Azure DevOps AI Agent Frontend

This is a [Next.js](https://nextjs.org) project that provides the web interface for the Azure DevOps AI Agent application. It features Microsoft Entra ID authentication and integrates with the backend API.

## Getting Started

### Local Development

First, install dependencies:

```bash
npm install
```

Copy the environment template and configure your environment variables:

```bash
cp .env.example .env.local
```

Then run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

### Docker Build

The frontend supports Docker builds with environment variable injection at build time:

```bash
# Basic build (uses runtime configuration)
docker build -t azure-devops-ai-agent-frontend .

# Build with environment variables baked in
docker build \
  --build-arg NEXT_PUBLIC_AZURE_TENANT_ID=your-tenant-id \
  --build-arg NEXT_PUBLIC_AZURE_CLIENT_ID=your-client-id \
  --build-arg NEXT_PUBLIC_BACKEND_URL=https://api.example.com \
  -t azure-devops-ai-agent-frontend .
```

For detailed Docker build options, see [Frontend Docker Build Args](../../docs/development/frontend-docker-build-args.md).

## Environment Variables

The application requires several `NEXT_PUBLIC_*` environment variables for proper configuration. See `.env.example` for the complete list.

### Required Variables

- `NEXT_PUBLIC_AZURE_TENANT_ID` - Azure tenant ID for authentication
- `NEXT_PUBLIC_AZURE_CLIENT_ID` - Azure client ID for authentication
- `NEXT_PUBLIC_BACKEND_URL` - Backend API URL

### Optional Variables

- `NEXT_PUBLIC_AZURE_AUTHORITY` - Azure authority URL
- `NEXT_PUBLIC_AZURE_REDIRECT_URI` - Azure redirect URI
- `NEXT_PUBLIC_FRONTEND_URL` - Frontend application URL
- `NEXT_PUBLIC_ENVIRONMENT` - Environment name
- `NEXT_PUBLIC_DEBUG` - Debug mode flag

## Configuration Approaches

The application supports two configuration approaches:

1. **Build-time Configuration**: Environment variables are baked into the JavaScript bundle during Docker build
2. **Runtime Configuration**: Environment variables are resolved at runtime (legacy approach with container secret references)

## Development

You can start editing the page by modifying `src/app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load fonts.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.
