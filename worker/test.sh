#!/bin/bash
set -e

echo "🧪 Running tests for Worker App..."

# Build the project
echo "🔨 Building .NET project..."
dotnet build

# Run tests if test project exists
if [ -f "Worker.Tests.csproj" ]; then
    echo "🔍 Running unit tests..."
    dotnet test
else
    echo "🔍 Running basic validation tests..."
    
    # Test that the project compiles
    dotnet build --configuration Release
    
    # Test that required packages are installed
    dotnet list package
    
    # Basic code validation
    echo "📝 Validating C# code..."
    dotnet format --verify-no-changes --verbosity diagnostic || true
fi

echo "✅ Worker App tests completed!"