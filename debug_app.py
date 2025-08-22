#!/usr/bin/env python3
"""Debug script to identify the exact source of the error."""

import traceback
import sys
from pathlib import Path

# Add frontend directory to path
frontend_dir = Path(__file__).parent / "src" / "frontend"
sys.path.insert(0, str(frontend_dir))

def debug_import_sequence():
    """Test imports in sequence to find the exact failure point."""
    print("=== DEBUG: Import sequence test ===")
    
    try:
        print("1. Testing basic imports...")
        import logging
        import sys
        from pathlib import Path
        print("   ✓ Basic imports successful")
        
        print("2. Testing dotenv and config...")
        from dotenv import load_dotenv
        load_dotenv()
        from config import settings
        print(f"   ✓ Config loaded. Telemetry enabled: {settings.enable_telemetry}")
        
        print("3. Testing telemetry service...")
        from services.telemetry import telemetry
        print("   ✓ Telemetry service loaded")
        
        print("4. Testing gradio import...")
        import gradio as gr
        print("   ✓ Gradio imported")
        
        print("5. Testing auth components...")
        from components.auth import auth_state, create_login_interface
        print("   ✓ Auth components imported")
        
        print("6. Testing chat components...")
        from components.chat import create_chat_interface
        print("   ✓ Chat components imported")
        
        print("7. Testing main app function...")
        from app import create_main_interface
        print("   ✓ Main app function imported")
        
        print("8. Creating interface (this is where the error occurs)...")
        app = create_main_interface()
        print("   ✓ Interface created successfully!")
        
    except Exception as e:
        print(f"   ✗ Error at step: {e}")
        print("\nFull traceback:")
        traceback.print_exc()
        return False
    
    return True

if __name__ == "__main__":
    debug_import_sequence()