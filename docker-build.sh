#!/bin/bash

# =================================================================
# GCC Web Application - Docker Image Build Script (Linux/Mac)
# =================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }

clear
echo "============================================================"
echo "   GCC Web Application - Docker Image Builder"
echo "============================================================"
echo

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed."
    echo "Please install Docker: https://docs.docker.com/get-docker/"
    exit 1
fi

# Check if Docker daemon is running
if ! docker ps &> /dev/null; then
    print_error "Docker daemon is not running."
    echo "Please start Docker and try again."
    exit 1
fi

print_info "Docker is installed and running."
echo

# Set build variables
BUILD_DATE=$(date +%Y%m%d)
BUILD_TIME=$(date +%H%M%S)
BUILD_NUMBER="${BUILD_DATE}_${BUILD_TIME}"
GIT_COMMIT=$(git rev-parse --short HEAD 2>/dev/null || echo "unknown")
LOG_DIR="logs"
LOG_FILE="${LOG_DIR}/docker-build_${BUILD_NUMBER}.log"

# Create log directory
mkdir -p "$LOG_DIR"

# Start logging
{
    echo "============================================================"
    echo "Docker Build Started: $(date)"
    echo "Git Commit: $GIT_COMMIT"
    echo "============================================================"
} >> "$LOG_FILE"

# Function to build image
build_image() {
    local env_name=$1
    local build_config=$2
    local aspnet_env=$3
    local tag_suffix=$4
    
    local image_tag="gcc-webapp:${env_name}-${BUILD_NUMBER}"
    local image_latest="gcc-webapp:${env_name}-latest"
    
    print_info "Building $env_name image: $image_tag"
    
    if docker build \
        --build-arg BUILD_CONFIGURATION="$build_config" \
        --build-arg BUILD_DATE="$BUILD_DATE" \
        --build-arg BUILD_NUMBER="$BUILD_NUMBER" \
        --build-arg GIT_COMMIT="$GIT_COMMIT" \
        --build-arg ASPNETCORE_ENVIRONMENT="$aspnet_env" \
        --label "version=$BUILD_NUMBER" \
        --label "environment=$env_name" \
        --label "git.commit=$GIT_COMMIT" \
        --label "build.date=$(date)" \
        -t "$image_tag" \
        -t "$image_latest" \
        -f Dockerfile \
        . >> "$LOG_FILE" 2>&1; then
        
        print_success "$env_name image built: $image_tag"
        docker images "$image_tag" --format "Size: {{.Size}}"
        return 0
    else
        print_error "Build failed. Check $LOG_FILE for details."
        return 1
    fi
}

# Function to push image to registry
push_image() {
    local registry=$1
    local username=$2
    local image=$3
    
    print_info "Pushing $image to $registry..."
    
    local remote_tag="$registry/$image"
    
    docker tag "$image" "$remote_tag"
    
    if docker push "$remote_tag" >> "$LOG_FILE" 2>&1; then
        print_success "Image pushed: $remote_tag"
        return 0
    else
        print_error "Push failed."
        return 1
    fi
}

# Function to run security scan
security_scan() {
    local image=$1
    
    print_info "Running security scan on $image..."
    
    # Try Docker Scout first
    if command -v docker-scout &> /dev/null; then
        docker scout cves "$image"
        docker scout recommendations "$image"
    # Try Trivy
    elif command -v trivy &> /dev/null; then
        trivy image "$image"
    # Try Docker scan
    elif docker scan --version &> /dev/null; then
        docker scan "$image"
    else
        print_warning "No security scanner available. Install Docker Scout, Trivy, or enable Docker Scan."
    fi
}

# Main menu
show_menu() {
    echo "============================================================"
    echo "   Docker Image Build Menu"
    echo "============================================================"
    echo
    echo "Build Information:"
    echo "   Date:        $BUILD_DATE"
    echo "   Build ID:    $BUILD_NUMBER"
    echo "   Git Commit:  $GIT_COMMIT"
    echo
    echo "============================================================"
    echo
    echo "1. Build Development Image"
    echo "2. Build Test Image"
    echo "3. Build Production Image"
    echo "4. Build All Environments"
    echo "5. Build and Push to Registry"
    echo "6. Build Multi-Architecture Image"
    echo "7. Security Scan Image"
    echo "8. Clean Build (No Cache)"
    echo "9. View Build History"
    echo "0. Exit"
    echo
    read -p "Select build option: " choice
}

