# API Reference

Welcome to the JD.MSBuild.Containers API reference documentation. This section provides comprehensive information about all MSBuild properties, tasks, and targets available in the library.

## MSBuild Properties {#properties}

JD.MSBuild.Containers is configured entirely through MSBuild properties in your `.csproj` file. Properties are organized into logical categories.

### Core Enablement Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockerEnabled` | bool | `false` | Master switch to enable Docker integration |
| `DockerGenerateDockerfile` | bool | `true` | Generate Dockerfile during build |
| `DockerBuildImage` | bool | `false` | Build Docker image |
| `DockerRunContainer` | bool | `false` | Run container after build |
| `DockerPushImage` | bool | `false` | Push image to registry |

### Timing Properties

Control when Docker operations occur:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockerGenerateOnBuild` | bool | `true` | Generate Dockerfile during Build target |
| `DockerBuildOnBuild` | bool | `false` | Build image during Build target |
| `DockerBuildOnPublish` | bool | `true` | Build image during Publish target |
| `DockerRunOnBuild` | bool | `false` | Run container after Build |
| `DockerPushOnPublish` | bool | `true` | Push image after Publish |

### Image Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockerImageName` | string | `$(AssemblyName).ToLower()` | Docker image name |
| `DockerImageTag` | string | `latest` | Docker image tag |
| `DockerRegistry` | string | *(empty)* | Container registry URL (e.g., `myregistry.azurecr.io`) |
| `DockerBaseImageRuntime` | string | `mcr.microsoft.com/dotnet/aspnet` | Runtime base image |
| `DockerBaseImageSdk` | string | `mcr.microsoft.com/dotnet/sdk` | SDK base image for build |
| `DockerBaseImageVersion` | string | Auto-detected | Base image version (e.g., `8.0`) |

### Build Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockerBuildContext` | string | `$(MSBuildProjectDirectory)` | Docker build context path |
| `DockerBuildArgs` | string | *(empty)* | Additional build arguments |
| `DockerBuildPlatform` | string | *(empty)* | Target platform (e.g., `linux/amd64`) |
| `DockerBuildTarget` | string | *(empty)* | Target stage in multi-stage build |
| `DockerUseMultiStage` | bool | `true` | Use multi-stage Dockerfile |
| `DockerOptimizeLayers` | bool | `true` | Optimize Docker layers for caching |

### File Path Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockerfileOutput` | string | `$(MSBuildProjectDirectory)\Dockerfile` | Generated Dockerfile path |
| `DockerfileSource` | string | `$(MSBuildProjectDirectory)\Dockerfile` | Existing Dockerfile path (when not generating) |
| `DockerOutput` | string | `$(BaseIntermediateOutputPath)docker\` | Docker build output directory |
| `DockerTemplateFile` | string | *(empty)* | Custom Dockerfile template path |

### Script Properties

Execute custom scripts at various stages:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockerPreBuildScript` | string | *(empty)* | Script to run before Docker build |
| `DockerPostBuildScript` | string | *(empty)* | Script to run after Docker build |
| `DockerPrePublishScript` | string | *(empty)* | Script to run before Publish |
| `DockerPostPublishScript` | string | *(empty)* | Script to run after Publish |
| `DockerExecutePreBuildScript` | bool | `true` (if script set) | Enable pre-build script |
| `DockerExecutePostBuildScript` | bool | `true` (if script set) | Enable post-build script |
| `DockerExecutePrePublishScript` | bool | `true` (if script set) | Enable pre-publish script |
| `DockerExecutePostPublishScript` | bool | `true` (if script set) | Enable post-publish script |

### Container Runtime Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockerPortMappings` | string | *(empty)* | Port mappings (e.g., `8080:8080;9090:9090`) |
| `DockerEnvironmentVariables` | string | *(empty)* | Environment variables (e.g., `VAR1=value1;VAR2=value2`) |
| `DockerVolumeMappings` | string | *(empty)* | Volume mounts (e.g., `./data:/app/data`) |
| `DockerNetworkMode` | string | *(empty)* | Docker network mode (e.g., `bridge`, `host`) |

### Advanced Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockerUseFingerprinting` | bool | `true` | Enable incremental builds via fingerprinting |
| `DockerLogVerbosity` | string | `minimal` | Logging level: `quiet`, `minimal`, `normal`, `detailed`, `diagnostic` |
| `DockerCommand` | string | `docker` | Docker CLI command path |
| `DockerProjectType` | string | Auto-detected | Project type: `web`, `worker`, `console`, `library` |
| `DockerExposePort` | int | `8080` (for web) | Port to expose in container |
| `DockerUser` | string | `app` | Container user name |
| `DockerCreateUser` | bool | `true` | Create non-root user in container |

