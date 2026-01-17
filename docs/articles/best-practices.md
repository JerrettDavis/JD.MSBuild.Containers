# Best Practices

This guide covers recommended patterns, approaches, and best practices for using JD.MSBuild.Containers effectively.

## Configuration Best Practices

### 1. Use Directory.Build.props for Shared Settings

For multi-project solutions, define common Docker settings in `Directory.Build.props`:

```xml
<!-- Directory.Build.props at solution root -->
<Project>
  <PropertyGroup>
    <!-- Common Docker settings -->
    <DockerRegistry>myregistry.azurecr.io</DockerRegistry>
    <DockerBaseImageRuntime>mcr.microsoft.com/dotnet/aspnet</DockerBaseImageRuntime>
    <DockerBaseImageSdk>mcr.microsoft.com/dotnet/sdk</DockerBaseImageSdk>
    <DockerBaseImageVersion>8.0</DockerBaseImageVersion>
    
    <!-- Default to generate-only in Debug -->
    <DockerEnabled Condition="'$(Configuration)' == 'Debug'">true</DockerEnabled>
    <DockerBuildImage Condition="'$(Configuration)' == 'Debug'">false</DockerBuildImage>
    
    <!-- Full automation in Release -->
    <DockerEnabled Condition="'$(Configuration)' == 'Release'">true</DockerEnabled>
    <DockerBuildImage Condition="'$(Configuration)' == 'Release'">true</DockerBuildImage>
    <DockerBuildOnPublish Condition="'$(Configuration)' == 'Release'">true</DockerBuildOnPublish>
  </PropertyGroup>
</Project>
```

**Benefits**:
- Consistency across all projects
- Single source of truth for Docker configuration
- Easier to maintain and update

### 2. Use Conditional Properties for Different Environments

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DockerImageTag>dev-$(USERNAME)</DockerImageTag>
  <DockerBuildImage>false</DockerBuildImage>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DockerImageTag>$(Version)</DockerImageTag>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerPushImage>true</DockerPushImage>
</PropertyGroup>
```

### 3. Pin Base Image Versions

Always specify explicit versions for base images in production:

```xml
<!-- ❌ Bad: Unpredictable -->
<DockerBaseImageVersion>latest</DockerBaseImageVersion>

<!-- ✅ Good: Explicit version -->
<DockerBaseImageVersion>8.0.1</DockerBaseImageVersion>

<!-- ✅ Better: Full digest for immutability -->
<DockerBaseImageRuntime>mcr.microsoft.com/dotnet/aspnet@sha256:abc123...</DockerBaseImageRuntime>
```

### 4. Use Semantic Versioning for Image Tags

```xml
<!-- Use GitVersion or similar for automatic versioning -->
<PropertyGroup>
  <DockerImageTag>$(GitVersion_SemVer)</DockerImageTag>
  <!-- Results in: myapp:1.2.3 -->
</PropertyGroup>
```

### 5. Externalize Sensitive Configuration

Never hardcode credentials or secrets:

```xml
<!-- ❌ Bad: Hardcoded credentials -->
<DockerRegistry>myregistry.azurecr.io</DockerRegistry>
<DockerRegistryUsername>admin</DockerRegistryUsername>
<DockerRegistryPassword>MyP@ssw0rd</DockerRegistryPassword>

<!-- ✅ Good: Use environment variables or CI/CD secrets -->
<DockerRegistry>$(DOCKER_REGISTRY)</DockerRegistry>
<!-- Authentication handled by docker login separately -->
```

## Performance Best Practices

### 1. Enable Fingerprinting (Default)

Keep fingerprinting enabled for incremental builds:

```xml
<!-- Default behavior - no need to set -->
<DockerUseFingerprinting>true</DockerUseFingerprinting>
```

**Impact**: Skips Dockerfile regeneration when project hasn't changed, saving 1-5 seconds per build.

### 2. Optimize Docker Build Context

Exclude unnecessary files from Docker build context:

**Create .dockerignore**:
```
# .dockerignore
**/bin/
**/obj/
**/.git/
**/.vs/
**/.vscode/
**/node_modules/
**/TestResults/
*.md
.gitignore
.editorconfig
```

**Impact**: Reduces context size, speeds up builds by 30-50%.

### 3. Use Multi-Stage Builds (Default)

Keep multi-stage builds enabled:

```xml
<!-- Default behavior - no need to set -->
<DockerUseMultiStage>true</DockerUseMultiStage>
```

**Benefits**:
- Smaller final images (50-70% reduction)
- Better layer caching
- Faster subsequent builds

### 4. Minimize Dockerfile Regeneration

Only regenerate when necessary:

```xml
<!-- Generate once, then use existing Dockerfile -->
<PropertyGroup Condition="Exists('Dockerfile')">
  <DockerGenerateDockerfile>false</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
