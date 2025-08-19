"""Basic test for backend health check."""

def test_health_check():
    """Test that health check function exists."""
    from app.main import health_check
    assert health_check is not None