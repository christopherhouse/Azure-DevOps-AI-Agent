# Azure DevOps AI Agent

An intelligent AI agent that provides administrative capabilities for Azure DevOps organizations and projects through a modern web interface. This solution combines a Next.js TypeScript frontend with a .NET Web API backend powered by Semantic Kernel and Azure OpenAI.

## 🎯 Overview

This repository contains a complete solution for automating Azure DevOps administrative tasks through natural language interactions. Users can manage projects, work items, repositories, and pipelines using conversational AI powered by Azure OpenAI.

### Key Features

- **Intelligent Chat Interface**: Modern web UI built with Next.js and React for natural language interactions
- **Azure DevOps Integration**: Comprehensive support for projects, work items, repositories, and pipelines
- **Enterprise Authentication**: Secure Microsoft Entra ID integration
- **Containerized Deployment**: Ready for Azure Container Apps with complete CI/CD pipeline
- **Production Ready**: Built with security, reliability, and observability in mind

### Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌───────────────────┐
│   Next.js UI    │    │  .NET Web API    │    │  Azure DevOps     │
│                 │    │                  │    │                   │
│ - Chat Interface│◄──►│ - Semantic Kernel│◄──►│ - Projects        │
│ - Entra ID Auth │    │ - Azure OpenAI   │    │ - Work Items      │
│ - Responsive UI │    │ - REST APIs      │    │ - Repositories    │
└─────────────────┘    └──────────────────┘    │ - Pipelines       │
                                                └───────────────────┘
```

## 🏗️ Repository Structure

```
├── .github/              # CI/CD workflows and templates
│   ├── workflows/        # GitHub Actions workflows
│   └── templates/        # Reusable workflow templates
├── docs/                 # Comprehensive documentation
│   ├── development/      # Development setup and guidelines
│   ├── architecture/     # System design and architecture
│   ├── authentication/   # Authentication configuration
│   └── api/             # API documentation
├── infra/                # Infrastructure as Code (Bicep)
│   ├── modules/          # Custom Bicep modules
│   ├── parameters/       # Environment-specific parameters
│   └── main.bicep        # Main infrastructure template
├── src/                  # Application source code
│   ├── frontend/         # Next.js TypeScript application
│   └── backend/          # .NET Web API application
└── README.md            # This file
```

## 🚀 Quick Start

### Prerequisites

- **Option 1 (Docker Compose - Recommended for Quick Start):**
  - Docker Desktop or Docker Engine (20.10.0+)
  - Docker Compose (2.0.0+)

- **Option 2 (Local Development):**
  - Node.js 18+ (for frontend)
  - .NET 8.0 SDK (for backend)
  - Azure subscription
  - Azure DevOps organization
  - Microsoft Entra ID tenant

### Option 1: Docker Compose (Recommended)

The fastest way to get started is using Docker Compose, which runs both frontend and backend containers locally:

1. **Clone the repository**
   ```bash
   git clone https://github.com/christopherhouse/Azure-DevOps-AI-Agent.git
   cd Azure-DevOps-AI-Agent
   ```

2. **Create environment files**
   ```bash
   # Copy example files
   cp .env.backend.example .env.backend
   cp .env.frontend.example .env.frontend
   
   # Edit files with your Azure credentials
   # At minimum, configure:
   # - Azure OpenAI endpoint and API key
   # - Azure DevOps organization and PAT
   # - Azure tenant ID and client IDs
   # - JWT secret key (generate with: openssl rand -hex 32)
   ```

3. **Start the application**
   ```bash
   docker compose up --build
   ```

4. **Access the application**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:8000
   - API Documentation: http://localhost:8000/docs (when auth disabled)

For detailed Docker Compose instructions, see [DOCKER_COMPOSE.md](DOCKER_COMPOSE.md).

### Option 2: Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/christopherhouse/Azure-DevOps-AI-Agent.git
   cd Azure-DevOps-AI-Agent
   ```

2. **Set up development environment**
   ```bash
   # Install backend dependencies (.NET)
   cd src/backend/dotnet/AzureDevOpsAI.Backend
   dotnet restore
   
   # Install frontend dependencies
   cd ../../../frontend
   npm install
   ```

3. **Configure environment variables**
   ```bash
   cp .env.example .env
   # Edit .env with your Azure and Azure DevOps configuration
   ```

4. **Run the development servers**
   ```bash
   # Terminal 1 - Backend API
   cd src/backend/dotnet/AzureDevOpsAI.Backend
   dotnet run

   # Terminal 2 - Frontend UI
   cd src/frontend
   npm run dev
   ```

5. **Access the application**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:8000
   - API Documentation: http://localhost:8000/docs

For detailed setup instructions, see [docs/development/setup.md](docs/development/setup.md).

## 🔧 Technology Stack

### Frontend
- **Next.js**: Modern React framework with TypeScript
- **React**: Component-based UI library
- **Tailwind CSS**: Utility-first CSS framework
- **Microsoft Authentication Library (MSAL)**: Entra ID integration
- **Application Insights**: Client-side telemetry and monitoring

