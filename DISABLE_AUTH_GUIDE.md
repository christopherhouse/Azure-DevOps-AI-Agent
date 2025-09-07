# DISABLE_AUTH Feature Flag - Usage Guide

The backend now includes a `DISABLE_AUTH` feature flag that allows you to bypass Azure AD authentication for testing and development purposes.

## How to Use

1. **Enable the feature flag**: Edit `src/backend/app/main.py` line 37:
   ```python
   # Change this line:
   DISABLE_AUTH = False
   
   # To this:
   DISABLE_AUTH = True
   ```

2. **Start the backend server**:
   ```bash
   cd src/backend
   uv run uvicorn app.main:app --reload --port 8000
   ```

3. **You'll see this warning message** when authentication is disabled:
   ```
   ⚠️  AUTHENTICATION DISABLED - Using mock user for all endpoints
   ```

4. **Test API endpoints without authentication**:
   ```bash
   # Chat endpoint (normally requires auth)
   curl -X POST http://localhost:8000/api/chat/message \
     -H "Content-Type: application/json" \
     -d '{"message": "Test without auth", "context": {"organization": "test"}}'
   
   # Projects endpoint (normally requires auth)
   curl http://localhost:8000/api/projects
   
   # Work items endpoint (normally requires auth)
   curl http://localhost:8000/api/test-project/workitems
   ```

## Mock User Details

When authentication is disabled, all endpoints receive this mock user:
- **User ID**: `mock-user-123`
- **Email**: `mock-user@example.com`
- **Roles**: `["User"]`
- **Access Token**: `mock-access-token`

## Important Notes

- **Default State**: The flag is `False` by default - no behavior changes unless you explicitly enable it
- **Production Safety**: This is only a variable in the code, not an environment variable, so it can't be accidentally enabled in production
- **No Frontend Changes**: The frontend continues to work normally and is unaware of this backend flag
- **Development Only**: This feature is intended for development and testing purposes

## Reverting

To re-enable authentication, simply change the flag back:
```python
DISABLE_AUTH = False
```

And restart the server. All endpoints will require proper Azure AD authentication again.