# Process menu choice
while true; do
    clear
    show_menu
    
    case $choice in
        1)
            echo
            build_image "dev" "Debug" "Development"
            read -p "Press Enter to continue..."
            ;;
            
        2)
            echo
            build_image "test" "Release" "Test"
            
            # Run quick test
            print_info "Running health check..."
            docker run --rm -d -p 5102:8080 --name gcc-test-temp gcc-webapp:test-latest
            sleep 5
            
            if curl -f http://localhost:5102/health &>/dev/null; then
                print_success "Health check passed"
            else
                print_warning "Health check failed"
            fi
            
            docker stop gcc-test-temp &>/dev/null
            read -p "Press Enter to continue..."
            ;;
            
        3)
            echo
            read -p "Build production image? (y/n): " confirm
            if [[ $confirm == "y" ]]; then
                # Run tests first
                print_info "Running tests..."
                if dotnet test --configuration Release --no-build &>/dev/null; then
                    build_image "prod" "Release" "Production"
                    
                    # Security scan
                    security_scan "gcc-webapp:prod-latest"
                else
                    print_error "Tests failed. Fix tests before production build."
                fi
            fi
            read -p "Press Enter to continue..."
            ;;
            
        4)
            echo
            print_info "Building all environment images..."
            build_image "dev" "Debug" "Development"
            build_image "test" "Release" "Test"
            build_image "prod" "Release" "Production"
            
            echo
            docker images "gcc-webapp:*" --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
            read -p "Press Enter to continue..."
            ;;
            
        5)
            echo
            echo "Registry Options:"
            echo "1. Docker Hub"
            echo "2. Azure Container Registry"
            echo "3. AWS ECR"
            echo "4. Custom Registry"
            read -p "Select registry: " registry_choice
            
            read -p "Registry username: " registry_user
            read -s -p "Registry password: " registry_pass
            echo
            read -p "Registry URL (or dockerhub username): " registry_url
            
            # Login to registry
            echo "$registry_pass" | docker login -u "$registry_user" --password-stdin
            
            if [ $? -eq 0 ]; then
                read -p "Environment to build and push (dev/test/prod): " env
                
                case $env in
                    dev) build_image "dev" "Debug" "Development" ;;
                    test) build_image "test" "Release" "Test" ;;
                    prod) build_image "prod" "Release" "Production" ;;
                esac
                
                push_image "$registry_url" "$registry_user" "gcc-webapp:${env}-latest"
                docker logout
            else
                print_error "Registry login failed"
            fi
            read -p "Press Enter to continue..."
            ;;
            
        6)
            echo
            print_info "Building multi-architecture image..."
            
            # Check if buildx is available
            if ! docker buildx version &>/dev/null; then
                print_error "Docker buildx is not available."
                echo "Please update Docker to the latest version."
            else
                # Setup buildx
                docker buildx create --name gcc-builder --use &>/dev/null
                docker buildx inspect --bootstrap &>/dev/null
                
                read -p "Environment (dev/test/prod): " env
                
                docker buildx build \
                    --platform linux/amd64,linux/arm64 \
                    --build-arg BUILD_CONFIGURATION=Release \
                    --build-arg BUILD_NUMBER="$BUILD_NUMBER" \
                    -t "gcc-webapp:${env}-multi-${BUILD_NUMBER}" \
                    -f Dockerfile \
                    . >> "$LOG_FILE" 2>&1
                
                if [ $? -eq 0 ]; then
                    print_success "Multi-architecture image built"
                else
                    print_error "Multi-architecture build failed"
                fi
                
                docker buildx rm gcc-builder &>/dev/null
            fi
            read -p "Press Enter to continue..."
            ;;
            
        7)
            echo
            docker images "gcc-webapp:*" --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}"
            echo
            read -p "Enter image tag to scan: " image_to_scan
            
            security_scan "$image_to_scan"
            read -p "Press Enter to continue..."
            ;;
            
        8)
            echo
            read -p "Environment for clean build (dev/test/prod): " env
            
            print_info "Cleaning Docker build cache..."
            docker builder prune -f &>/dev/null
            
            print_info "Pulling latest base images..."
            docker pull mcr.microsoft.com/dotnet/aspnet:8.0
            docker pull mcr.microsoft.com/dotnet/sdk:8.0
            
            case $env in
                dev) config="Debug"; aspnet="Development" ;;
                test) config="Release"; aspnet="Test" ;;
                prod) config="Release"; aspnet="Production" ;;
            esac
            
            print_info "Building without cache..."
            docker build \
                --no-cache \
                --pull \
                --build-arg BUILD_CONFIGURATION="$config" \
                --build-arg ASPNETCORE_ENVIRONMENT="$aspnet" \
                --build-arg BUILD_NUMBER="$BUILD_NUMBER" \
                -t "gcc-webapp:${env}-clean-${BUILD_NUMBER}" \
                -f Dockerfile \
                . >> "$LOG_FILE" 2>&1
            
            if [ $? -eq 0 ]; then
                print_success "Clean build completed"
            else
                print_error "Clean build failed"
            fi
            read -p "Press Enter to continue..."
            ;;
            
        9)
            echo
            echo "Recent Docker images:"
            echo "--------------------"
            docker images "gcc-webapp:*" --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
            
            echo
            echo "Build logs:"
            echo "-----------"
            ls -lt logs/docker-build_*.log 2>/dev/null | head -10
            
            echo
            echo "Disk usage:"
            echo "-----------"
            docker system df
            
            read -p "Press Enter to continue..."
            ;;
            
        0)
            print_info "Exiting..."
            exit 0
            ;;
            
        *)
            print_error "Invalid option"
            read -p "Press Enter to continue..."
            ;;
    esac
done