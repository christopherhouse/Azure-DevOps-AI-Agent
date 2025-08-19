# Testing Guidelines

This document outlines the testing strategy and guidelines for the Azure DevOps AI Agent project.

## Testing Philosophy

We maintain high code quality through comprehensive testing with the following principles:

- **90%+ Code Coverage**: All production code must be thoroughly tested
- **Test-Driven Development**: Write tests before implementation when possible
- **Fast Feedback**: Tests should run quickly to enable rapid development
- **Realistic Scenarios**: Tests should reflect real-world usage patterns
- **Isolation**: Tests should be independent and not rely on external dependencies

## Testing Framework

### Primary Tools
- **pytest**: Main testing framework
- **pytest-asyncio**: For testing async code
- **pytest-cov**: Code coverage reporting
- **pytest-mock**: Mocking and patching
- **httpx**: HTTP client testing
- **factory-boy**: Test data generation

### Test Categories

#### 1. Unit Tests
Test individual functions, classes, and methods in isolation.

```python
# Example unit test
import pytest
from app.services.azure_devops import AzureDevOpsService

@pytest.mark.asyncio
async def test_create_work_item():
    service = AzureDevOpsService()
    work_item = await service.create_work_item(
        project="TestProject",
        work_item_type="Task",
        title="Test Task"
    )
    assert work_item.title == "Test Task"
    assert work_item.work_item_type == "Task"
```

#### 2. Integration Tests
Test interactions between components and external services.

```python
# Example integration test
@pytest.mark.integration
async def test_api_create_project(client):
    response = await client.post(
        "/api/projects",
        json={"name": "Test Project", "description": "Test Description"}
    )
    assert response.status_code == 201
    assert response.json()["name"] == "Test Project"
```

#### 3. End-to-End Tests
Test complete user workflows through the full application stack.

```python
# Example E2E test
@pytest.mark.e2e
async def test_complete_project_creation_workflow(browser):
    # Login to application
    await browser.goto("http://localhost:7860")
    await browser.fill("#username", "test@example.com")
    await browser.click("#login-button")
    
    # Create new project
    await browser.fill("#chat-input", "Create a new project called 'E2E Test'")
    await browser.click("#send-button")
    
    # Verify project creation
    response_text = await browser.text_content("#chat-response")
    assert "successfully created" in response_text.lower()
```

## Test Organization

### Directory Structure
```
src/
├── backend/
│   ├── app/
│   │   ├── services/
│   │   ├── api/
│   │   └── models/
│   └── tests/
│       ├── unit/
│       │   ├── test_services/
│       │   ├── test_api/
│       │   └── test_models/
│       ├── integration/
│       │   ├── test_api_endpoints.py
│       │   └── test_azure_devops.py
│       ├── e2e/
│       │   └── test_workflows.py
│       ├── fixtures/
│       │   ├── azure_devops.py
│       │   └── test_data.py
│       └── conftest.py
└── frontend/
    ├── app/
    └── tests/
        ├── unit/
        ├── integration/
        └── conftest.py
```

### Test Configuration

#### conftest.py (Backend)
```python
import pytest
import asyncio
from httpx import AsyncClient
from app.main import app
from app.dependencies import get_azure_devops_service

@pytest.fixture(scope="session")
def event_loop():
    """Create an instance of the default event loop for the test session."""
    loop = asyncio.get_event_loop_policy().new_event_loop()
    yield loop
    loop.close()

@pytest.fixture
async def client():
    """Create test HTTP client."""
    async with AsyncClient(app=app, base_url="http://test") as ac:
        yield ac

@pytest.fixture
def mock_azure_devops_service():
    """Mock Azure DevOps service for testing."""
    class MockAzureDevOpsService:
        async def create_project(self, name, description):
            return {"id": "123", "name": name, "description": description}
    
    return MockAzureDevOpsService()

@pytest.fixture(autouse=True)
def override_dependencies(mock_azure_devops_service):
    """Override app dependencies with mocks."""
    app.dependency_overrides[get_azure_devops_service] = lambda: mock_azure_devops_service
    yield
    app.dependency_overrides.clear()
```

