# Copilot Coding Instructions

These instructions guide GitHub Copilot when generating code, infrastructure, or workflows in this repository. Always follow these rules.

---

## Python (Backend)

- Always target **Python 3.11+**.  
- Use **async/await** for FastAPI endpoints and service calls.  
- Always include **type hints**.  
- Use **Pydantic models** for request/response validation.  
- Generate **docstrings** and inline comments for non-trivial logic.  
- Structure code with **dependency injection** for services/config.  
- Use `httpx.AsyncClient` for async API calls.  
- Never suggest hardcoded secrets → always assume **Azure Managed Identity** or **Key Vault**.  

---

## FastAPI + Semantic Kernel

- Default to **FastAPI async endpoints** with OpenAPI annotations.  
- Use **Semantic Kernel plugins** for Azure DevOps operations (projects, work items, repos, pipelines).  
- Always implement **error handling and logging**.  
- Default to **OpenTelemetry logging** with **Application Insights**.  

---

## Frontend (Gradio)

- Generate **modular UI components**, not monolithic apps.  
- Use **Gradio state management** for user sessions.  
- Always integrate **Microsoft Entra ID authentication (PKCE)**.  
- Provide **clear error messages** and **loading states**.  

---

## Infrastructure (Bicep)

- Always use **Azure Verified Modules (AVM)** when available.  
- Only create custom modules if AVM doesn’t exist.  
- Default naming convention: `main.dev.bicepparam`, `main.prod.bicepparam`.  
- Never store secrets in param files → assume **Key Vault references**.  
- Always configure **diagnostic settings** with category groups.  
- Always use **managed identity** for resource access.  

---

## Docker

- Use **multi-stage builds** with `python:3.11-slim` base.  
- Always run as a **non-root user**.  
- Optimize for **small image size**.  
- Include security scanning steps in CI/CD.  

---

## GitHub Actions (CI/CD)

- Always generate workflows in `.github/workflows/`.  
- Separate workflows for:  
  - `ci.yml` → lint, test, build, push  
  - `deploy-dev.yml` → dev deploy  
  - `deploy-prod.yml` → prod deploy  
  - `infrastructure.yml` → IaC deploy  
- Use **GitHub OIDC → Azure federated identity** (never secrets in workflow files).  
- Fail builds if lint, type-check, tests, or security scans fail.  

---

## Quality Tools

- Lint & format: `ruff`  
- Type check: `mypy`  
- Tests: `pytest` with coverage (≥90% backend, ≥70% frontend)  
- Security: `bandit` + dependency scans  
- Always generate code that **passes all quality tools**.  

---

## Security Defaults

- Frontend auth: Entra ID (PKCE).  
- Backend auth: Validate Entra ID tokens + RBAC.  
- All service-to-service calls must be **authenticated**.  
- Encrypt all data in transit (HTTPS).  
- Always use **Azure Key Vault** for secrets.  

---

## Reliability & Performance

- Use **retry logic with exponential backoff** for external calls.  
- Use **circuit breaker patterns** for Azure DevOps API calls.  
- Implement **caching** for frequently accessed data.  
- Assume target response times:  
  - API < 2s  
  - Frontend load < 3s  

---

## Documentation

- Always generate/update OpenAPI docs for APIs.  
- Provide **README updates** when adding features.  
- Add inline comments for non-trivial sections.  

---

⚠️ **Reminder for Copilot**:  
Always generate **secure, type-safe, async, modular, and production-ready code** aligned with Azure Well-Architected principles.  
