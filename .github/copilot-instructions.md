# Azure DevOps AI Agent - Copilot Instructions

This document provides comprehensive guidance for GitHub Copilot to work efficiently with the Azure DevOps AI Agent repository. Trust these instructions to minimize exploration time and avoid common build failures.

## 🎯 Repository Overview

**Azure DevOps AI Agent** is a conversational AI solution for Azure DevOps administrative tasks, built with:
- **Frontend**: Gradio web interface with Microsoft Entra ID authentication  
- **Backend**: FastAPI with Azure OpenAI and Semantic Kernel integration
- **Infrastructure**: Azure Container Apps deployed via Bicep and Azure Verified Modules (AVM)
- **Languages**: Python 3.11+ throughout, with comprehensive type hints and async/await patterns

**Repository Size**: ~500 source files across frontend, backend, infrastructure, and documentation

## 🔧 Critical Build Requirements

### Package Manager: uv (NOT pip)
**ALWAYS use `uv` instead of pip** - this is the most critical difference from typical Python projects:

```bash
# Install uv first
curl -LsSf https://astral.sh/uv/install.sh | sh

# Install dependencies (NEVER use pip install -r requirements.txt)
cd src/backend && uv sync --group dev
cd src/frontend && uv sync --group dev

# Run commands through uv
uv run pytest
uv run uvicorn app.main:app --reload
uv run ruff check app/
```

### Environment Setup
**ALWAYS copy .env.example before running tests or development servers:**
```bash
cp .env.example .env.test  # Required for test environment
cp .env.example .env       # Required for development
```

### Python Version
- **Required**: Python 3.11+
- **Target**: Python 3.11 (used in containers and CI)
- **Installed via uv**: Automatically downloads Python 3.11 if needed

## 📋 Development Workflow Commands

### Quality Checks (MUST pass before commits)
```bash
# Format code (run from project root)
cd src/backend && uv run ruff format app/
cd src/frontend && uv run ruff format .

# Lint code
cd src/backend && uv run ruff check app/
cd src/frontend && uv run ruff check .

# Type checking
cd src/backend && uv run mypy app --ignore-missing-imports

# Security scan
cd src/backend && uv run bandit -r app -f json -o bandit-report.json
```

### Testing
```bash
# Backend tests (from src/backend/)
uv run pytest tests/ --cov=app --cov-report=term-missing

# Frontend tests (from src/frontend/)  
uv run pytest tests/ --cov=. --cov-report=term-missing

# Coverage requirement: 25% minimum (currently ~72%)
```

### Running Development Servers
```bash
# Terminal 1 - Backend (from src/backend/)
uv run uvicorn app.main:app --reload --port 8000 --host 0.0.0.0

# Terminal 2 - Frontend (from src/frontend/)
uv run python app.py

# Access points:
# - Frontend UI: http://localhost:7860
# - Backend API: http://localhost:8000
# - API Docs: http://localhost:8000/docs
```

## 🏗️ Infrastructure Commands

### Bicep Validation (requires Azure CLI)
```bash
# Lint Bicep templates
az bicep lint --file infra/main.bicep

# Build to ARM template
az bicep build --file infra/main.bicep --outfile infra/main.json

# Validate against Azure (requires proper Azure credentials)
az deployment group validate \
  --resource-group rg-azure-devops-agent-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/main.dev.bicepparam
```

## 🏢 Project Architecture & Key Locations

### Directory Structure
```
├── .github/workflows/          # CI/CD pipelines
│   ├── ci.yml                 # Main build/test/publish pipeline  
│   ├── infrastructure.yml     # Azure infrastructure deployment
│   └── deploy.yml            # Application deployment
├── src/backend/               # FastAPI application
│   ├── app/main.py           # FastAPI app entry point
│   ├── app/api/              # API route modules
│   ├── app/services/         # Business logic services
│   ├── app/models/           # Pydantic models
│   └── tests/                # Backend tests
├── src/frontend/              # Gradio web application
│   ├── app.py                # Gradio app entry point
│   ├── components/           # UI components
│   └── tests/                # Frontend tests
├── infra/                     # Infrastructure as Code
│   ├── main.bicep            # Main Bicep template
│   ├── parameters/           # Environment-specific parameters
│   └── modules/              # Custom Bicep modules
└── docs/                      # Comprehensive documentation
    ├── development/          # Setup and development guides
    ├── architecture/         # System design
    └── api/                  # API documentation
```