</PropertyGroup>
```

## Security Best Practices

### 1. Use Non-Root Users (Default)

JD.MSBuild.Containers automatically creates non-root users:

```xml
<!-- Default behavior - recommended to keep enabled -->
<DockerCreateUser>true</DockerCreateUser>
<DockerUser>app</DockerUser>
```

**Why**: Running as root in containers is a security risk.

### 2. Scan Images for Vulnerabilities

Integrate security scanning in CI/CD:

```yaml
# GitHub Actions
- name: Run Trivy vulnerability scanner
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: 'myapp:${{ github.sha }}'
    format: 'sarif'
    output: 'trivy-results.sarif'

- name: Upload Trivy results to GitHub Security
  uses: github/codeql-action/upload-sarif@v2
  with:
    sarif_file: 'trivy-results.sarif'
```

### 3. Keep Base Images Updated

Regularly update base image versions:

```bash
# Check for updates
docker pull mcr.microsoft.com/dotnet/aspnet:8.0

# Update in .csproj
<DockerBaseImageVersion>8.0.2</DockerBaseImageVersion>
```

### 4. Don't Embed Secrets in Images

Never include secrets in Docker images:

```xml
<!-- ❌ Bad: Secrets in environment variables -->
<DockerEnvironmentVariables>API_KEY=sk-12345;DB_PASSWORD=secret</DockerEnvironmentVariables>

<!-- ✅ Good: Pass secrets at runtime -->
<!-- docker run -e API_KEY=${API_KEY} myapp:latest -->
```

### 5. Use Minimal Base Images

Prefer minimal/slim images when possible:

```xml
<DockerBaseImageRuntime>mcr.microsoft.com/dotnet/aspnet:8.0-alpine</DockerBaseImageRuntime>
```

**Benefits**:
- Smaller attack surface
- Fewer vulnerabilities
- Smaller image size

## CI/CD Best Practices

### 1. Separate Build and Deploy Steps

```yaml
# Build job - create and test image
build:
  - dotnet build
  - dotnet test
  - dotnet publish  # Creates Docker image
  - docker run myapp:test
  - run-integration-tests

# Deploy job - push to registry
deploy:
  needs: build
  - docker push myapp:$VERSION
```

### 2. Tag Images with Commit SHA

```yaml
- name: Build with commit SHA
  run: |
    dotnet publish \
      /p:DockerImageTag=${{ github.sha }}
```

**Benefits**:
- Traceability
- Ability to rollback to any commit
- Prevents tag collisions

### 3. Use Build Matrix for Multi-Platform

```yaml
strategy:
  matrix:
    platform: [linux/amd64, linux/arm64]
steps:
  - name: Build for platform
    run: |
      dotnet publish \
        /p:DockerBuildPlatform=${{ matrix.platform }}
```

### 4. Cache Docker Layers

```yaml
- name: Set up Docker Buildx
  uses: docker/setup-buildx-action@v3

- name: Build with cache
  uses: docker/build-push-action@v5
  with:
    cache-from: type=gha
    cache-to: type=gha,mode=max
```

### 5. Validate Before Pushing

Always test images before pushing to registry:

```yaml
- name: Build image
  run: dotnet publish

- name: Test image
  run: |
    docker run -d -p 8080:8080 --name test myapp:latest
    sleep 5
    curl --fail http://localhost:8080/health || exit 1
    docker stop test

- name: Push image
  run: docker push myapp:latest
```

## Development Best Practices

### 1. Review Generated Dockerfiles

Periodically review generated Dockerfiles to understand what's being created:

```bash
dotnet build
cat Dockerfile
```

### 2. Commit Generated Dockerfiles

For transparency and review:

```bash
# Generate Dockerfile
dotnet build

