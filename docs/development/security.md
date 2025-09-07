# Bandit Security Configuration

This document explains the bandit security configuration for the Azure DevOps AI Agent project.

## Security Issues Fixed

### 1. B104 - Hardcoded Bind All Interfaces (MEDIUM Severity)
**Issue**: Server configuration binding to `0.0.0.0` was flagged as a security risk.
**Resolution**: Added `# nosec B104` suppression with justification. Binding to all interfaces is required for containerized deployments where the container needs to accept connections from outside.

**Location**: `src/backend/app/core/config.py:18`

### 2. B106 - Hardcoded Password in Fallback Config (LOW Severity)
**Issue**: Mock JWT secret key was hardcoded in fallback configuration.
**Resolution**: Removed hardcoded secrets from fallback configuration. Now requires proper environment variables or `.env.test` file for development.

**Location**: `src/backend/app/core/config.py:69-78`

### 3. B106 - False Positive on Bearer Token Type (LOW Severity)
**Issue**: "bearer" token type was incorrectly flagged as a hardcoded password.
**Resolution**: Added `# nosec B106` suppression as this is the standard OAuth 2.0 token type, not a password.

**Location**: `src/backend/app/services/auth_service.py:105`

### 4. B106 - False Positive on Mock Access Token (LOW Severity)
**Issue**: Mock access token in test dependencies was flagged as a hardcoded password.
**Resolution**: Added `# nosec B106` suppression as this is clearly a mock value for testing/development purposes, not a real access token.

**Location**: `src/backend/app/core/dependencies.py:49`

## Configuration Files

### .bandit Configuration
The `.bandit` file provides project-wide configuration for bandit scans:
- Excludes test directories and non-production files
- Documents suppression rationale

### Environment Configuration
- `.env.example` - Template for production environment variables
- `.env.test` - Test environment configuration with safe defaults
- Proper error messages guide developers to correct configuration

## CI/CD Integration

### Enhanced Error Reporting
The GitHub Actions workflow now provides detailed error information when bandit fails:
- Shows human-readable security issues
- Displays JSON report excerpt
- Provides troubleshooting guidance
- Links to bandit documentation

### Workflow Commands
```bash
# Run bandit security scan
bandit -r src/backend/app -f json -o bandit-report.json

# Show issues in human-readable format on failure
bandit -r src/backend/app -f screen
```

## Best Practices Applied

1. **Minimal Suppressions**: Only suppress false positives or acceptable security trade-offs
2. **Clear Documentation**: Each suppression includes context
3. **Proper Configuration**: Remove hardcoded secrets, use environment variables
4. **Better Error Messages**: Guide developers to proper setup
5. **CI/CD Visibility**: Security issues are clearly reported in pipeline failures

## Future Maintenance

When adding new code:
1. Run `bandit -r src/backend/app` locally before committing
2. Address legitimate security issues in code
3. Only use `# nosec` for verified false positives
4. Update this documentation for any new suppressions
5. Review and update `.bandit` configuration as needed