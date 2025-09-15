# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY AzureDevOpsAI.Backend/AzureDevOpsAI.Backend.csproj AzureDevOpsAI.Backend/
COPY AzureDevOpsAI.sln .
RUN dotnet restore AzureDevOpsAI.Backend/AzureDevOpsAI.Backend.csproj

# Copy source code and build
COPY AzureDevOpsAI.Backend/ AzureDevOpsAI.Backend/
RUN dotnet publish AzureDevOpsAI.Backend/AzureDevOpsAI.Backend.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' --shell /bin/bash --uid 1001 appuser

# Copy published application
COPY --from=build /app/publish .

# Set ownership and permissions
RUN chown -R appuser:appuser /app
USER appuser

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8000/health || exit 1

# Expose port
EXPOSE 8000

# Start application
ENTRYPOINT ["dotnet", "AzureDevOpsAI.Backend.dll"]