# Azure DevOps AI Agent Frontend

A modern Gradio-based web interface for the Azure DevOps AI Agent, providing an intuitive chat interface for managing Azure DevOps projects, work items, repositories, and pipelines through natural language conversations.

## Features

- 🔐 **Microsoft Entra ID Authentication**: Secure authentication with enterprise identity
- 💬 **Intelligent Chat Interface**: Natural language interaction with AI agent
- 📊 **Application Insights Integration**: Comprehensive telemetry and monitoring
- 🎨 **Modern UI Design**: Professional, responsive interface built with Gradio
- 🚀 **Production Ready**: Containerized deployment with health checks
- 🔄 **Real-time Communication**: Async API communication with backend

## Architecture

```
Frontend (Gradio)
├── app.py                 # Main application entry point
├── config.py              # Configuration management
├── components/            # UI components
│   ├── auth.py           # Authentication components
│   └── chat.py           # Chat interface components
├── services/             # Business logic services
│   ├── auth_service.py   # Entra ID authentication
│   ├── api_client.py     # Backend API client
│   └── telemetry.py      # Application Insights integration
├── static/               # Static assets
│   ├── css/              # Custom styles
│   └── js/               # Client-side scripts
└── tests/                # Unit tests
```

## Quick Start

### Prerequisites

- Python 3.11+
- Microsoft Entra ID app registration
- Azure DevOps organization access
- Backend API running (optional for development)

### Installation

1. **Install dependencies**
   ```bash
   pip install -r requirements.txt
   ```

2. **Configure environment variables**
   ```bash
   cp .env.example .env
   # Edit .env with your configuration
   ```

3. **Run the application**
   ```bash
   python app.py
   ```

4. **Access the interface**
   - Open http://localhost:7860 in your browser
   - Authenticate with Microsoft Entra ID
   - Start chatting with the AI agent

## Configuration

### Required Environment Variables

```bash
# Microsoft Entra ID Configuration
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret

# Application URLs
FRONTEND_URL=http://localhost:7860
BACKEND_URL=http://localhost:8000

# Application Insights (Optional)
APPLICATIONINSIGHTS_CONNECTION_STRING=your-connection-string
ENABLE_TELEMETRY=true
```

### Optional Configuration

```bash
# Environment
ENVIRONMENT=development
DEBUG=true

# Authentication
AZURE_AUTHORITY=https://login.microsoftonline.com/your-tenant-id
AZURE_REDIRECT_URI=http://localhost:7860/auth/callback
AZURE_SCOPES=openid profile User.Read

# Security
SESSION_TIMEOUT=3600
REQUIRE_HTTPS=false

# CORS
CORS_ORIGINS=http://localhost:7860,http://127.0.0.1:7860
```

## Development

### Running Tests

```bash
# Install development dependencies
pip install -r requirements-dev.txt

# Run all tests
pytest

# Run with coverage
pytest --cov=. --cov-report=html

# Run specific test file
pytest tests/test_auth_service.py
```

### Code Quality

```bash
# Format code
ruff format .

# Lint code
ruff check .

# Type checking
mypy .

# Security scan
bandit -r .
```

### Docker Development

```bash
# Build container
docker build -t azure-devops-agent-frontend .

# Run container
docker run -p 7860:7860 \
  -e AZURE_TENANT_ID=your-tenant-id \
  -e AZURE_CLIENT_ID=your-client-id \
  -e AZURE_CLIENT_SECRET=your-client-secret \
  azure-devops-agent-frontend
```

## Authentication Flow

1. **User Access**: User navigates to frontend URL
2. **Authentication Check**: Application checks for valid session
3. **Login Redirect**: Unauthenticated users redirected to Entra ID
4. **Token Exchange**: Authorization code exchanged for access token
5. **Session Creation**: User session created with token
6. **API Communication**: All backend calls include Bearer token

## API Integration

The frontend communicates with the backend API using the `BackendAPIClient`:

```python
from services.api_client import BackendAPIClient

# Initialize with access token
client = BackendAPIClient(access_token)

# Send chat message
response = await client.send_chat_message("Create a new project")

# Get conversation history
conversations = await client.get_conversations()
```

## Telemetry

Application Insights integration provides:

- **Page Views**: Track user navigation
- **Custom Events**: User actions and interactions
- **Exceptions**: Error tracking and debugging
- **Performance**: Response times and load metrics
- **Dependencies**: API call tracking

Example telemetry usage:

```python
from services.telemetry import telemetry

# Track user action
telemetry.track_user_action("send_message", user_id="123")

# Track API call
telemetry.track_api_call("/chat/message", "POST", 200, 150.5)

# Track exception
try:
    # Some operation
    pass
except Exception as e:
    telemetry.track_exception(e)
```

## Production Deployment

### Container Configuration

```dockerfile
# Build for production
FROM python:3.11-slim
COPY . /app
WORKDIR /app
RUN pip install -r requirements.txt
EXPOSE 7860
CMD ["python", "app.py"]
```

### Environment Configuration

```yaml
# Azure Container Apps
environment:
  - name: ENVIRONMENT
    value: production
  - name: DEBUG
    value: false
  - name: AZURE_TENANT_ID
    secretRef: azure-tenant-id
  - name: AZURE_CLIENT_ID
    secretRef: azure-client-id
  - name: AZURE_CLIENT_SECRET
    secretRef: azure-client-secret
```

## Security Considerations

- **Authentication Required**: All endpoints require valid Entra ID token
- **Token Validation**: Backend validates all tokens before processing
- **HTTPS Enforcement**: Production deployments use HTTPS only
- **Session Management**: Secure session handling with timeout
- **CORS Configuration**: Restricted to allowed origins
- **Secret Management**: Environment variables for sensitive data

## Troubleshooting

### Common Issues

1. **Authentication Errors**
   - Verify Entra ID app registration
   - Check redirect URI configuration
   - Validate client ID and secret

2. **API Connection Issues**
   - Verify backend URL configuration
   - Check network connectivity
   - Validate backend API health

3. **Module Import Errors**
   - Ensure all dependencies are installed
   - Check Python path configuration
   - Verify virtual environment activation

### Debug Mode

Enable debug mode for detailed error information:

```bash
DEBUG=true python app.py
```

## Contributing

1. Follow existing code style and patterns
2. Add tests for new functionality
3. Update documentation for changes
4. Ensure all quality checks pass
5. Test authentication flow thoroughly

## Support

For support and questions:

- Check existing GitHub issues
- Review application logs
- Validate environment configuration
- Test authentication setup

---

**Built with ❤️ using Gradio, Python, and Azure**