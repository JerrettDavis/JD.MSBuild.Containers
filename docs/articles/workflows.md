# Workflows

This guide covers common development and CI/CD workflows using JD.MSBuild.Containers.

## Local Development Workflows

### Workflow 1: Review-First (Generate-Only)

**Use Case**: You want to review Dockerfiles before building images.

#### Configuration

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <!-- DockerGenerateDockerfile defaults to true -->
  <!-- DockerBuildImage defaults to false -->
</PropertyGroup>
```

#### Workflow Steps

```bash
# 1. Make code changes
vim Program.cs

# 2. Build project (generates Dockerfile)
dotnet build

# 3. Review generated Dockerfile
cat Dockerfile

# 4. Commit Dockerfile to source control
git add Dockerfile
git commit -m "Update Dockerfile"

# 5. Build Docker image manually when ready
docker build -t myapp:latest .

# 6. Run container
docker run -p 8080:8080 myapp:latest
```

**Benefits**:
- Review and understand Dockerfiles before building
- Commit Dockerfiles for team visibility
- Manual control over image builds

### Workflow 2: Fully Automated (Build-On-Publish)

**Use Case**: You want complete automation during publish.

#### Configuration

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  
  <DockerImageName>myapp</DockerImageName>
  <DockerImageTag>dev</DockerImageTag>
</PropertyGroup>
```

#### Workflow Steps

```bash
# 1. Make code changes
vim Program.cs

# 2. Publish (generates Dockerfile and builds image)
dotnet publish

# 3. Run container immediately
docker run -p 8080:8080 myapp:dev

# 4. Test your changes
curl http://localhost:8080/api/health
```

**Benefits**:
- Single command builds everything
- Fast inner-loop development
- Immediate container testing

### Workflow 3: Build-Only (Custom Dockerfile)

**Use Case**: You maintain a custom Dockerfile but want automated builds.

#### Configuration

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>false</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  
  <!-- Optional: specify custom Dockerfile location -->
  <DockerfileSource>$(MSBuildProjectDirectory)/deploy/Dockerfile</DockerfileSource>
</PropertyGroup>
```

#### Workflow Steps

```bash
# 1. Edit custom Dockerfile
vim deploy/Dockerfile

# 2. Make code changes
vim Program.cs

# 3. Publish (builds image using custom Dockerfile)
dotnet publish

# 4. Run container
docker run -p 8080:8080 myapp:latest
```

**Benefits**:
- Full control over Dockerfile
- Automated build process
- Flexibility for complex scenarios

### Workflow 4: Hot Reload with Docker

**Use Case**: Develop inside containers with live reload.

#### Configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DockerEnabled>true</DockerEnabled>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnBuild>true</DockerBuildOnBuild>
  <DockerRunContainer>true</DockerRunContainer>
  <DockerRunOnBuild>true</DockerRunOnBuild>
  
  <!-- Mount source for hot reload -->
  <DockerVolumeMappings>$(MSBuildProjectDirectory):/src:ro</DockerVolumeMappings>
  <DockerEnvironmentVariables>ASPNETCORE_ENVIRONMENT=Development;DOTNET_USE_POLLING_FILE_WATCHER=true</DockerEnvironmentVariables>
</PropertyGroup>
```

#### Workflow Steps

```bash
# 1. Start development with hot reload
dotnet watch

# Container automatically:
# - Rebuilds on code changes
# - Restarts application
# - Reloads browser

# 2. Make changes and see them immediately
vim Program.cs
# Auto-reload happens
```

**Benefits**:
- Container-based development
- Hot reload support
- Production-like environment

## CI/CD Workflows

### GitHub Actions

#### Basic CI Workflow

```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-containerize:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
      
      - name: Test
        run: dotnet test --no-build --configuration Release
      
      - name: Publish and Build Docker Image
        run: dotnet publish --configuration Release
        # JD.MSBuild.Containers handles Docker build
      
      - name: Verify Image
        run: docker images | grep myapp
```

