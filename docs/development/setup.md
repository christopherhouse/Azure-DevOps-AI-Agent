# Development Setup Guide

This guide provides step-by-step instructions for setting up a local development environment for the Azure DevOps AI Agent.

## Prerequisites

### Required Software
- **Python 3.11+**: Latest stable version recommended
- **Git**: For version control
- **Azure CLI**: For Azure resource management
- **Docker**: For containerization (optional for local development)
- **VS Code**: Recommended IDE with Python extension

### Azure Requirements
- **Azure Subscription**: With contributor access
- **Azure DevOps Organization**: With administrative permissions
- **Microsoft Entra ID Tenant**: For authentication setup

## Environment Setup

### 1. Clone and Navigate
```bash
git clone https://github.com/christopherhouse/Azure-DevOps-AI-Agent.git
cd Azure-DevOps-AI-Agent
```

### 2. Python Environment
```bash
# Create virtual environment
python -m venv venv

# Activate virtual environment
# Linux/macOS:
source venv/bin/activate
# Windows:
venv\Scripts\activate

# Upgrade pip
pip install --upgrade pip
```

### 3. Install Dependencies
```bash
# Install backend dependencies
cd src/backend
pip install -r requirements.txt
pip install -r requirements-dev.txt

# Install frontend dependencies
cd ../frontend
pip install -r requirements.txt

# Return to root
cd ../..
```

### 4. Environment Configuration
```bash
# Copy environment template
cp .env.example .env

# Edit .env file with your configuration
# Use your preferred editor
code .env
```

### 5. Required Environment Variables
```bash
# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-openai-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4

# Azure DevOps Configuration
AZURE_DEVOPS_ORGANIZATION=https://dev.azure.com/your-org
AZURE_DEVOPS_PAT=your-personal-access-token

# Microsoft Entra ID Configuration
AZURE_CLIENT_ID=your-client-id
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_SECRET=your-client-secret

# Application Configuration
BACKEND_URL=http://localhost:8000
FRONTEND_URL=http://localhost:7860
ENVIRONMENT=development
```

## Development Tools Setup

### 1. Install Development Tools
```bash
# Install pre-commit hooks
pip install pre-commit
pre-commit install

# Install quality tools globally
pip install ruff mypy pytest bandit
```

### 2. Configure VS Code (Recommended)
Create `.vscode/settings.json`:
```json
{
    "python.defaultInterpreterPath": "./venv/bin/python",
    "python.linting.enabled": true,
    "python.linting.ruffEnabled": true,
    "python.formatting.provider": "none",
    "[python]": {
        "editor.formatOnSave": true,
        "editor.codeActionsOnSave": {
            "source.organizeImports": true
        },
        "editor.defaultFormatter": "charliermarsh.ruff"
    },
    "python.testing.pytestEnabled": true,
    "python.testing.pytestArgs": [
        "tests"
    ]
}
```

### 3. Install VS Code Extensions
- Python
- Ruff
- Azure Tools
- Bicep
- GitHub Actions

## Running the Application

### 1. Start Backend API
```bash
cd src/backend
uvicorn app.main:app --reload --port 8000 --host 0.0.0.0
```

### 2. Start Frontend (New Terminal)
```bash
cd src/frontend
python app.py
```

### 3. Access Applications
- **Frontend UI**: http://localhost:7860
- **Backend API**: http://localhost:8000
- **API Documentation**: http://localhost:8000/docs
- **API Schema**: http://localhost:8000/openapi.json

## Development Workflow

### 1. Quality Checks
Run these commands before committing:

```bash
# Format code
ruff format .

# Lint code
ruff check .

# Type checking
mypy src/

# Security scan
bandit -r src/

# Run tests
pytest src/backend/tests/ --cov=src/backend/app
pytest src/frontend/tests/
```

### 2. Git Workflow
```bash
# Create feature branch
git checkout -b feature/your-feature-name

# Make changes and commit
git add .
git commit -m "feat: add your feature description"

# Push and create PR
git push origin feature/your-feature-name
```

### 3. Testing
```bash
# Run all tests
pytest

# Run with coverage
pytest --cov=src --cov-report=html

# Run specific test file
pytest src/backend/tests/test_api.py

# Run with verbose output
pytest -v
```

## Database Setup (If Applicable)

