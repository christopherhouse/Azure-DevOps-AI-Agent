#!/bin/bash
#
# Azure Container App Deployment Script
# This script creates or updates Azure Container Apps using Azure CLI
# Supports multi-revision deployments with attractive console output
#

set -euo pipefail

# Default values
SCRIPT_NAME="$(basename "$0")"
ENVIRONMENT_NAME=""
RESOURCE_GROUP=""
APP_NAME=""
IMAGE=""
TARGET_PORT=""
INGRESS="external"
REGISTRY_SERVER=""
REGISTRY_IDENTITY=""
MANAGED_IDENTITY=""
KEY_VAULT_NAME=""
ENV_VARS=()
SECRETS=()
SECRET_REFS=()
CPU="1.0"
MEMORY="2Gi"
MIN_REPLICAS="1"
MAX_REPLICAS="10"
REVISIONS_MODE="multiple"
VERBOSE=false

# Color codes for attractive output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Emojis for visual appeal
ROCKET="🚀"
CHECK="✅"
CROSS="❌"
GEAR="⚙️"
PACKAGE="📦"
GLOBE="🌐"
WARNING="⚠️"
INFO="ℹ️"
SPARKLES="✨"

# Function to print colored output
print_message() {
    local color=$1
    local emoji=$2
    local message=$3
    echo -e "${color}${emoji} ${message}${NC}"
}

