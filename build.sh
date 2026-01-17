#!/usr/bin/env bash
#
# Build script for JD.MSBuild.Containers and samples
# Orchestrates building the library, publishing to local NuGet feed,
# building samples, and optionally containerizing and running them.
#
# Usage:
#   ./build.sh [target] [configuration] [options]
#
# Targets: build, pack, restore-samples, build-samples, containerize, run, test, clean, all
# Configuration: debug, release (default: release)
# Options: --skip-tests, --skip-docker
#
# Examples:
#   ./build.sh all
#   ./build.sh build-samples debug
#   ./build.sh all --skip-tests

set -euo pipefail

# Paths
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOCAL_NUGET_PATH="$REPO_ROOT/local-nuget"
SAMPLES_PATH="$REPO_ROOT/samples"
SOLUTION_FILE="$REPO_ROOT/JD.MSBuild.Containers.sln"

# Defaults
TARGET="${1:-all}"
CONFIGURATION="${2:-release}"
SKIP_TESTS=false
SKIP_DOCKER=false

# Parse options
shift 1 2>/dev/null || true
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --skip-docker)
            SKIP_DOCKER=true
            shift
            ;;
        debug|release)
            CONFIGURATION="$1"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Normalize configuration
CONFIGURATION="${CONFIGURATION,,}"
case $CONFIGURATION in
    debug|d)
        CONFIGURATION="Debug"
        ;;
    release|r|*)
        CONFIGURATION="Release"
        ;;
esac

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Output functions
write_header() {
    echo -e "\n${CYAN}========================================${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}========================================${NC}\n"
}

write_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

write_info() {
    echo -e "${BLUE}→ $1${NC}"
}

write_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

write_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Clean target
invoke_clean() {
    write_header "Cleaning build artifacts"
    
    write_info "Cleaning solution..."
    dotnet clean "$SOLUTION_FILE" --configuration "$CONFIGURATION"
    
    write_info "Removing bin/obj directories..."
    find "$REPO_ROOT" -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true
    
    write_info "Removing local NuGet feed..."
    rm -rf "$LOCAL_NUGET_PATH"
    
    write_info "Removing generated Dockerfiles..."
    find "$SAMPLES_PATH" -type f -name Dockerfile -delete 2>/dev/null || true
    
    write_success "Clean completed"
}

# Build library
invoke_build() {
    write_header "Building JD.MSBuild.Containers"
    
    write_info "Restoring packages..."
    dotnet restore "$SOLUTION_FILE" --use-lock-file
    
    write_info "Building solution (Configuration: $CONFIGURATION)..."
    dotnet build "$SOLUTION_FILE" --configuration "$CONFIGURATION" --no-restore
    
    write_success "Build completed"
}

# Run tests
invoke_test() {
    if [ "$SKIP_TESTS" = true ]; then
        write_warning "Skipping tests (--skip-tests flag set)"
        return
    fi
    
    write_header "Running tests"
    
    write_info "Executing test suite..."
    dotnet test "$SOLUTION_FILE" --configuration "$CONFIGURATION" --no-build --verbosity normal
    
    write_success "Tests completed"
}

# Pack library to local NuGet feed
invoke_pack() {
    write_header "Packing to local NuGet feed"
    
    write_info "Creating local NuGet directory..."
    mkdir -p "$LOCAL_NUGET_PATH"
    
    write_info "Packing JD.MSBuild.Containers..."
    dotnet pack "$SOLUTION_FILE" --configuration "$CONFIGURATION" --no-build --output "$LOCAL_NUGET_PATH"
    
    write_info "Packages in local feed:"
    ls -lh "$LOCAL_NUGET_PATH"/*.nupkg 2>/dev/null | awk '{print "  - " $9}' || true
    
    write_success "Pack completed"
}

# Restore samples
invoke_restore_samples() {
    write_header "Restoring samples"
    
    find "$SAMPLES_PATH" -name "*.csproj" -type f | while read -r sample; do
        sample_name=$(basename "$(dirname "$sample")")
        write_info "Restoring $sample_name..."
        dotnet restore "$sample"
    done
    
    write_success "Sample restore completed"
}

# Build samples
invoke_build_samples() {
    write_header "Building samples"
    
    find "$SAMPLES_PATH" -name "*.csproj" -type f | while read -r sample; do
        sample_name=$(basename "$(dirname "$sample")")
        write_info "Building $sample_name..."
        dotnet build "$sample" --configuration "$CONFIGURATION"
    done
    
    write_success "Sample build completed"
}

# Containerize samples
invoke_containerize() {
    if [ "$SKIP_DOCKER" = true ]; then
        write_warning "Skipping containerization (--skip-docker flag set)"
        return
    fi
    
    write_header "Containerizing samples"
    
    # Check Docker availability
    if ! command -v docker &> /dev/null; then
        write_error "Docker not found. Please install Docker to containerize samples."
        return
    fi
    
    docker_version=$(docker --version)
    write_info "Docker found: $docker_version"
    
    find "$SAMPLES_PATH" -name "*.csproj" -type f | while read -r sample; do
        sample_name=$(basename "$(dirname "$sample")")
        sample_dir=$(dirname "$sample")
        
        write_info "Publishing and containerizing $sample_name..."
        
        # Publish triggers Docker build when DockerBuildOnPublish=true
        dotnet publish "$sample" --configuration "$CONFIGURATION"
        
        # Verify Dockerfile was generated
        if [ -f "$sample_dir/Dockerfile" ]; then
            write_success "Dockerfile generated for $sample_name"
        else
            write_warning "Dockerfile not found for $sample_name"
        fi
    done
    
    write_info "\nListing Docker images:"
    docker images | grep -E "sample|REPOSITORY" || true
    
    write_success "Containerization completed"
}

# Run containerized samples
invoke_run() {
    if [ "$SKIP_DOCKER" = true ]; then
        write_warning "Skipping container run (--skip-docker flag set)"
        return
    fi
    
    write_header "Running containerized samples"
    
    write_info "This is a placeholder for integration test execution"
    write_info "Container orchestration will be implemented in integration tests"
    
    write_success "Run completed"
}

# Main execution
cat << "EOF"
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║            JD.MSBuild.Containers Build Script                 ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝
EOF

write_info "Target: $TARGET"
write_info "Configuration: $CONFIGURATION"
write_info "Skip Tests: $SKIP_TESTS"
write_info "Skip Docker: $SKIP_DOCKER"
echo ""

# Execute target
case "${TARGET,,}" in
    clean)
        invoke_clean
        ;;
    build)
        invoke_build
        ;;
    test)
        invoke_build
        invoke_test
        ;;
    pack)
        invoke_build
        invoke_test
        invoke_pack
        ;;
    restore-samples)
        invoke_restore_samples
        ;;
    build-samples)
        invoke_restore_samples
        invoke_build_samples
        ;;
    containerize)
        invoke_restore_samples
        invoke_build_samples
        invoke_containerize
        ;;
    run)
        invoke_run
        ;;
    all)
        invoke_clean
        invoke_build
        invoke_test
        invoke_pack
        invoke_restore_samples
        invoke_build_samples
        invoke_containerize
        ;;
    *)
        write_error "Unknown target: $TARGET"
        echo "Available targets: build, pack, restore-samples, build-samples, containerize, run, test, clean, all"
        exit 1
        ;;
esac

echo ""
write_success "Build script completed successfully!"
exit 0