### Configuration Files
- **Build**: `src/*/pyproject.toml` (contains ruff, mypy, pytest config)
- **Dependencies**: `src/*/uv.lock` (lockfiles), `src/*/pyproject.toml` (dependencies)
- **Environment**: `.env.example` (template), `.env.test` (test environment)
- **Security**: `.bandit` (security scan configuration)
- **Docker**: `src/*/Dockerfile` (multi-stage builds)

## 🔍 CI/CD Pipeline Details

### GitHub Actions Workflows
1. **ci.yml** (Main Pipeline - ~8-10 minutes):
   - Quality checks (ruff, mypy, bandit)
   - Backend tests with Redis service
   - Frontend tests
   - Container image builds
   - Security scanning with Trivy

2. **infrastructure.yml** (Infrastructure - ~5-7 minutes):
   - Bicep validation and linting
   - Checkov security scanning
   - Azure deployment (dev → prod)
   - Smoke testing

### Common CI Failures & Solutions
- **uv sync fails**: Check uv.lock files are committed
- **Tests fail with auth errors**: Ensure .env.test is properly configured  
- **Bandit security fails**: Review security scan output, add `# nosec` for false positives
- **Type checking fails**: Add missing type hints or mypy ignores
- **Coverage below 25%**: Add tests or adjust coverage threshold

## 🔒 Security & Authentication

### Authentication Flow
- **Frontend**: Microsoft Entra ID with PKCE flow
- **Backend**: JWT token validation with Azure identity
- **Services**: Managed Identity for Azure service access

### Secret Management
- **Development**: Environment variables in `.env` files
- **Production**: Azure Key Vault integration
- **Never commit**: Actual secrets, API keys, or connection strings

## 📦 Technology Stack Details

### Backend Dependencies (key modules)
- **fastapi**: Web framework with OpenAPI
- **azure-identity**: Azure authentication
- **azure-monitor-opentelemetry**: Telemetry and logging
- **structlog**: Structured logging
- **httpx**: Async HTTP client
- **pydantic**: Data validation and serialization

### Frontend Dependencies (key modules)  
- **gradio**: Web interface framework
- **msal**: Microsoft Authentication Library
- **httpx**: API client for backend communication
- **pandas**: Data manipulation for results display

### Infrastructure Components
- **Azure Container Apps**: Serverless container hosting
- **Azure OpenAI**: GPT models for AI functionality
- **Azure Container Registry**: Container image storage
- **Application Insights**: Monitoring and telemetry
- **Azure Key Vault**: Secret management

## ⚠️ Common Pitfalls & Solutions

### Build Issues
1. **"uv command not found"**: Install uv via curl script or package manager
2. **"No module named 'app'"**: Run commands from correct directory (src/backend/ or src/frontend/)
3. **"ImportError during tests"**: Ensure test environment has .env.test file
4. **"Coverage below threshold"**: Tests pass but coverage calculation may need adjustment

### Authentication Issues in Development
1. **Mock token errors**: Expected in fresh environments - tests need proper mocking setup
2. **Entra ID config**: Development requires actual Azure tenant configuration
3. **CORS errors**: Frontend/backend ports must match CORS configuration

### Infrastructure Issues
1. **Bicep warnings**: Parameters may be unused in templates (warnings, not errors)
2. **Azure CLI required**: Infrastructure validation needs Azure CLI installed
3. **Resource naming**: Container Registry names can't contain hyphens

## 📝 Code Quality Standards

### Python Code Requirements
- **Type hints**: Required on all functions and methods
- **Async patterns**: Use async/await for I/O operations
- **Pydantic models**: Required for API request/response validation
- **Error handling**: Comprehensive exception handling with logging
- **Documentation**: Docstrings for public methods and complex logic

### Testing Requirements
- **Coverage**: Minimum 25% (target 90% for backend, 70% for frontend)
- **Test organization**: Unit, integration, and e2e test separation
- **Fixtures**: Reusable test data and authentication mocking
- **Async testing**: Use pytest-asyncio for async code testing

## 🎯 Key Success Patterns