print_info() {
    print_message "$CYAN" "$INFO" "$1"
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

print_header() {
    echo -e "\n${MAGENTA}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${WHITE}${SPARKLES}  Azure Container App Deployment Script  ${SPARKLES}${NC}"
    echo -e "${MAGENTA}═══════════════════════════════════════════════════════════════${NC}\n"
}

# Function to show usage
show_usage() {
    cat << EOF
Usage: $SCRIPT_NAME [OPTIONS]

Deploy or update an Azure Container App with multi-revision support.

Required Options:
  --environment, -e     Container Apps Environment name or resource ID
  --resource-group, -g  Resource group name
  --app-name, -n        Container app name
  --image, -i           Container image (e.g., myregistry.azurecr.io/app:tag)
  --target-port, -p     Application port for ingress traffic

Optional Options:
  --ingress             Ingress type: external (default) or internal
  --registry-server     Container registry server (e.g., myregistry.azurecr.io)
  --registry-identity   Managed identity for registry authentication
  --managed-identity    User-assigned managed identity for the container app
  --key-vault-name      Key Vault name for secret references (required when using --secret-ref)
  --env-var             Environment variable (key=value). Can be used multiple times
  --secret              Secret (key=value). Can be used multiple times
  --secret-ref          Key Vault secret reference (key=vault-secret-name). Can be used multiple times
  --cpu                 CPU allocation (default: 1.0)
  --memory              Memory allocation (default: 2Gi)
  --min-replicas        Minimum replicas (default: 1)
  --max-replicas        Maximum replicas (default: 10)
  --revisions-mode      Revisions mode: multiple (default) or single
  --verbose, -v         Enable verbose output
  --help, -h            Show this help message

Examples:
  # Deploy a backend API
  $SCRIPT_NAME \\
    --environment my-containerapp-env \\
    --resource-group my-rg \\
    --app-name my-backend \\
    --image myregistry.azurecr.io/backend:v1.0 \\
    --target-port 8000 \\
    --registry-identity system \\
    --env-var API_VERSION=1.0 \\
    --secret DB_PASSWORD=secretvalue

  # Deploy a frontend app
  $SCRIPT_NAME \\
    --environment my-containerapp-env \\
    --resource-group my-rg \\
    --app-name my-frontend \\
    --image myregistry.azurecr.io/frontend:v1.0 \\
    --target-port 7860 \\
    --cpu 0.5 \\
    --memory 1Gi
EOF
}

# Function to parse environment variables
parse_env_var() {
    local env_var="$1"
    if [[ "$env_var" == *"="* ]]; then
        ENV_VARS+=("$env_var")
    else
        print_error "Invalid environment variable format: $env_var. Use key=value format."
    fi
}

# Function to parse secrets
parse_secret() {
    local secret="$1"
    if [[ "$secret" == *"="* ]]; then
        SECRETS+=("$secret")
    else
        print_error "Invalid secret format: $secret. Use key=value format."
    fi
}

# Function to parse secret references (Key Vault)
parse_secret_ref() {
    local secret_ref="$1"
    if [[ "$secret_ref" == *"="* ]]; then
        SECRET_REFS+=("$secret_ref")
    else
        print_error "Invalid secret reference format: $secret_ref. Use key=vault-secret-name format."
    fi
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --environment|-e)
                ENVIRONMENT_NAME="$2"
                shift 2
                ;;
            --resource-group|-g)
                RESOURCE_GROUP="$2"
                shift 2
                ;;
            --app-name|-n)
                APP_NAME="$2"
                shift 2
                ;;
            --image|-i)
                IMAGE="$2"
                shift 2
                ;;
            --target-port|-p)
                TARGET_PORT="$2"
                shift 2
                ;;
            --ingress)
                INGRESS="$2"
                shift 2
                ;;
            --registry-server)
                REGISTRY_SERVER="$2"
                shift 2
                ;;
            --registry-identity)
                REGISTRY_IDENTITY="$2"
                shift 2
                ;;
            --managed-identity)
                MANAGED_IDENTITY="$2"
                shift 2
                ;;
            --key-vault-name)
                KEY_VAULT_NAME="$2"
                shift 2
                ;;
            --env-var)
                parse_env_var "$2"
                shift 2
                ;;
            --secret)
                parse_secret "$2"
                shift 2
                ;;
            --secret-ref)
                parse_secret_ref "$2"
                shift 2
                ;;
            --cpu)
                CPU="$2"
                shift 2
                ;;
            --memory)
                MEMORY="$2"
                shift 2
                ;;
            --min-replicas)
                MIN_REPLICAS="$2"
                shift 2
                ;;
            --max-replicas)
                MAX_REPLICAS="$2"
                shift 2
                ;;
            --revisions-mode)
                REVISIONS_MODE="$2"
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
    
    [[ -z "$ENVIRONMENT_NAME" ]] && missing_params+=("--environment")
    [[ -z "$RESOURCE_GROUP" ]] && missing_params+=("--resource-group")
    [[ -z "$APP_NAME" ]] && missing_params+=("--app-name")
    [[ -z "$IMAGE" ]] && missing_params+=("--image")
    [[ -z "$TARGET_PORT" ]] && missing_params+=("--target-port")
    
    if [[ ${#missing_params[@]} -gt 0 ]]; then
        print_error "Missing required parameters: ${missing_params[*]}"
    fi
    
    # Validate Key Vault name when secret references are used
    if [[ ${#SECRET_REFS[@]} -gt 0 && -z "$KEY_VAULT_NAME" ]]; then
        print_error "Key Vault name (--key-vault-name) is required when using secret references (--secret-ref)"
    fi
    
    # Validate revisions mode
    if [[ "$REVISIONS_MODE" != "multiple" && "$REVISIONS_MODE" != "single" ]]; then
        print_error "Invalid revisions mode: $REVISIONS_MODE. Must be 'multiple' or 'single'."
    fi
    
    # Validate ingress type
    if [[ "$INGRESS" != "external" && "$INGRESS" != "internal" ]]; then
        print_error "Invalid ingress type: $INGRESS. Must be 'external' or 'internal'."
    fi
}

# Function to check if container app exists
check_app_exists() {
    print_info "Checking if container app '$APP_NAME' exists..."
    
    if az containerapp show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --output none 2>/dev/null; then
        print_success "Container app '$APP_NAME' exists. Will update with new revision."
        return 0
    else
        print_info "Container app '$APP_NAME' does not exist. Will create new app."
        return 1
    fi
}

# Function to build Azure CLI command
build_az_command() {
    local operation="$1"  # "create" or "update"
    local cmd_args=()
    
    # Base arguments
    cmd_args+=(
        "$operation"
        "--name" "$APP_NAME"
        "--resource-group" "$RESOURCE_GROUP"
    )
    
    # Add environment for create operation
    if [[ "$operation" == "create" ]]; then
        cmd_args+=("--environment" "$ENVIRONMENT_NAME")
    fi
    
    # Container configuration - deploys a single container image per app
    # Each call to this script deploys exactly one container to one container app
    cmd_args+=(
        "--image" "$IMAGE"
        "--cpu" "$CPU"
        "--memory" "$MEMORY"
        "--min-replicas" "$MIN_REPLICAS"
        "--max-replicas" "$MAX_REPLICAS"
    )
    
    # Ingress and revision configuration - only supported for create operations
    if [[ "$operation" == "create" ]]; then
        cmd_args+=(
            "--target-port" "$TARGET_PORT"
            "--ingress" "$INGRESS"
            "--revisions-mode" "$REVISIONS_MODE"
        )
    fi
    
    # Registry configuration - only supported for create operations
    if [[ "$operation" == "create" ]]; then
        if [[ -n "$REGISTRY_SERVER" ]]; then
            cmd_args+=("--registry-server" "$REGISTRY_SERVER")
        fi
        
        if [[ -n "$REGISTRY_IDENTITY" ]]; then
            cmd_args+=("--registry-identity" "$REGISTRY_IDENTITY")
        fi
    fi
    
    # Managed Identity - only supported for create operations
    if [[ "$operation" == "create" && -n "$MANAGED_IDENTITY" ]]; then
        cmd_args+=("--user-assigned" "$MANAGED_IDENTITY")
    fi
    
    # Environment variables (combine regular env vars and secret refs)
    local all_env_vars=()
    all_env_vars+=("${ENV_VARS[@]}")
    
    # Convert secret refs to proper Key Vault format using the Key Vault name
    for secret_ref in "${SECRET_REFS[@]}"; do
        # Input format: ENV_VAR_NAME=secret-name
        # Need to get the Key Vault URI and convert to: ENV_VAR_NAME=secretref:secret-name
        if [[ "$secret_ref" == *"="* ]]; then
            env_var_name="${secret_ref%%=*}"
            secret_name="${secret_ref#*=}"
            # Format for Container Apps: secretref:secret-name
            all_env_vars+=("${env_var_name}=secretref:${secret_name}")
        fi
    done
    
    if [[ ${#all_env_vars[@]} -gt 0 ]]; then
        # Use different parameter names for create vs update operations
        if [[ "$operation" == "create" ]]; then
            cmd_args+=("--env-vars")
        else
            cmd_args+=("--set-env-vars")
        fi
        cmd_args+=("${all_env_vars[@]}")
    fi
    
    # Secrets - only supported for create operations
    # For update operations, secrets are handled separately via `az containerapp secret set`
    if [[ "$operation" == "create" ]]; then
        local all_secrets=()
        all_secrets+=("${SECRETS[@]}")
        
        # Create Key Vault secret references
        if [[ ${#SECRET_REFS[@]} -gt 0 && -n "$KEY_VAULT_NAME" && -n "$MANAGED_IDENTITY" ]]; then
            for secret_ref in "${SECRET_REFS[@]}"; do
                if [[ "$secret_ref" == *"="* ]]; then
                    env_var_name="${secret_ref%%=*}"
                    secret_name="${secret_ref#*=}"
                    # Format for Container Apps Key Vault reference:
                    # secret-name=keyvaultref:https://vault.vault.azure.net/secrets/secret-name,identityref:managed-identity-id
                    key_vault_uri="https://${KEY_VAULT_NAME}.vault.azure.net/secrets/${secret_name}"
                    all_secrets+=("${secret_name}=keyvaultref:${key_vault_uri},identityref:${MANAGED_IDENTITY}")
                fi
            done
        fi
        
        if [[ ${#all_secrets[@]} -gt 0 ]]; then
            cmd_args+=("--secrets")
            cmd_args+=("${all_secrets[@]}")
        fi
    fi
    
    echo "${cmd_args[@]}"
}

# Function to update secrets for update operations
update_secrets() {
    # Only run for update operations when we have secret references
    if [[ ${#SECRET_REFS[@]} -eq 0 || -z "$KEY_VAULT_NAME" || -z "$MANAGED_IDENTITY" ]]; then
        return 0
    fi
    
    print_info "Updating secrets for container app '$APP_NAME'..."
    
    local all_secrets=()
    all_secrets+=("${SECRETS[@]}")
    
    # Create Key Vault secret references
    for secret_ref in "${SECRET_REFS[@]}"; do
        if [[ "$secret_ref" == *"="* ]]; then
            env_var_name="${secret_ref%%=*}"
            secret_name="${secret_ref#*=}"
            # Format for Container Apps Key Vault reference:
            # secret-name=keyvaultref:https://vault.vault.azure.net/secrets/secret-name,identityref:managed-identity-id
            key_vault_uri="https://${KEY_VAULT_NAME}.vault.azure.net/secrets/${secret_name}"
            all_secrets+=("${secret_name}=keyvaultref:${key_vault_uri},identityref:${MANAGED_IDENTITY}")
        fi
    done
    
    if [[ ${#all_secrets[@]} -gt 0 ]]; then
        if [[ "$VERBOSE" == "true" ]]; then
            print_info "Secret update command: az containerapp secret set --name $APP_NAME --resource-group $RESOURCE_GROUP --secrets ${all_secrets[*]}"
        fi
        
        if az containerapp secret set \
            --name "$APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --secrets "${all_secrets[@]}"; then
            print_success "Secrets updated successfully!"
            return 0
        else
            print_error "Failed to update secrets!"
            return 1
        fi
    fi
    
    return 0
}

# Function to execute deployment
execute_deployment() {
    local operation="$1"
    local cmd_args
    
    print_info "Building Azure CLI command for $operation operation..."
    
    # For update operations, update secrets first
    if [[ "$operation" == "update" ]]; then
        if ! update_secrets; then
            return 1
        fi
    fi
    
    # Build the command arguments
    cmd_args=($(build_az_command "$operation"))
    
    if [[ "$VERBOSE" == "true" ]]; then
        print_info "Command: az containerapp ${cmd_args[*]}"
    fi
    
    print_info "Executing $operation operation for container app '$APP_NAME'..."
    
    # Execute the Azure CLI command
    if az containerapp "${cmd_args[@]}"; then
        print_success "$operation operation completed successfully!"
        return 0
    else
        print_error "$operation operation failed!"
        return 1
    fi
}

# Function to get app URL
get_app_url() {
    print_info "Retrieving application URL..."
    
    local app_url
    app_url=$(az containerapp show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "properties.configuration.ingress.fqdn" \
        --output tsv 2>/dev/null)
    
    if [[ -n "$app_url" && "$app_url" != "null" ]]; then
        print_success "Application URL: ${GLOBE} https://$app_url"
        return 0
    else
        print_warning "Could not retrieve application URL."
        return 1
    fi
}

# Function to show deployment summary
show_summary() {
    echo -e "\n${BLUE}${GEAR} Deployment Summary:${NC}"
    echo -e "${WHITE}  • App Name:       ${NC}$APP_NAME"
    echo -e "${WHITE}  • Resource Group: ${NC}$RESOURCE_GROUP"
    echo -e "${WHITE}  • Environment:    ${NC}$ENVIRONMENT_NAME"
    echo -e "${WHITE}  • Image:          ${NC}$IMAGE"
    echo -e "${WHITE}  • Target Port:    ${NC}$TARGET_PORT"
    echo -e "${WHITE}  • Ingress:        ${NC}$INGRESS"
    echo -e "${WHITE}  • Revisions Mode: ${NC}$REVISIONS_MODE"
    echo -e "${WHITE}  • CPU:            ${NC}$CPU"
    echo -e "${WHITE}  • Memory:         ${NC}$MEMORY"
    echo -e "${WHITE}  • Replicas:       ${NC}$MIN_REPLICAS-$MAX_REPLICAS"
    
    if [[ -n "$MANAGED_IDENTITY" ]]; then
        echo -e "${WHITE}  • Managed Identity: ${NC}$MANAGED_IDENTITY"
    fi
    
    if [[ ${#ENV_VARS[@]} -gt 0 ]]; then
        echo -e "${WHITE}  • Env Variables:  ${NC}${#ENV_VARS[@]} variables"
    fi
    
    if [[ ${#SECRET_REFS[@]} -gt 0 ]]; then
        echo -e "${WHITE}  • Secret Refs:    ${NC}${#SECRET_REFS[@]} Key Vault references"
    fi
    
    if [[ ${#SECRETS[@]} -gt 0 ]]; then
        echo -e "${WHITE}  • Secrets:        ${NC}${#SECRETS[@]} inline secrets"
    fi
    
    echo ""
}

# Main function
main() {
    print_header
    
    # Parse arguments
    parse_args "$@"
    
    # Validate parameters
    validate_params
    
    # Show deployment summary
    show_summary
    
    # Check if app exists and determine operation
    if check_app_exists; then
        operation="update"
        print_info "Will create new revision for existing container app."
    else
        operation="create"
        print_info "Will create new container app."
    fi
    
    echo ""
    
    # Execute deployment
    print_message "$BLUE" "$ROCKET" "Starting deployment..."
    
    if execute_deployment "$operation"; then
        echo ""
        get_app_url
        echo ""
        print_message "$GREEN" "$SPARKLES" "Deployment completed successfully!"
        
        if [[ "$REVISIONS_MODE" == "multiple" ]]; then
            print_info "Multi-revision mode enabled. New revision created alongside existing ones."
        fi
    else
        echo ""
        print_message "$RED" "$CROSS" "Deployment failed!"
        exit 1
    fi
    
    echo ""
}

# Run main function with all arguments
main "$@"