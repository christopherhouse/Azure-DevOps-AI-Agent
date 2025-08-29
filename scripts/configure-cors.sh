#!/bin/bash
#
# Configure CORS for Azure Container Apps
# This script configures CORS settings for a container app to allow requests from specified origins
#

set -euo pipefail

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Emoji codes
SPARKLES='✨'
GEAR='⚙️'
CROSS='❌'
CHECK='✅'
WARNING='⚠️'

# Default values
SCRIPT_NAME="$(basename "$0")"
APP_NAME=""
RESOURCE_GROUP=""
ALLOWED_ORIGINS=""
ALLOWED_METHODS="*"
ALLOW_CREDENTIALS="true"
VERBOSE=false

# Print functions
print_message() {
    local color="$1"
    local icon="$2"
    local message="$3"
    echo -e "${color}${icon} ${message}${NC}"
}

print_info() {
    print_message "$BLUE" "$GEAR" "$1"
}

print_success() {
    print_message "$GREEN" "$CHECK" "$1"
}

print_warning() {
    print_message "$YELLOW" "$WARNING" "$1"
}

print_error() {
    print_message "$RED" "$CROSS" "$1"
    exit 1
}

# Function to show usage
show_usage() {
    cat << EOF
Usage: $SCRIPT_NAME [OPTIONS]

Configure CORS settings for an Azure Container App.

Required Options:
  --app-name, -n        Container app name
  --resource-group, -g  Resource group name
  --allowed-origins, -o Comma-separated list of allowed origins (e.g., https://example.com,https://app.com)

Optional Options:
  --allowed-methods, -m Comma-separated list of allowed methods (default: *)
  --allow-credentials   Allow credentials (default: true)
  --verbose, -v         Enable verbose output
  --help, -h            Show this help message

Examples:
  # Configure CORS for backend app to allow frontend origin
  $SCRIPT_NAME \\
    --app-name my-backend \\
    --resource-group my-rg \\
    --allowed-origins https://my-frontend.azurecontainerapps.io

  # Configure CORS with multiple origins and custom methods
  $SCRIPT_NAME \\
    --app-name my-api \\
    --resource-group my-rg \\
    --allowed-origins https://app1.com,https://app2.com \\
    --allowed-methods GET,POST \\
    --verbose
EOF
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --app-name|-n)
                APP_NAME="$2"
                shift 2
                ;;
            --resource-group|-g)
                RESOURCE_GROUP="$2"
                shift 2
                ;;
            --allowed-origins|-o)
                ALLOWED_ORIGINS="$2"
                shift 2
                ;;
            --allowed-methods|-m)
                ALLOWED_METHODS="$2"
                shift 2
                ;;
            --allow-credentials)
                ALLOW_CREDENTIALS="$2"
                shift 2
                ;;
            --verbose|-v)
                VERBOSE=true
                shift
                ;;
            --help|-h)
                show_usage
                exit 0
                ;;
            *)
                print_error "Unknown option: $1"
                ;;
        esac
    done
}

# Function to validate required parameters
validate_params() {
    local missing_params=()
    
    [[ -z "$APP_NAME" ]] && missing_params+=("--app-name")
    [[ -z "$RESOURCE_GROUP" ]] && missing_params+=("--resource-group")
    [[ -z "$ALLOWED_ORIGINS" ]] && missing_params+=("--allowed-origins")
    
    if [[ ${#missing_params[@]} -gt 0 ]]; then
        print_error "Missing required parameters: ${missing_params[*]}"
    fi
}

# Function to configure CORS settings
configure_cors() {
    print_info "Configuring CORS settings for container app '$APP_NAME'..."
    
    # Convert comma-separated origins to space-separated for Azure CLI
    local origins_array
    IFS=',' read -ra origins_array <<< "$ALLOWED_ORIGINS"
    
    # Convert comma-separated methods to space-separated for Azure CLI
    local methods_array
    IFS=',' read -ra methods_array <<< "$ALLOWED_METHODS"
    
    print_info "Allowed origins: ${origins_array[*]}"
    print_info "Allowed methods: ${methods_array[*]}"
    print_info "Allow credentials: $ALLOW_CREDENTIALS"
    
    if [[ "$VERBOSE" == "true" ]]; then
        print_info "Checking current CORS configuration..."
    fi
    
    # Check if CORS is already configured
    local cors_status
    cors_status=$(az containerapp ingress cors show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "allowedOrigins" \
        --output tsv 2>/dev/null || echo "not-configured")
    
    if [[ "$cors_status" == "not-configured" || "$cors_status" == "" ]]; then
        # Enable CORS for the first time
        print_info "Enabling CORS for the first time..."
        
        local cmd_args=(
            "az" "containerapp" "ingress" "cors" "enable"
            "--name" "$APP_NAME"
            "--resource-group" "$RESOURCE_GROUP"
            "--allowed-origins" "${origins_array[@]}"
            "--allowed-methods" "${methods_array[@]}"
        )
        
        if [[ "$ALLOW_CREDENTIALS" == "true" ]]; then
            cmd_args+=("--allow-credentials")
        fi
        
        if [[ "$VERBOSE" == "true" ]]; then
            print_info "Command: ${cmd_args[*]}"
        fi
        
        if "${cmd_args[@]}"; then
            print_success "CORS enabled successfully!"
        else
            print_error "Failed to enable CORS!"
        fi
    else
        # Update existing CORS configuration
        print_info "Updating existing CORS configuration..."
        
        local cmd_args=(
            "az" "containerapp" "ingress" "cors" "update"
            "--name" "$APP_NAME"
            "--resource-group" "$RESOURCE_GROUP"
            "--allowed-origins" "${origins_array[@]}"
            "--allowed-methods" "${methods_array[@]}"
        )
        
        if [[ "$ALLOW_CREDENTIALS" == "true" ]]; then
            cmd_args+=("--allow-credentials")
        fi
        
        if [[ "$VERBOSE" == "true" ]]; then
            print_info "Command: ${cmd_args[*]}"
        fi
        
        if "${cmd_args[@]}"; then
            print_success "CORS updated successfully!"
        else
            print_error "Failed to update CORS!"
        fi
    fi
}

# Function to show CORS configuration
show_cors_config() {
    print_info "Current CORS configuration:"
    
    if az containerapp ingress cors show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --output table 2>/dev/null; then
        return 0
    else
        print_warning "Could not retrieve CORS configuration or CORS is not enabled."
        return 1
    fi
}

# Main function
main() {
    echo -e "\n${MAGENTA}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${WHITE}${SPARKLES}  Azure Container App CORS Configuration  ${SPARKLES}${NC}"
    echo -e "${MAGENTA}═══════════════════════════════════════════════════════════════${NC}\n"
    
    # Parse arguments
    parse_args "$@"
    
    # Validate parameters
    validate_params
    
    # Configure CORS
    if configure_cors; then
        echo ""
        show_cors_config
        echo ""
        print_message "$GREEN" "$SPARKLES" "CORS configuration completed successfully!"
    else
        echo ""
        print_message "$RED" "$CROSS" "CORS configuration failed!"
        exit 1
    fi
}

# Run main function with all arguments
main "$@"