# Review changes
git diff Dockerfile

# Commit if acceptable
git add Dockerfile
git commit -m "Update Dockerfile for new dependencies"
```

### 3. Use Generate-Only Mode Locally

For local development, prefer generate-only mode:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DockerEnabled>true</DockerEnabled>
  <DockerBuildImage>false</DockerBuildImage>
</PropertyGroup>
```

**Rationale**: Faster builds, review Dockerfiles before building images.

### 4. Test in Containers Regularly

Don't wait until CI to test in containers:

```bash
# Local container test
dotnet publish
docker run -p 8080:8080 myapp:dev
curl http://localhost:8080/health
```

### 5. Keep Docker Running Locally

Ensure Docker is always running during development:

```bash
# Add to shell profile
if ! docker info > /dev/null 2>&1; then
    echo "⚠️  Docker is not running!"
fi
```

## Project Structure Best Practices

### 1. Organize Multi-Project Solutions

```
MySolution/
├── Directory.Build.props          ← Shared Docker settings
├── src/
│   ├── MyApp.Api/
│   │   ├── MyApp.Api.csproj       ← API-specific Docker settings
│   │   └── Dockerfile             ← Generated Dockerfile
│   └── MyApp.Worker/
│       ├── MyApp.Worker.csproj    ← Worker-specific Docker settings
│       └── Dockerfile             ← Generated Dockerfile
└── tests/
    └── MyApp.Tests/
        └── MyApp.Tests.csproj     ← No Docker settings
```

### 2. Use Scripts for Complex Scenarios

For complex pre/post build logic, use external scripts:

```xml
<PropertyGroup>
  <DockerPreBuildScript>$(MSBuildProjectDirectory)/scripts/pre-build.sh</DockerPreBuildScript>
  <DockerPostPublishScript>$(MSBuildProjectDirectory)/scripts/deploy.ps1</DockerPostPublishScript>
</PropertyGroup>
```

**scripts/pre-build.sh**:
```bash
#!/bin/bash
set -e

# Complex logic here
echo "Running pre-build tasks..."
```

### 3. Maintain Separate .dockerignore

Keep `.dockerignore` alongside Dockerfile:

```
MyApp/
├── MyApp.csproj
├── Dockerfile           ← Generated
├── .dockerignore        ← Maintained manually
└── Program.cs
```

## Image Tagging Best Practices

### 1. Use Multiple Tags

Tag images with multiple identifiers:

```bash
# In CI/CD pipeline
VERSION=1.2.3
COMMIT=$(git rev-parse --short HEAD)

# Tag with version
docker tag myapp:build myapp:$VERSION

# Tag with commit
docker tag myapp:build myapp:$COMMIT

# Tag as latest
docker tag myapp:build myapp:latest

# Push all tags
docker push myapp:$VERSION
docker push myapp:$COMMIT
docker push myapp:latest
```

### 2. Semantic Versioning Tags

```
myapp:1.2.3        ← Full version
myapp:1.2          ← Minor version
myapp:1            ← Major version
myapp:latest       ← Latest stable
```

### 3. Environment Tags

```
myapp:dev          ← Development
myapp:staging      ← Staging
myapp:prod         ← Production
```

## Monitoring and Logging Best Practices

### 1. Include Health Checks

Ensure your application has health endpoints:

```csharp
// ASP.NET Core
app.MapHealthChecks("/health");
```

Configure Docker health check:

```xml
<PropertyGroup>
  <DockerHealthCheck>CMD curl --fail http://localhost:8080/health || exit 1</DockerHealthCheck>
  <DockerHealthCheckInterval>30s</DockerHealthCheckInterval>
  <DockerHealthCheckTimeout>3s</DockerHealthCheckTimeout>
  <DockerHealthCheckRetries>3</DockerHealthCheckRetries>
</PropertyGroup>
```

### 2. Use Structured Logging

Configure proper logging in containers:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddJsonConsole(); // Structured JSON logs
});
```

### 3. Set Log Verbosity

Control MSBuild/Docker log verbosity:

```xml
<!-- Minimal logs for CI -->
<DockerLogVerbosity Condition="'$(CI)' == 'true'">minimal</DockerLogVerbosity>