### When Adding New Features
1. Start with Pydantic models for data validation
2. Implement service layer with proper error handling
3. Add API endpoints with OpenAPI documentation  
4. Write tests with appropriate mocking
5. Update documentation and run quality checks

### When Fixing Issues
1. **ALWAYS** run quality checks first to understand current state
2. Make minimal changes to achieve the goal
3. Validate changes with tests and linting
4. Update related documentation if needed

### When Working with Infrastructure
1. Use Azure Verified Modules (AVM) when available
2. Test Bicep templates with `az bicep lint`
3. Use parameter files for environment-specific values
4. Include diagnostic settings and managed identity

## 🛠️ Available MCP Servers & External Tools

GitHub Copilot agents have access to several MCP (Model Context Protocol) servers that provide additional capabilities beyond code editing. Use these tools strategically to enhance development efficiency.

### Context7 Documentation Server
**Purpose**: Access up-to-date documentation for libraries and frameworks
**When to use**:
- Need current API documentation for dependencies (FastAPI, Gradio, Azure SDK, etc.)
- Implementing new features with unfamiliar libraries
- Debugging integration issues with external services
- Understanding best practices for specific technologies

**Usage pattern**:
```
1. context7-resolve-library-id: Find the correct library identifier
2. context7-get-library-docs: Retrieve focused documentation
```

**Example scenarios**:
- Adding new Azure OpenAI features: Get Context7 docs for `/azure/azure-sdk-for-python`
- Gradio UI improvements: Get Context7 docs for `/gradio-app/gradio`
- FastAPI endpoint patterns: Get Context7 docs for `/tiangolo/fastapi`

### GitHub MCP Server
**Purpose**: Comprehensive GitHub repository interaction and automation
**When to use**:
- Analyzing PR changes, issues, or workflow failures
- Understanding repository history and commit patterns
- Investigating CI/CD pipeline failures
- Reviewing code quality and security alerts

**Key capabilities**:
- **Repository exploration**: Search code, issues, PRs across GitHub
- **Workflow analysis**: Get job logs, artifact downloads, run details
- **Security scanning**: Access code scanning and secret scanning alerts
- **PR management**: Review files, comments, status checks

**Best practices**:
- Use `github-mcp-server-search_code` for finding implementation patterns
- Use `github-mcp-server-get_job_logs` with `failed_only=true` for CI debugging
- Use `github-mcp-server-list_workflow_runs` to understand deployment patterns

### Playwright Browser Server
**Purpose**: Web-based testing and debugging of the Gradio frontend
**When to use**:
- Testing authentication flows (Microsoft Entra ID integration)
- Debugging UI components and user interactions
- Validating responsive design and accessibility
- End-to-end testing scenarios

**Usage workflow**:
```
1. playwright-browser_navigate: Go to local development server
2. playwright-browser_snapshot: Capture accessibility tree for analysis
3. playwright-browser_click/type: Interact with UI elements
4. playwright-browser_take_screenshot: Document UI changes
```

### Tool Selection Guidelines

**For Documentation Tasks**:
- **First choice**: Context7 for current library documentation
- **Alternative**: GitHub search for repository-specific examples
- **Last resort**: Web browsing for official documentation

**For Debugging Tasks**:
- **CI/CD issues**: GitHub MCP server for workflow logs and run details
- **Authentication issues**: Playwright for testing auth flows
- **Integration issues**: Context7 for service-specific documentation

**For Development Tasks**:
- **New features**: Context7 for API patterns, GitHub search for existing implementations
- **Bug fixes**: GitHub MCP server for related issues/PRs, Context7 for troubleshooting guides
- **UI changes**: Playwright for testing, Context7 for component documentation

### Performance Considerations
- **Context7**: Lightweight, fast documentation retrieval
- **GitHub MCP**: Rate-limited, use focused queries
- **Playwright**: Resource-intensive, use for targeted testing only

**Avoid over-usage**: These tools supplement but don't replace understanding the codebase through file exploration and code analysis.

## 🔍 Trust These Instructions

**These instructions are comprehensive and tested.** Only search for additional information if:
- Instructions are incomplete for your specific task
- Build commands fail with errors not covered here  
- You need details about specific business logic or Azure service integration
- The available MCP servers don't provide the needed external information

Focus on following these patterns rather than exploring alternatives to minimize development time and avoid common pitfalls.