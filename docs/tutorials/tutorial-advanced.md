# Advanced Tutorial: Complex Scenarios and Customization

This tutorial covers advanced scenarios including multi-project solutions, custom base images, CI/CD integration, and pre/post build scripts.

## Prerequisites

Complete the [Basic Tutorial](tutorial-basic.md) first to understand the fundamentals.

## Scenario 1: Multi-Project Solution with Shared Configuration

### Setup

Create a solution with multiple containerized projects:

```bash
# Create solution
mkdir MyMicroservices
cd MyMicroservices
dotnet new sln -n MyMicroservices

# Create projects
dotnet new webapi -n MyMicroservices.Api
dotnet new worker -n MyMicroservices.Worker
dotnet new console -n MyMicroservices.Console

# Add to solution
dotnet sln add MyMicroservices.Api MyMicroservices.Worker MyMicroservices.Console
```

### Shared Configuration with Directory.Build.props

Create `Directory.Build.props` in the solution root:

```xml
<Project>
  <PropertyGroup>
    <!-- Shared Docker Settings -->
    <DockerEnabled>true</DockerEnabled>
    <DockerRegistry>myregistry.azurecr.io</DockerRegistry>
    <DockerBaseImageVersion>8.0</DockerBaseImageVersion>
    
    <!-- Conditional settings per configuration -->
    <DockerBuildImage Condition="'$(Configuration)' == 'Release'">true</DockerBuildImage>
    <DockerBuildOnPublish Condition="'$(Configuration)' == 'Release'">true</DockerBuildOnPublish>
  </PropertyGroup>
</Project>
```

### Project-Specific Configuration

**MyMicroservices.Api/MyMicroservices.Api.csproj**:
```xml
<PropertyGroup>
  <DockerImageName>myservices-api</DockerImageName>
  <DockerImageTag>$(Version)</DockerImageTag>
  <DockerExposePort>8080</DockerExposePort>
</PropertyGroup>
```

**MyMicroservices.Worker/MyMicroservices.Worker.csproj**:
```xml
<PropertyGroup>
  <DockerImageName>myservices-worker</DockerImageName>
  <DockerImageTag>$(Version)</DockerImageTag>
  <!-- Workers don't expose ports -->
</PropertyGroup>
```

### Build All Services

```bash
# Build all (generates Dockerfiles)
dotnet build

# Publish all (builds Docker images)
dotnet publish --configuration Release

# Verify images
docker images | grep myservices
```

## Scenario 2: Custom Base Images

### Using Alpine Images

For smaller image sizes, use Alpine-based images:

```xml
<PropertyGroup>
  <DockerBaseImageRuntime>mcr.microsoft.com/dotnet/aspnet:8.0-alpine</DockerBaseImageRuntime>
  <DockerBaseImageSdk>mcr.microsoft.com/dotnet/sdk:8.0-alpine</DockerBaseImageSdk>
</PropertyGroup>
```

### Using Custom Corporate Base Images

```xml
<PropertyGroup>
  <DockerBaseImageRuntime>corporate-registry.company.com/dotnet/aspnet:8.0-hardened</DockerBaseImageRuntime>
  <DockerBaseImageSdk>corporate-registry.company.com/dotnet/sdk:8.0-hardened</DockerBaseImageSdk>
</PropertyGroup>
```

### Using Specific Digest for Immutability

```xml
<PropertyGroup>
  <DockerBaseImageRuntime>mcr.microsoft.com/dotnet/aspnet@sha256:abc123...</DockerBaseImageRuntime>
</PropertyGroup>
```

## Scenario 3: Pre/Post Build Scripts

### Pre-Build Script for Version Generation

Create `scripts/pre-build.sh`:

```bash
#!/bin/bash
set -e

echo "Generating version information..."

# Get git info
GIT_COMMIT=$(git rev-parse --short HEAD)
GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD)
BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Create version file
cat > version.json <<EOF
{
  "commit": "$GIT_COMMIT",
  "branch": "$GIT_BRANCH",
  "buildDate": "$BUILD_DATE",
  "version": "$1"
}
EOF

echo "Version file created: version.json"
```

**Configure in .csproj**:

```xml
<PropertyGroup>
  <DockerPreBuildScript>$(MSBuildProjectDirectory)/scripts/pre-build.sh</DockerPreBuildScript>
  <DockerExecutePreBuildScript>true</DockerExecutePreBuildScript>
</PropertyGroup>
```