### Backend
- **ASP.NET Core**: High-performance web API framework
- **C#/.NET 8.0**: Modern language and runtime
- **Semantic Kernel**: AI orchestration and plugin system
- **Azure OpenAI**: GPT models for natural language processing
- **Azure SDK**: Azure service integrations

### Infrastructure
- **Azure Container Apps**: Serverless container hosting
- **Azure Container Registry**: Container image management
- **Azure OpenAI**: AI model hosting
- **Application Insights**: Monitoring and telemetry
- **Azure Key Vault**: Secrets management
- **Bicep**: Infrastructure as Code

### DevOps
- **GitHub Actions**: CI/CD pipelines
- **ESLint**: JavaScript/TypeScript linting
- **Prettier**: Code formatting
- **Jest**: Testing framework for frontend
- **.NET Analyzers**: Code quality and security analysis (backend)
- **xUnit/NUnit**: Testing framework (backend)
- **Docker**: Containerization for both frontend and backend
- **Docker Compose**: Local multi-container orchestration

## 🔐 Security & Authentication

This application implements enterprise-grade security:

- **Microsoft Entra ID Integration**: Secure authentication for all users
- **Token Validation**: All API requests validated with proper RBAC
- **Encryption**: All data encrypted at rest and in transit
- **Managed Identity**: Azure resources accessed via managed identity
- **Secret Management**: All secrets stored in Azure Key Vault

For authentication setup instructions, see [docs/authentication/](docs/authentication/).

## 🏥 Azure DevOps Capabilities

The AI agent can perform the following Azure DevOps operations:

### Project Management
- Create and configure new projects
- Update project settings and permissions
- List and search existing projects
- Archive or delete projects

### Work Item Management
- Create work items (User Stories, Tasks, Bugs, etc.)
- Update work item fields and states
- Query work items with complex filters
- Manage work item relationships and hierarchies

### Repository Management
- Create and initialize repositories
- Manage repository permissions and policies
- Configure branch policies and pull request rules
- Handle repository settings and configurations

### Pipeline Management
- Create and configure build/release pipelines
- Trigger pipeline runs and monitor status
- Manage pipeline variables and environments
- Configure pipeline permissions and approvals

## 📊 Monitoring & Observability

The application includes comprehensive monitoring:

- **Application Insights**: Performance and usage telemetry
- **OpenTelemetry**: Distributed tracing across services
- **Health Checks**: Endpoint monitoring for all services
- **Logging**: Structured logging with correlation IDs
- **Metrics**: Custom metrics for business logic monitoring

## 🚢 Deployment

### Development Environment
- Automated deployment via GitHub Actions
- Containerized services in Azure Container Apps
- Managed with Bicep infrastructure templates

### Production Environment
- Blue-green deployment strategy
- Automated rollback capabilities
- Production-grade monitoring and alerting
- High availability configuration

For deployment instructions, see [docs/development/deployment.md](docs/development/deployment.md).

## 🧪 Testing

The project maintains high code quality with comprehensive testing:

- **Unit Tests**: 90%+ code coverage requirement
- **Integration Tests**: API endpoint and service testing
- **End-to-End Tests**: Complete user workflow validation
- **Security Tests**: Authentication and authorization verification

Run tests locally:
```bash
# Backend tests
cd src/backend/dotnet
dotnet test

# Frontend tests
cd src/frontend
npm test
```

## 📚 Documentation

Comprehensive documentation is available in the [docs/](docs/) directory:

- **[Development Guide](docs/development/)**: Setup, testing, and contribution guidelines
- **[Architecture](docs/architecture/)**: System design and technical decisions
- **[Authentication](docs/authentication/)**: Entra ID configuration and integration
- **[API Documentation](docs/api/)**: Complete API reference

## 🤝 Contributing

We welcome contributions! Please see our contribution guidelines:

1. **Fork the repository** and create a feature branch
2. **Follow code quality standards** (ruff, mypy, pytest all must pass)
3. **Add tests** for new functionality
4. **Update documentation** for significant changes
5. **Submit a pull request** with clear description

For detailed guidelines, see [.copilot-instructions.md](.copilot-instructions.md).

## 📋 Development Workflow

1. **Quality First**: All code must pass linting, type checking, and tests
2. **Security by Design**: Authentication and authorization in every feature
3. **Documentation**: Update docs with every significant change
4. **Testing**: Maintain 90%+ test coverage
5. **Infrastructure as Code**: All Azure resources managed via Bicep

## 🔗 Related Links

- [Azure DevOps REST API Documentation](https://docs.microsoft.com/en-us/rest/api/azure/devops/)
- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Azure OpenAI Service](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service)
- [Next.js Documentation](https://nextjs.org/docs)
- [React Documentation](https://react.dev/)
- [FastAPI Documentation](https://fastapi.tiangolo.com/)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:

- **Issues**: Create a GitHub issue for bugs or feature requests
- **Discussions**: Use GitHub discussions for general questions
- **Documentation**: Check the [docs/](docs/) directory for detailed guides

---

**Built with ❤️ using Azure, Python, and AI**