#### Versioned Release Workflow

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  release:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      
      - name: Get version from tag
        id: version
        run: echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT
      
      - name: Log in to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      
      - name: Build and Publish
        run: |
          dotnet publish \
            --configuration Release \
            /p:DockerImageTag=${{ steps.version.outputs.VERSION }} \
            /p:DockerRegistry=${{ env.REGISTRY }} \
            /p:DockerImageName=${{ env.IMAGE_NAME }} \
            /p:DockerPushImage=true \
            /p:DockerPushOnPublish=true
```

#### Multi-Environment Deployment

```yaml
name: Deploy

on:
  push:
    branches: [ main, develop, staging ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      
      - name: Determine Environment
        id: env
        run: |
          if [ "${{ github.ref }}" == "refs/heads/main" ]; then
            echo "ENV=production" >> $GITHUB_OUTPUT
            echo "TAG=latest" >> $GITHUB_OUTPUT
          elif [ "${{ github.ref }}" == "refs/heads/staging" ]; then
            echo "ENV=staging" >> $GITHUB_OUTPUT
            echo "TAG=staging" >> $GITHUB_OUTPUT
          else
            echo "ENV=development" >> $GITHUB_OUTPUT
            echo "TAG=dev" >> $GITHUB_OUTPUT
          fi
      
      - name: Build and Push
        run: |
          dotnet publish \
            --configuration Release \
            /p:DockerImageTag=${{ steps.env.outputs.TAG }} \
            /p:DockerRegistry=myregistry.azurecr.io \
            /p:DockerPushImage=true
      
      - name: Deploy to Environment
        run: |
          # Your deployment logic here
          echo "Deploying to ${{ steps.env.outputs.ENV }}"
```

### Azure DevOps

#### Pipeline YAML

```yaml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  dockerRegistry: 'myregistry.azurecr.io'

stages:
  - stage: Build
    jobs:
      - job: BuildAndContainerize
        steps:
          - task: UseDotNet@2
            inputs:
              version: '8.x'
          
          - task: DotNetCoreCLI@2
            displayName: 'Restore'
            inputs:
              command: 'restore'
          
          - task: DotNetCoreCLI@2
            displayName: 'Build'
            inputs:
              command: 'build'
              arguments: '--configuration $(buildConfiguration)'
          
          - task: DotNetCoreCLI@2
            displayName: 'Test'
            inputs:
              command: 'test'
              arguments: '--configuration $(buildConfiguration) --no-build'
          
          - task: DotNetCoreCLI@2
            displayName: 'Publish and Build Docker Image'
            inputs:
              command: 'publish'
              arguments: >
                --configuration $(buildConfiguration)
                /p:DockerBuildImage=true
                /p:DockerRegistry=$(dockerRegistry)
                /p:DockerImageTag=$(Build.BuildId)
          
          - task: Docker@2
            displayName: 'Push Docker Image'
            inputs:
              command: 'push'
              repository: '$(dockerRegistry)/myapp'
              tags: '$(Build.BuildId)'
```

### GitLab CI

#### .gitlab-ci.yml

```yaml
stages:
  - build
  - test
  - containerize
  - deploy

variables:
  DOCKER_REGISTRY: registry.gitlab.com
  IMAGE_NAME: $CI_REGISTRY_IMAGE

build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet restore
    - dotnet build --configuration Release --no-restore
  artifacts:
    paths:
      - bin/
      - obj/

test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet test --configuration Release --no-build

containerize:
  stage: containerize
  image: mcr.microsoft.com/dotnet/sdk:8.0
  services:
    - docker:dind
  script:
    - dotnet publish --configuration Release
      /p:DockerBuildImage=true
      /p:DockerImageTag=$CI_COMMIT_SHORT_SHA
      /p:DockerRegistry=$DOCKER_REGISTRY
  only:
    - main
    - develop

deploy:
  stage: deploy
  script:
    - docker push $IMAGE_NAME:$CI_COMMIT_SHORT_SHA
  only:
    - main
```

## Local NuGet Package Workflow

When developing JD.MSBuild.Containers itself or testing local changes:

### Step 1: Pack Local Version

```bash
# Build and pack the library
dotnet pack src/JD.MSBuild.Containers --configuration Release --output ./local-nuget

# Version will be in the .nupkg filename
ls ./local-nuget/
# JD.MSBuild.Containers.1.0.0-local.nupkg
```

### Step 2: Configure Local Feed

Create or update `NuGet.Config` in your test project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="../local-nuget" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### Step 3: Reference Local Package

```xml
<ItemGroup>
  <PackageReference Include="JD.MSBuild.Containers" Version="1.0.0-local" />
</ItemGroup>
```

### Step 4: Test Changes

```bash
# Clear NuGet cache to ensure fresh package
dotnet nuget locals all --clear

# Restore with local package
dotnet restore

# Build/publish to test
dotnet publish
```

## Advanced Workflows

### Pre-Build Script Workflow

Execute custom logic before Docker build:

#### Configuration

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerPreBuildScript>$(MSBuildProjectDirectory)/scripts/pre-build.sh</DockerPreBuildScript>
</PropertyGroup>
```

#### Pre-Build Script (pre-build.sh)

```bash
#!/bin/bash
set -e

echo "Running pre-build tasks..."

# Generate version file
echo "VERSION=$(git describe --tags --always)" > version.txt

# Download external dependencies
curl -o config.json https://config-service/api/config

# Validate environment
if [ -z "$API_KEY" ]; then
  echo "ERROR: API_KEY not set"
  exit 1
fi

echo "Pre-build completed successfully"
```

### Post-Publish Script Workflow

Execute deployment after successful build:

#### Configuration

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerPushImage>true</DockerPushImage>
  <DockerPostPublishScript>$(MSBuildProjectDirectory)/scripts/deploy.ps1</DockerPostPublishScript>
</PropertyGroup>
```

#### Post-Publish Script (deploy.ps1)

```powershell
#!/usr/bin/env pwsh
param(
    [string]$ImageName = "myapp:latest",
    [string]$Environment = "staging"
)

Write-Host "Deploying $ImageName to $Environment..."

# Update Kubernetes deployment
kubectl set image deployment/myapp myapp=$ImageName -n $Environment

# Wait for rollout
kubectl rollout status deployment/myapp -n $Environment

# Run smoke tests
Invoke-RestMethod -Uri "https://$Environment.myapp.com/health"

Write-Host "Deployment completed successfully"
```

### Conditional Containerization

Different settings per configuration:

```xml
<!-- Debug: Generate only -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>false</DockerBuildImage>
</PropertyGroup>

<!-- Release: Full automation -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  <DockerPushImage>true</DockerPushImage>
  <DockerPushOnPublish>true</DockerPushOnPublish>
</PropertyGroup>
```

## Troubleshooting Common Workflows

### Issue: Slow Docker Builds in CI

**Solution**: Use Docker layer caching

```yaml
# GitHub Actions
- name: Set up Docker Buildx
  uses: docker/setup-buildx-action@v3

- name: Build and Push
  uses: docker/build-push-action@v5
  with:
    context: .
    cache-from: type=gha
    cache-to: type=gha,mode=max
```

### Issue: Credentials Not Available in CI

**Solution**: Use environment variables

```xml
<!-- .csproj -->
<PropertyGroup>
  <DockerRegistry Condition="'$(DOCKER_REGISTRY)' != ''">$(DOCKER_REGISTRY)</DockerRegistry>
</PropertyGroup>
```

```yaml
# GitHub Actions
- name: Publish
  run: dotnet publish
  env:
    DOCKER_REGISTRY: ghcr.io
```

### Issue: Different Image Names Per Branch

**Solution**: Dynamic image tags

```bash
# Bash script
BRANCH_NAME=$(git rev-parse --abbrev-ref HEAD)
TAG=$(echo $BRANCH_NAME | tr '/' '-')

dotnet publish /p:DockerImageTag=$TAG
```

## Next Steps

- [Best Practices](best-practices.md) - Learn recommended patterns
- [Samples](../samples/sample-overview.md) - See working examples
- [API Reference](../api/index.md) - Explore all configuration options