<!-- Detailed logs for troubleshooting -->
<DockerLogVerbosity Condition="'$(DEBUG_BUILD)' == 'true'">detailed</DockerLogVerbosity>
```

## Testing Best Practices

### 1. Test Generated Dockerfiles

Create tests for Dockerfile generation:

```csharp
[Test]
public void GeneratedDockerfile_ShouldUseCorrectBaseImage()
{
    // Build project
    var result = DotNetBuild("MyApp.csproj");
    
    // Read generated Dockerfile
    var dockerfile = File.ReadAllText("Dockerfile");
    
    // Assert
    Assert.That(dockerfile, Contains.Substring("FROM mcr.microsoft.com/dotnet/aspnet:8.0"));
}
```

### 2. Integration Tests with Containers

Test your application in containers:

```csharp
[Test]
public async Task Container_ShouldRespondToHealthCheck()
{
    // Start container
    var container = await DockerHelper.RunAsync("myapp:test", port: 8080);
    
    try
    {
        // Wait for startup
        await Task.Delay(5000);
        
        // Test health endpoint
        var response = await httpClient.GetAsync("http://localhost:8080/health");
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
    finally
    {
        await container.StopAsync();
    }
}
```

### 3. Validate Image Size

Ensure images don't grow unexpectedly:

```csharp
[Test]
public void Image_ShouldBeUnder500MB()
{
    var image = DockerHelper.InspectImage("myapp:latest");
    var sizeMB = image.Size / 1024 / 1024;
    
    Assert.That(sizeMB, Is.LessThan(500));
}
```

## Documentation Best Practices

### 1. Document Docker Configuration

Add comments to explain Docker settings:

```xml
<PropertyGroup>
  <!-- Enable Docker for production builds only -->
  <DockerEnabled Condition="'$(Configuration)' == 'Release'">true</DockerEnabled>
  
  <!-- Use Azure Container Registry for production images -->
  <DockerRegistry>myregistry.azurecr.io</DockerRegistry>
  
  <!-- Version images using GitVersion -->
  <DockerImageTag>$(GitVersion_SemVer)</DockerImageTag>
</PropertyGroup>
```

### 2. Maintain README with Docker Instructions

Include Docker commands in project README:

```markdown
## Building Docker Image

### Local Development
```bash
dotnet build  # Generates Dockerfile
docker build -t myapp:dev .
docker run -p 8080:8080 myapp:dev
```

### Production Build
```bash
dotnet publish --configuration Release  # Builds image
docker push myregistry.azurecr.io/myapp:latest
```
```

### 3. Document Custom Dockerfile Modifications

If using custom Dockerfiles, document changes:

```dockerfile
# Custom Dockerfile
# Modifications from generated version:
# 1. Added nginx for reverse proxy
# 2. Custom healthcheck interval
# 3. Additional apt packages for dependencies
```

## Anti-Patterns to Avoid

### ❌ Don't Commit Build Artifacts

```xml
<!-- Bad: Building inside source control -->
<DockerBuildOnBuild>true</DockerBuildOnBuild>
```

### ❌ Don't Use `latest` Tag in Production

```xml
<!-- Bad: Unpredictable -->
<DockerImageTag>latest</DockerImageTag>

<!-- Good: Explicit version -->
<DockerImageTag>$(Version)</DockerImageTag>
```

### ❌ Don't Hardcode Environment-Specific Values

```xml
<!-- Bad: Hardcoded -->
<DockerRegistry>prod-registry.azurecr.io</DockerRegistry>

<!-- Good: Configurable -->
<DockerRegistry>$(DOCKER_REGISTRY)</DockerRegistry>
```

### ❌ Don't Ignore .dockerignore

Always maintain a `.dockerignore` file to exclude unnecessary files.

### ❌ Don't Run as Root

```xml
<!-- Bad: Security risk -->
<DockerCreateUser>false</DockerCreateUser>

<!-- Good: Non-root user -->
<DockerCreateUser>true</DockerCreateUser>
```

## Next Steps

- [Samples](../samples/sample-overview.md) - See best practices in action
- [API Reference](../api/index.md) - Explore all configuration options
- [Workflows](workflows.md) - Implement these practices in your workflows
