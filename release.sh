#!/usr/bin/env bash
set -e

# Parse VersionPrefix from .csproj file
CSPROJ_PATH="src/FlexLabs.EntityFrameworkCore.Upsert/FlexLabs.EntityFrameworkCore.Upsert.csproj"
if [ ! -f "$CSPROJ_PATH" ]; then
    echo "-- Error: Could not find $CSPROJ_PATH --"
    exit 1
fi

VERSION=$(grep -oP '<VersionPrefix>\K[^<]+' "$CSPROJ_PATH")
if [ -z "$VERSION" ]; then
    echo "-- Error: Could not parse VersionPrefix from $CSPROJ_PATH --"
    exit 1
fi

SUFFIX=""
# Handle version suffix if provided as first argument
if [ -n "$1" ]; then
    SUFFIX="--version-suffix $1"
    VERSION="$VERSION-$1"
fi

echo "-- Version: $VERSION --"

# Build solution in release mode
echo "-- Building solution in release mode"
OPENSSL_ENABLE_SHA1_SIGNATURES=1 dotnet pack \
    -c Release \
    -p:ContinuousIntegrationBuild=true \
    -p:PostBuildEvent="signtool-helper FlexLabs.EntityFrameworkCore.Upsert.dll" \
    -o dist \
    $SUFFIX

# Sign the nuget package using signtool-helper alias
echo "-- Signing the nuget package"
signtool-helper "dist/FlexLabs.EntityFrameworkCore.Upsert.${VERSION}.nupkg"
echo "-- Build completed successfully --"