### Post-Publish Script for Deployment

Create `scripts/deploy.ps1`:

```powershell
#!/usr/bin/env pwsh
param(
    [string]$ImageName,
    [string]$Environment = "staging"
)

Write-Host "Deploying $ImageName to $Environment..."

# Push to registry
docker push $ImageName

# Update Kubernetes
kubectl set image deployment/myapp myapp=$ImageName -n $Environment

# Wait for rollout
kubectl rollout status deployment/myapp -n $Environment

# Run smoke tests
$healthUrl = "https://$Environment.myapp.com/health"
$response = Invoke-RestMethod -Uri $healthUrl -ErrorAction Stop

if ($response.status -eq "Healthy") {
    Write-Host "Deployment successful!" -ForegroundColor Green
} else {
    Write-Host "Health check failed!" -ForegroundColor Red
    exit 1
}
```

**Configure in .csproj**:

```xml
<PropertyGroup>
  <DockerPostPublishScript>$(MSBuildProjectDirectory)/scripts/deploy.ps1</DockerPostPublishScript>
  <DockerExecutePostPublishScript>true</DockerExecutePostPublishScript>
</PropertyGroup>
```

## Scenario 4: CI/CD with Dynamic Versioning

### Using GitVersion

Install GitVersion:

```bash
dotnet tool install -g GitVersion.Tool
```

Create `GitVersion.yml`:

```yaml
mode: ContinuousDelivery
branches:
  main:
    tag: ''
    increment: Patch
  develop:
    tag: alpha
    increment: Minor
  feature:
    tag: beta
    increment: Minor
```

**GitHub Actions Workflow**:

```yaml
name: Build and Deploy

on:
  push:
    branches: [ main, develop ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # GitVersion needs full history
      
      - name: Install GitVersion
        uses: gittools/actions/gitversion/setup@v0
        with:
          versionSpec: '5.x'
      
      - name: Determine Version
        id: gitversion
        uses: gittools/actions/gitversion/execute@v0
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      
      - name: Build and Containerize
        run: |
          dotnet publish \
            --configuration Release \
            /p:Version=${{ steps.gitversion.outputs.semVer }} \
            /p:DockerImageTag=${{ steps.gitversion.outputs.semVer }} \
            /p:DockerPushImage=true
        env:
          DOCKER_REGISTRY: ghcr.io
      
      - name: Tag as Latest (main only)
        if: github.ref == 'refs/heads/main'
        run: |
          docker tag myapp:${{ steps.gitversion.outputs.semVer }} myapp:latest
          docker push myapp:latest
```

## Scenario 5: Multi-Platform Builds

### Building for ARM64

```xml
<PropertyGroup>
  <DockerBuildPlatform>linux/arm64</DockerBuildPlatform>
</PropertyGroup>
```

### Building for Multiple Platforms

Use Docker Buildx:

```bash
# Create and use buildx builder
docker buildx create --use

# Build for multiple platforms
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t myapp:latest \
  --push \
  .
```

## Scenario 6: Custom Dockerfile with Partial Generation

### Using Build-Only Mode

Keep your custom Dockerfile but automate the build:

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>false</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerfileSource>$(MSBuildProjectDirectory)/custom.Dockerfile</DockerfileSource>
</PropertyGroup>
```

Create `custom.Dockerfile`:

```dockerfile
# Custom Dockerfile with additional requirements
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Install additional dependencies
RUN apt-get update && apt-get install -y \
    curl \
    nginx \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyApp.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Custom configuration
COPY nginx.conf /etc/nginx/nginx.conf

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

## Scenario 7: Environment-Specific Configuration

### Development Configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>false</DockerBuildImage>
  
  <DockerImageTag>dev-$(USERNAME)</DockerImageTag>
  <DockerLogVerbosity>detailed</DockerLogVerbosity>
  
  <!-- Enable debugging in container -->
  <DockerBuildArgs>--build-arg CONFIGURATION=Debug</DockerBuildArgs>
