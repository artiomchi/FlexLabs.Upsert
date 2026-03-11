#!/usr/bin/env bash
set -e

# Color codes for output
SUCCESS_ICON='\033[0;32m✓\033[0m'
FAILURE_ICON='\033[0;31m✗\033[0m'

# Parse arguments
TARGET="${1:-both}"

if [ "$TARGET" != "nuget" ] && [ "$TARGET" != "github" ] && [ "$TARGET" != "both" ]; then
    echo "Error: Invalid target '$TARGET'. Valid options: nuget, github, both (or leave blank for both)"
    exit 1
fi

# Find the latest package in dist/
echo "-- Looking for the latest package in dist/"
if [ ! -d "dist" ]; then
    echo "Error: dist/ directory not found. Have you run release.sh?"
    exit 1
fi

PACKAGE_PATH=$(ls -t dist/*.nupkg 2>/dev/null | head -n1)
if [ -z "$PACKAGE_PATH" ]; then
    echo "Error: No .nupkg files found in dist/"
    exit 1
fi

PACKAGE_NAME=$(basename "$PACKAGE_PATH")
PACKAGE_SIZE=$(du -h "$PACKAGE_PATH" | cut -f1)

# Determine which registries to publish to
PUBLISH_NUGET=false
PUBLISH_GITHUB=false

if [ "$TARGET" = "nuget" ] || [ "$TARGET" = "both" ]; then
    PUBLISH_NUGET=true
fi

if [ "$TARGET" = "github" ] || [ "$TARGET" = "both" ]; then
    PUBLISH_GITHUB=true
fi

# Validate environment variables
if [ "$PUBLISH_NUGET" = true ] && [ -z "$NUGET_API_KEY" ]; then
    echo "Error: NUGET_API_KEY environment variable is not set"
    exit 1
fi

if [ "$PUBLISH_GITHUB" = true ] && [ -z "$GITHUB_TOKEN" ]; then
    echo "Error: GITHUB_TOKEN environment variable is not set"
    exit 1
fi

# Display confirmation prompt
echo ""
echo "=========================================="
echo "Package:      $PACKAGE_NAME"
echo "Size:         $PACKAGE_SIZE"
echo "Target(s):    $([ "$PUBLISH_NUGET" = true ] && echo -n "NuGet.org ")$([ "$PUBLISH_GITHUB" = true ] && echo -n "GitHub Packages")"
echo "=========================================="
echo ""
read -p "Proceed with publish? (yes/y or Enter): " CONFIRM

if [ "$CONFIRM" != "yes" ] && [ "$CONFIRM" != "y" ] && [ "$CONFIRM" != "" ]; then
    echo "Publish cancelled"
    exit 0
fi

# Initialize status tracking
NUGET_STATUS=""
GITHUB_STATUS=""
FAILED=false

# Publish to NuGet.org
if [ "$PUBLISH_NUGET" = true ]; then
    echo ""
    echo "-- Publishing to NuGet.org"
    set +e
    dotnet nuget push "$PACKAGE_PATH" \
        --api-key "$NUGET_API_KEY" \
        --source https://api.nuget.org/v3/index.json \
        --skip-duplicate
    NUGET_EXIT_CODE=$?
    set -e
    
    if [ $NUGET_EXIT_CODE -eq 0 ]; then
        NUGET_STATUS="$SUCCESS_ICON"
    else
        NUGET_STATUS="$FAILURE_ICON"
        FAILED=true
    fi
fi

# Publish to GitHub Packages
if [ "$PUBLISH_GITHUB" = true ]; then
    echo ""
    echo "-- Publishing to GitHub Packages"
    set +e
    dotnet nuget push "$PACKAGE_PATH" \
        --api-key "$GITHUB_TOKEN" \
        --source https://nuget.pkg.github.com/artiomchi/index.json \
        --skip-duplicate
    GITHUB_EXIT_CODE=$?
    set -e
    
    if [ $GITHUB_EXIT_CODE -eq 0 ]; then
        GITHUB_STATUS="$SUCCESS_ICON"
    else
        GITHUB_STATUS="$FAILURE_ICON"
        FAILED=true
    fi
fi

# Display summary
echo ""
echo "=========================================="
echo "Publishing Summary:"
echo "=========================================="

if [ "$PUBLISH_NUGET" = true ]; then
    echo -e "${NUGET_STATUS} NuGet.org"
fi

if [ "$PUBLISH_GITHUB" = true ]; then
    echo -e "${GITHUB_STATUS} GitHub Packages"
fi

echo "=========================================="

if [ "$FAILED" = true ]; then
    exit 1
fi

echo ""
echo "-- Publish completed successfully!"