If using a local database for development:

```bash
# Start PostgreSQL container (example)
docker run -d \
  --name azure-devops-db \
  -e POSTGRES_PASSWORD=dev-password \
  -e POSTGRES_DB=azure_devops_agent \
  -p 5432:5432 \
  postgres:15

# Update .env with database connection
DATABASE_URL=postgresql://postgres:dev-password@localhost:5432/azure_devops_agent
```

## Azure Services Setup

### 1. Azure OpenAI
```bash
# Create Azure OpenAI resource
az cognitiveservices account create \
  --name your-openai-resource \
  --resource-group your-rg \
  --kind OpenAI \
  --sku S0 \
  --location eastus

# Deploy GPT-4 model
az cognitiveservices account deployment create \
  --name your-openai-resource \
  --resource-group your-rg \
  --deployment-name gpt-4 \
  --model-name gpt-4 \
  --model-version "0613" \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name Standard
```

### 2. Entra ID App Registration
```bash
# Create app registration
az ad app create \
  --display-name "Azure DevOps AI Agent" \
  --web-redirect-uris "http://localhost:7860/auth/callback" \
  --required-resource-accesses @manifest.json
```

## Troubleshooting

### Common Issues

1. **Port Already in Use**
   ```bash
   # Find process using port
   lsof -i :8000
   # Kill process
   kill -9 <PID>
   ```

2. **Python Path Issues**
   ```bash
   # Ensure you're in virtual environment
   which python
   # Should show venv path
   ```

3. **Import Errors**
   ```bash
   # Install in development mode
   cd src/backend
   pip install -e .
   ```

4. **Azure Authentication Errors**
   - Verify Azure CLI login: `az account show`
   - Check environment variables
   - Validate Entra ID app permissions

### Getting Help

- Check existing GitHub issues
- Review error logs in terminal
- Validate all environment variables
- Ensure all prerequisites are installed

## GitHub Actions Setup

For automated deployment and CI/CD pipelines, you'll need to configure GitHub repository secrets. The workflows use individual Azure credential parameters for enhanced security.

### Required GitHub Secrets

Configure these secrets in your GitHub repository (`Settings` → `Secrets and variables` → `Actions`):

#### Development Environment
- `AZURE_CLIENT_ID`: Application (client) ID from Azure App Registration
- `AZURE_TENANT_ID`: Directory (tenant) ID from Azure Active Directory  
- `AZURE_SUBSCRIPTION_ID`: Azure subscription ID for development resources

#### Production Environment (Optional)
- `AZURE_CLIENT_ID_PROD`: Production application (client) ID
- `AZURE_TENANT_ID_PROD`: Production directory (tenant) ID
- `AZURE_SUBSCRIPTION_ID_PROD`: Production subscription ID

### Service Principal Setup

Create a service principal with the required permissions:

```bash
# Create service principal
az ad sp create-for-rbac \
  --name "azure-devops-ai-agent-github" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth

# Note: Use the output values for GitHub secrets:
# - appId → AZURE_CLIENT_ID
# - password → AZURE_CLIENT_SECRET (if using creds format)
# - tenant → AZURE_TENANT_ID
```

### Workflow Authentication

The GitHub Actions workflows now use the recommended individual parameter format instead of the deprecated `creds` JSON format for improved security:

```yaml
- name: Azure Login
  uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

This approach provides:
- Enhanced security through individual parameter validation
- Better error messages when credentials are missing
- Compatibility with OpenID Connect (OIDC) authentication
- Consistent authentication across all workflow jobs

## Next Steps

After successful setup:

1. **Review Architecture**: Read [../architecture/overview.md](../architecture/overview.md)
2. **Authentication Setup**: Configure [../authentication/entra-setup.md](../authentication/entra-setup.md)
3. **API Documentation**: Explore [../api/endpoints.md](../api/endpoints.md)
4. **Testing Guide**: Learn about [testing.md](testing.md)

## Development Resources

- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [Semantic Kernel Python](https://learn.microsoft.com/en-us/semantic-kernel/get-started/quick-start-guide/python)
- [Gradio Documentation](https://gradio.app/docs/)
- [Azure DevOps REST API](https://docs.microsoft.com/en-us/rest/api/azure/devops/)
- [Ruff Configuration](https://docs.astral.sh/ruff/)