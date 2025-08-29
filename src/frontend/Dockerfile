# Multi-stage Dockerfile for Next.js frontend
FROM node:20-alpine AS base

# Install dependencies only when needed
FROM base AS deps
# Check https://github.com/nodejs/docker-node/tree/b4117f9333da4138b03a546ec926ef50a31506c3#nodealpine to understand why libc6-compat might be needed.
RUN apk add --no-cache libc6-compat
WORKDIR /app

# Install dependencies based on the preferred package manager
COPY package.json package-lock.json* ./
RUN npm ci --only=production

# Install dev dependencies for build stage
FROM base AS deps-build
RUN apk add --no-cache libc6-compat
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci

# Rebuild the source code only when needed
FROM base AS builder
WORKDIR /app
COPY --from=deps-build /app/node_modules ./node_modules
COPY . .

# Build args for NEXT_PUBLIC_ environment variables
# These can be passed at build time to bake values into the application
ARG NEXT_PUBLIC_AZURE_TENANT_ID
ARG NEXT_PUBLIC_AZURE_CLIENT_ID
ARG NEXT_PUBLIC_AZURE_AUTHORITY
ARG NEXT_PUBLIC_AZURE_REDIRECT_URI
ARG NEXT_PUBLIC_AZURE_SCOPES
ARG NEXT_PUBLIC_BACKEND_URL
ARG NEXT_PUBLIC_FRONTEND_URL
ARG NEXT_PUBLIC_ENVIRONMENT
ARG NEXT_PUBLIC_DEBUG
ARG NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING
ARG NEXT_PUBLIC_ENABLE_TELEMETRY
ARG NEXT_PUBLIC_SESSION_TIMEOUT
ARG NEXT_PUBLIC_REQUIRE_HTTPS

# Set environment variables from build args
# This ensures NEXT_PUBLIC_ vars are available during build and get inlined
ENV NEXT_PUBLIC_AZURE_TENANT_ID=$NEXT_PUBLIC_AZURE_TENANT_ID
ENV NEXT_PUBLIC_AZURE_CLIENT_ID=$NEXT_PUBLIC_AZURE_CLIENT_ID
ENV NEXT_PUBLIC_AZURE_AUTHORITY=$NEXT_PUBLIC_AZURE_AUTHORITY
ENV NEXT_PUBLIC_AZURE_REDIRECT_URI=$NEXT_PUBLIC_AZURE_REDIRECT_URI
ENV NEXT_PUBLIC_AZURE_SCOPES=$NEXT_PUBLIC_AZURE_SCOPES
ENV NEXT_PUBLIC_BACKEND_URL=$NEXT_PUBLIC_BACKEND_URL
ENV NEXT_PUBLIC_FRONTEND_URL=$NEXT_PUBLIC_FRONTEND_URL
ENV NEXT_PUBLIC_ENVIRONMENT=$NEXT_PUBLIC_ENVIRONMENT
ENV NEXT_PUBLIC_DEBUG=$NEXT_PUBLIC_DEBUG
ENV NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING=$NEXT_PUBLIC_APPLICATIONINSIGHTS_CONNECTION_STRING
ENV NEXT_PUBLIC_ENABLE_TELEMETRY=$NEXT_PUBLIC_ENABLE_TELEMETRY
ENV NEXT_PUBLIC_SESSION_TIMEOUT=$NEXT_PUBLIC_SESSION_TIMEOUT
ENV NEXT_PUBLIC_REQUIRE_HTTPS=$NEXT_PUBLIC_REQUIRE_HTTPS

ENV NEXT_TELEMETRY_DISABLED=1

# Build the application
RUN npm run build

# Production image, copy all the files and run next
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Create a non-root user
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Copy the public folder
COPY --from=builder /app/public ./public

# Set the correct permission for prerender cache
RUN mkdir .next
RUN chown nextjs:nodejs .next

# Automatically leverage output traces to reduce image size
# https://nextjs.org/docs/advanced-features/output-file-tracing
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

# Metadata labels
ARG BUILD_DATE
ARG VERSION=1.0.0
ARG VCS_REF

LABEL org.opencontainers.image.title="Azure DevOps AI Agent Frontend"
LABEL org.opencontainers.image.description="Next.js web interface for Azure DevOps AI Agent"
LABEL org.opencontainers.image.version=${VERSION}
LABEL org.opencontainers.image.created=${BUILD_DATE}
LABEL org.opencontainers.image.source="https://github.com/christopherhouse/Azure-DevOps-AI-Agent"
LABEL org.opencontainers.image.revision=${VCS_REF}
LABEL org.opencontainers.image.vendor="Microsoft"
LABEL org.opencontainers.image.authors="Christopher House"

USER nextjs

EXPOSE 3000

ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget -q --spider http://0.0.0.0:3000/api/health || exit 1

CMD ["node", "server.js"]