#### pytest.ini
```ini
[tool:pytest]
testpaths = tests
python_files = test_*.py
python_classes = Test*
python_functions = test_*
addopts = 
    --strict-markers
    --strict-config
    --cov=app
    --cov-report=term-missing
    --cov-report=html:htmlcov
    --cov-fail-under=90
markers =
    unit: Unit tests
    integration: Integration tests
    e2e: End-to-end tests
    slow: Slow running tests
asyncio_mode = auto
```

## Testing Best Practices

### 1. Test Naming
- Use descriptive names that explain what is being tested
- Follow pattern: `test_<what_is_being_tested>_<expected_outcome>`
- Group related tests in classes with descriptive names

```python
class TestProjectCreation:
    def test_create_project_with_valid_data_returns_project(self):
        pass
    
    def test_create_project_with_invalid_name_raises_validation_error(self):
        pass
    
    def test_create_project_with_duplicate_name_raises_conflict_error(self):
        pass
```

### 2. Test Data Management
Use factories for consistent test data generation:

```python
# factories.py
import factory
from app.models import Project, WorkItem

class ProjectFactory(factory.Factory):
    class Meta:
        model = Project
    
    name = factory.Sequence(lambda n: f"Project {n}")
    description = factory.Faker("text", max_nb_chars=200)
    visibility = "private"

class WorkItemFactory(factory.Factory):
    class Meta:
        model = WorkItem
    
    title = factory.Faker("sentence", nb_words=4)
    work_item_type = "Task"
    state = "New"
    project = factory.SubFactory(ProjectFactory)
```

### 3. Mocking External Services
Always mock external API calls to ensure test isolation:

```python
@pytest.fixture
def mock_azure_devops_api(httpx_mock):
    """Mock Azure DevOps REST API calls."""
    httpx_mock.add_response(
        method="POST",
        url="https://dev.azure.com/test-org/_apis/projects",
        json={"id": "123", "name": "Test Project"},
        status_code=201
    )
    return httpx_mock

@pytest.mark.asyncio
async def test_create_project_calls_azure_api(mock_azure_devops_api):
    service = AzureDevOpsService()
    project = await service.create_project("Test Project", "Description")
    assert project["name"] == "Test Project"
```

### 4. Async Testing
For async code, use proper async test patterns:

```python
@pytest.mark.asyncio
async def test_async_function():
    result = await some_async_function()
    assert result is not None

# For testing multiple async operations
@pytest.mark.asyncio
async def test_concurrent_operations():
    tasks = [async_operation(i) for i in range(5)]
    results = await asyncio.gather(*tasks)
    assert len(results) == 5
```

### 5. Database Testing
For database operations, use transaction rollback:

```python
@pytest.fixture
async def db_session():
    """Database session that rolls back after each test."""
    async with database.transaction(rollback=True):
        yield database

@pytest.mark.asyncio
async def test_user_creation(db_session):
    user = await create_user(db_session, "test@example.com")
    assert user.email == "test@example.com"
    # Transaction will rollback automatically
```

## Running Tests

### Local Development
```bash
# Run all tests
pytest

# Run specific test categories
pytest -m unit
pytest -m integration
pytest -m "not slow"

# Run with coverage
pytest --cov=app --cov-report=html

# Run specific test file
pytest tests/unit/test_services/test_azure_devops.py

# Run with verbose output
pytest -v

# Run failed tests only
pytest --lf

# Run tests in parallel (requires pytest-xdist)
pytest -n auto
```

### Continuous Integration
```bash
# Run all tests with strict coverage requirements
pytest --cov=app --cov-fail-under=90 --cov-report=xml

# Run tests with JUnit XML output for CI
pytest --junit-xml=test-results.xml
```

## Test Data and Fixtures