## MSBuild Tasks {#tasks}

JD.MSBuild.Containers provides several MSBuild tasks that execute Docker operations.

### ResolveDockerInputs

Analyzes the project and determines Docker configuration.

**Input Properties:**
- Project file path
- Target framework
- Output type
- Package references

**Output Properties:**
- Resolved base images
- Detected project type
- Port configuration
- Entrypoint configuration

### GenerateDockerfile

Generates an optimized Dockerfile based on project analysis.

**Input:**
- Resolved Docker inputs
- Template file (optional)
- Configuration properties

**Output:**
- Dockerfile written to `DockerfileOutput`
- Fingerprint hash

### ComputeFingerprint

Calculates a hash of project files for incremental build support.

**Input:**
- Project files
- Dependency assemblies
- Docker properties

**Output:**
- SHA256 hash
- Stored in `obj/docker/fingerprint.txt`

### ExecuteDockerBuild

Invokes Docker CLI to build the image.

**Input:**
- Dockerfile path
- Image name and tag
- Build context
- Build arguments

**Output:**
- Built Docker image
- Build logs

### ExecuteDockerRun

Starts a container from the built image.

**Input:**
- Image name
- Port mappings
- Environment variables
- Volume mappings

**Output:**
- Running container
- Container ID

### ExecuteDockerPush

Pushes the image to a container registry.

**Input:**
- Image name with registry
- Credentials (from Docker CLI)

**Output:**
- Published image in registry

### ExecuteDockerScript

Executes custom scripts at various lifecycle stages.

**Input:**
- Script path
- Working directory
- Environment variables

**Output:**
- Script exit code
- Script output

## MSBuild Targets

JD.MSBuild.Containers defines several targets that integrate into the build pipeline.

### Main Targets

- `DockerResolveInputs` - Analyze project and resolve configuration
- `DockerGenerateDockerfile` - Generate Dockerfile
- `DockerBuild` - Build Docker image
- `DockerRun` - Run container
- `DockerPush` - Push to registry
- `DockerPublish` - Combined publish workflow
- `DockerClean` - Clean generated Docker files

### Extensibility Targets

Hook into these targets to customize behavior:

- `BeforeDockerGeneration` - Runs before Dockerfile generation
- `AfterDockerGeneration` - Runs after Dockerfile generation
- `BeforeDockerBuild` - Runs before Docker build
- `AfterDockerBuild` - Runs after Docker build
- `BeforeDockerRun` - Runs before container start
- `AfterDockerRun` - Runs after container start

### Example: Custom Target

```xml
<Target Name="MyCustomSetup" BeforeTargets="DockerBuild">
  <Message Text="Running custom setup before Docker build" Importance="high" />
  <Exec Command="./scripts/prepare.sh" />
</Target>
```

## Property Evaluation Order

MSBuild evaluates properties in this order (later overrides earlier):

1. **JD.MSBuild.Containers.props** - Default values from the package
2. **Directory.Build.props** - Solution-wide defaults
3. **Your .csproj** - Project-specific settings
4. **Command line** - Highest priority: `/p:PropertyName=Value`

Example:

```bash
# Override property from command line
dotnet build /p:DockerImageTag=custom-tag
```

## Common Configuration Patterns

### Generate-Only Mode

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <!-- DockerGenerateDockerfile defaults to true -->
  <!-- DockerBuildImage defaults to false -->
</PropertyGroup>
```

### Full Automation Mode

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
</PropertyGroup>
```

### Build-Only Mode

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>false</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerfileSource>$(MSBuildProjectDirectory)/custom.Dockerfile</DockerfileSource>
</PropertyGroup>
```

### Configuration-Specific Settings

```xml
<!-- Debug: Generate only -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DockerEnabled>true</DockerEnabled>
  <DockerBuildImage>false</DockerBuildImage>
</PropertyGroup>

<!-- Release: Full automation -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DockerEnabled>true</DockerEnabled>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  <DockerPushImage>true</DockerPushImage>
</PropertyGroup>
```

## Generated API Documentation

For detailed API documentation of the MSBuild tasks and their implementation, see the auto-generated API reference from XML documentation comments.

> **Note**: API documentation is automatically generated by DocFX from XML documentation comments in the source code.

## Next Steps

- [Getting Started](../articles/getting-started.md) - Quick start guide
- [Concepts](../articles/concepts.md) - Understand how it works
- [Best Practices](../articles/best-practices.md) - Recommended patterns
- [Samples](../samples/sample-overview.md) - Working examples