</PropertyGroup>
```

### Production Configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  <DockerPushImage>true</DockerPushImage>
  
  <DockerImageTag>$(Version)</DockerImageTag>
  <DockerRegistry>$(DOCKER_REGISTRY)</DockerRegistry>
  <DockerLogVerbosity>minimal</DockerLogVerbosity>
  
  <!-- Optimize for production -->
  <DockerOptimizeLayers>true</DockerOptimizeLayers>
</PropertyGroup>
```

## Scenario 8: Integration with External Tools

### Integrating with Tye for Local Development

Install Tye:

```bash
dotnet tool install -g Microsoft.Tye
```

Create `tye.yaml`:

```yaml
name: myservices
services:
  - name: api
    project: MyMicroservices.Api/MyMicroservices.Api.csproj
    bindings:
      - port: 8080
        protocol: http
  
  - name: worker
    project: MyMicroservices.Worker/MyMicroservices.Worker.csproj
```

Run locally:

```bash
tye run
```

Build Docker images:

```bash
dotnet publish --configuration Release
```

### Integrating with Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  api:
    image: myservices-api:latest
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Database=Server=db;Database=myapp
    depends_on:
      - db
  
  worker:
    image: myservices-worker:latest
    environment:
      - DOTNET_ENVIRONMENT=Production
    depends_on:
      - db
  
  db:
    image: postgres:15
    environment:
      - POSTGRES_PASSWORD=secret
    volumes:
      - db-data:/var/lib/postgresql/data

volumes:
  db-data:
```

Build and run:

```bash
# Build images
dotnet publish --configuration Release

# Start services
docker-compose up -d
```

## Scenario 9: Security Hardening

### Creating Security-Hardened Images

```xml
<PropertyGroup>
  <!-- Use distroless images for minimal attack surface -->
  <DockerBaseImageRuntime>gcr.io/distroless/dotnet/aspnet:8.0</DockerBaseImageRuntime>
  
  <!-- Non-root user (default, but explicit) -->
  <DockerCreateUser>true</DockerCreateUser>
  <DockerUser>nonroot</DockerUser>
  <DockerUserUid>65532</DockerUserUid>
  
  <!-- Read-only filesystem -->
  <DockerReadOnlyRootFilesystem>true</DockerReadOnlyRootFilesystem>
</PropertyGroup>
```

### Scanning for Vulnerabilities

Add to CI/CD pipeline:

```yaml
- name: Scan image with Trivy
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: 'myapp:${{ github.sha }}'
    format: 'sarif'
    output: 'trivy-results.sarif'
    severity: 'CRITICAL,HIGH'

- name: Upload Trivy results
  uses: github/codeql-action/upload-sarif@v2
  with:
    sarif_file: 'trivy-results.sarif'
```

## Scenario 10: Advanced Build Arguments

### Passing Build-Time Secrets

```xml
<PropertyGroup>
  <!-- Build arguments (non-secret) -->
  <DockerBuildArgs>--build-arg VERSION=$(Version) --build-arg BUILD_DATE=$(BuildDate)</DockerBuildArgs>
  
  <!-- For secrets, use BuildKit secret mounts instead of build args -->
</PropertyGroup>
```

In Dockerfile:

```dockerfile
ARG VERSION
ARG BUILD_DATE

# Use secrets via mount (requires Docker BuildKit)
RUN --mount=type=secret,id=nuget_token \
    dotnet restore --source "https://nuget.org/api/v2" \
    --source "https://myfeed.com/nuget" \
    --api-key $(cat /run/secrets/nuget_token)
```

## Practice Exercises

Try implementing these scenarios:

1. **Multi-Environment Pipeline** - Create separate images for dev, staging, and production with appropriate tags
2. **Health Check Integration** - Add custom health checks that work with Docker's HEALTHCHECK instruction
3. **Secrets Management** - Implement proper secret handling using Docker secrets or environment variables
4. **Monitoring Integration** - Add application monitoring and tracing to your containers

## Next Steps

- [Best Practices](../articles/best-practices.md) - Production-ready patterns
- [Samples](../samples/sample-overview.md) - Real-world examples
- [API Reference](../api/index.md) - Complete property reference
- [Workflows](../articles/workflows.md) - CI/CD patterns

## Additional Resources

- [Docker Multi-Stage Builds](https://docs.docker.com/develop/develop-images/multistage-build/)
- [Docker Security Best Practices](https://docs.docker.com/develop/security-best-practices/)
- [.NET Container Images](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/net-core-net-framework-containers/official-net-docker-images)