### Managing Test Data
- Use factories for creating test objects
- Keep test data minimal and focused
- Use realistic but not production data
- Clean up test data after each test

### Common Fixtures
```python
@pytest.fixture
def sample_project():
    return {
        "name": "Sample Project",
        "description": "A test project",
        "visibility": "private"
    }

@pytest.fixture
def authenticated_headers():
    return {"Authorization": "Bearer test-token"}

@pytest.fixture
async def created_project(client, sample_project):
    response = await client.post("/api/projects", json=sample_project)
    return response.json()
```

## Performance Testing

### Load Testing with Locust
```python
# locustfile.py
from locust import HttpUser, task, between

class APIUser(HttpUser):
    wait_time = between(1, 3)
    
    def on_start(self):
        # Login or setup
        response = self.client.post("/auth/login", json={
            "username": "test@example.com",
            "password": "password"
        })
        self.token = response.json()["token"]
        self.headers = {"Authorization": f"Bearer {self.token}"}
    
    @task(3)
    def list_projects(self):
        self.client.get("/api/projects", headers=self.headers)
    
    @task(1)
    def create_project(self):
        self.client.post("/api/projects", json={
            "name": f"Load Test Project {self.user_id}",
            "description": "Created during load testing"
        }, headers=self.headers)
```

## Security Testing

### Authentication Tests
```python
def test_protected_endpoint_requires_authentication(client):
    response = client.get("/api/projects")
    assert response.status_code == 401

def test_invalid_token_rejected(client):
    headers = {"Authorization": "Bearer invalid-token"}
    response = client.get("/api/projects", headers=headers)
    assert response.status_code == 401

def test_expired_token_rejected(client, expired_token):
    headers = {"Authorization": f"Bearer {expired_token}"}
    response = client.get("/api/projects", headers=headers)
    assert response.status_code == 401
```

### Input Validation Tests
```python
def test_sql_injection_protection(client):
    malicious_input = "'; DROP TABLE projects; --"
    response = client.post("/api/projects", json={
        "name": malicious_input,
        "description": "Test"
    })
    # Should either reject or sanitize input
    assert response.status_code in [400, 422]

def test_xss_protection(client):
    xss_input = "<script>alert('xss')</script>"
    response = client.post("/api/projects", json={
        "name": xss_input,
        "description": "Test"
    })
    # Should sanitize or reject
    assert "<script>" not in response.json().get("name", "")
```

## Debugging Tests

### Common Debugging Techniques
```bash
# Run single test with detailed output
pytest -vv tests/unit/test_specific.py::test_function_name

# Add print statements (use capfd fixture)
def test_with_debug_output(capfd):
    print("Debug info")
    result = some_function()
    captured = capfd.readouterr()
    assert "Debug info" in captured.out

# Use pytest debugger
pytest --pdb  # Drop into debugger on failures

# Use breakpoint() in Python 3.7+
def test_function():
    result = some_calculation()
    breakpoint()  # Debugger will stop here
    assert result == expected
```

## Test Maintenance

### Regular Test Maintenance Tasks
1. **Remove Obsolete Tests**: Delete tests for removed features
2. **Update Test Data**: Keep test data current with schema changes
3. **Refactor Duplicated Code**: Extract common test utilities
4. **Review Test Coverage**: Identify untested code paths
5. **Performance Review**: Remove or optimize slow tests

### Test Code Quality
- Apply same code quality standards to test code
- Use meaningful variable names in tests
- Keep tests simple and focused
- Avoid complex logic in tests
- Document complex test scenarios

## Resources

- [pytest Documentation](https://docs.pytest.org/)
- [pytest-asyncio Documentation](https://pytest-asyncio.readthedocs.io/)
- [HTTPx Testing Guide](https://www.python-httpx.org/advanced/#testing)
- [Factory Boy Documentation](https://factoryboy.readthedocs.io/)
- [Locust Documentation](https://docs.locust.io/)