# JD.MSBuild.Containers

**MSBuild tasks, targets, and props to integrate OCI containers with your .NET projects**

[![NuGet](https://img.shields.io/nuget/v/JD.MSBuild.Containers.svg)](https://www.nuget.org/packages/JD.MSBuild.Containers/)
[![License](https://img.shields.io/github/license/jerrettdavis/JD.MSBuild.Containers.svg)](LICENSE)
[![CI](https://github.com/JerrettDavis/JD.MSBuild.Containers/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/JD.MSBuild.Containers/actions/workflows/ci.yml)
[![CodeQL](https://github.com/JerrettDavis/JD.MSBuild.Containers/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/JerrettDavis/JD.MSBuild.Containers/security/code-scanning)
[![codecov](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Containers/branch/main/graph/badge.svg)](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Containers)
[![Documentation](https://img.shields.io/badge/docs-online-blue)](https://jerrettdavis.github.io/JD.MSBuild.Containers/)

Automate Docker containerization during `dotnet build`. Zero manual steps, full CI/CD support, reproducible container builds with granular control over every step of the process.

## 📚 Documentation

**[View Complete Documentation](https://jerrettdavis.github.io/JD.MSBuild.Containers/)**

- [Introduction](https://jerrettdavis.github.io/JD.MSBuild.Containers/articles/introduction.html) - Project overview and architecture
- [Getting Started](https://jerrettdavis.github.io/JD.MSBuild.Containers/articles/getting-started.html) - Installation and quick start guide
- [Tutorials](https://jerrettdavis.github.io/JD.MSBuild.Containers/tutorials/tutorial-basic.html) - Step-by-step walkthroughs
- [API Reference](https://jerrettdavis.github.io/JD.MSBuild.Containers/api/) - Complete property and task reference
- [Samples](https://jerrettdavis.github.io/JD.MSBuild.Containers/samples/sample-overview.html) - Working example projects

## Quick Start

### Installation

```bash
dotnet add package JD.MSBuild.Containers
```

### Basic Usage - Generate Dockerfile Only

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Enable Docker integration -->
    <DockerEnabled>true</DockerEnabled>
    
    <!-- Generate Dockerfile (default when DockerEnabled=true) -->
    <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Containers" Version="*" />
  </ItemGroup>
</Project>
```

Run `dotnet build` and a Dockerfile will be generated in your project directory.

### Full Automation - Generate and Build

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
</PropertyGroup>
```

Run `dotnet publish` to generate the Dockerfile and build the Docker image.

### Build-Only Mode - Use Existing Dockerfile

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  
  <!-- Don't generate, use existing Dockerfile -->
  <DockerGenerateDockerfile>false</DockerGenerateDockerfile>
  
  <!-- Build the image -->
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  
  <!-- Optional: specify custom Dockerfile location -->
  <DockerfileSource>$(MSBuildProjectDirectory)\custom.Dockerfile</DockerfileSource>
</PropertyGroup>
```

## Key Features

### Granular Control

JD.MSBuild.Containers is designed as a **configurable OCI MSBuild shim integration**. Every feature can be independently enabled or disabled:

- ✅ **Generate Dockerfile** - Auto-generate optimized multi-stage Dockerfiles
- ✅ **Build Images** - Execute `docker build` during MSBuild
- ✅ **Run Containers** - Start containers after build (opt-in)
- ✅ **Push to Registry** - Automatically push to container registries
- ✅ **Pre/Post Scripts** - Execute custom scripts at any stage
- ✅ **Incremental Builds** - Skip regeneration when nothing changes
- ✅ **MSBuild Hooks** - Integrate with Build, Clean, Publish, Run targets

### Configuration Modes

| Mode | Generate | Build | Use Case |
|------|----------|-------|----------|
| **Generate-Only** | ✅ | ❌ | Review/commit Dockerfiles, manual builds |
| **Build-Only** | ❌ | ✅ | Use existing Dockerfiles, custom templates |
| **Full Automation** | ✅ | ✅ | Complete CI/CD, zero manual steps |
| **Custom Hooks** | ➖ | ➖ | Execute scripts only, manual everything else |

## Configuration Reference

### Core Enablement

| Property | Default | Description |
|----------|---------|-------------|
| `DockerEnabled` | `false` | Master switch for Docker integration |
| `DockerGenerateDockerfile` | `true` (when enabled) | Controls Dockerfile generation |
| `DockerBuildImage` | `false` | Controls Docker image building |
| `DockerRunContainer` | `false` | Controls container execution |
| `DockerPushImage` | `false` | Controls pushing to registry |

### Hook Integration

| Property | Default | Description |
|----------|---------|-------------|
| `DockerGenerateOnBuild` | `true` | Generate Dockerfile during Build |
| `DockerBuildOnBuild` | `false` | Build image during Build |
| `DockerBuildOnPublish` | `true` | Build image during Publish |
| `DockerRunOnBuild` | `false` | Run container after Build |
| `DockerPushOnPublish` | `true` | Push image after Publish |

### Script Execution

| Property | Default | Description |
|----------|---------|-------------|
| `DockerPreBuildScript` | - | Path to pre-build script |
| `DockerPostBuildScript` | - | Path to post-build script |
| `DockerPrePublishScript` | - | Path to pre-publish script |
| `DockerPostPublishScript` | - | Path to post-publish script |
| `DockerExecutePreBuildScript` | `true` (if script set) | Enable pre-build script |
| `DockerExecutePostBuildScript` | `true` (if script set) | Enable post-build script |
| `DockerExecutePrePublishScript` | `true` (if script set) | Enable pre-publish script |
| `DockerExecutePostPublishScript` | `true` (if script set) | Enable post-publish script |

### Image Configuration

| Property | Default | Description |
|----------|---------|-------------|
| `DockerImageName` | `$(AssemblyName).ToLower()` | Docker image name |
| `DockerImageTag` | `latest` | Docker image tag |
| `DockerRegistry` | - | Container registry URL |
| `DockerBaseImageRuntime` | `mcr.microsoft.com/dotnet/aspnet` | Runtime base image |
| `DockerBaseImageSdk` | `mcr.microsoft.com/dotnet/sdk` | SDK base image |
| `DockerBaseImageVersion` | Auto-detected | Base image version |

### Build Options

| Property | Default | Description |
|----------|---------|-------------|
| `DockerBuildContext` | `$(MSBuildProjectDirectory)` | Docker build context |
| `DockerBuildArgs` | - | Additional build arguments |
| `DockerBuildPlatform` | - | Target platform (e.g., `linux/amd64`) |
| `DockerBuildTarget` | - | Target stage in multi-stage builds |
| `DockerUseMultiStage` | `true` | Use multi-stage Dockerfiles |
| `DockerOptimizeLayers` | `true` | Optimize Docker layers |

### File Paths

| Property | Default | Description |
|----------|---------|-------------|
| `DockerfileOutput` | `$(MSBuildProjectDirectory)\Dockerfile` | Generated Dockerfile path |
| `DockerfileSource` | `$(MSBuildProjectDirectory)\Dockerfile` | Existing Dockerfile path |
| `DockerOutput` | `$(BaseIntermediateOutputPath)docker\` | Build output directory |
| `DockerTemplateFile` | - | Custom Dockerfile template |

### Advanced Options

| Property | Default | Description |
|----------|---------|-------------|
| `DockerUseFingerprinting` | `true` | Enable incremental builds |
| `DockerLogVerbosity` | `minimal` | Logging level (quiet/minimal/normal/detailed/diagnostic) |
| `DockerCommand` | `docker` | Docker CLI command |
| `DockerProjectType` | Auto-detected | Project type (console/library) |
| `DockerExposePort` | `8080` (ASP.NET) | Exposed port |
| `DockerUser` | `app` | Container user |
| `DockerCreateUser` | `true` | Create non-root user |

## Usage Examples

### Example 1: Generate-Only Mode (Default)

Perfect for committing Dockerfiles to version control and reviewing before build:

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <!-- DockerGenerateDockerfile defaults to true -->
  <!-- DockerBuildImage defaults to false -->
</PropertyGroup>
```

```bash
dotnet build
# Dockerfile generated at ./Dockerfile
# No Docker image built
```

### Example 2: Build-Only Mode

Use your own hand-crafted Dockerfile:

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>false</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  
  <!-- Optional: custom Dockerfile location -->
  <DockerfileSource>$(MSBuildProjectDirectory)\deploy\Dockerfile</DockerfileSource>
</PropertyGroup>
```

```bash
dotnet publish
# Uses existing Dockerfile at ./deploy/Dockerfile
# Builds Docker image
```

### Example 3: Full Automation

Generate and build automatically:

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  
  <DockerImageName>myapp</DockerImageName>
  <DockerImageTag>$(Version)</DockerImageTag>
  <DockerRegistry>myregistry.azurecr.io</DockerRegistry>
</PropertyGroup>
```

```bash
dotnet publish
# Generates Dockerfile
# Builds image: myregistry.azurecr.io/myapp:1.0.0
```

### Example 4: CI/CD with Scripts and Push

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnPublish>true</DockerBuildOnPublish>
  <DockerPushImage>true</DockerPushImage>
  <DockerPushOnPublish>true</DockerPushOnPublish>
  
  <!-- Pre/Post scripts -->
  <DockerPreBuildScript>$(MSBuildProjectDirectory)\scripts\pre-build.sh</DockerPreBuildScript>
  <DockerPostPublishScript>$(MSBuildProjectDirectory)\scripts\deploy.ps1</DockerPostPublishScript>
  
  <DockerRegistry>myregistry.azurecr.io</DockerRegistry>
  <DockerImageName>myapp</DockerImageName>
  <DockerImageTag>$(GitVersion_SemVer)</DockerImageTag>
</PropertyGroup>
```

### Example 5: Development Workflow with Auto-Run

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  <DockerBuildImage>true</DockerBuildImage>
  <DockerBuildOnBuild>true</DockerBuildOnBuild>
  <DockerRunContainer>true</DockerRunContainer>
  <DockerRunOnBuild>true</DockerRunOnBuild>
  
  <!-- Container configuration -->
  <DockerPortMappings>8080:8080</DockerPortMappings>
  <DockerEnvironmentVariables>ASPNETCORE_ENVIRONMENT=Development</DockerEnvironmentVariables>
  <DockerVolumeMappings>$(MSBuildProjectDirectory)/data:/app/data</DockerVolumeMappings>
</PropertyGroup>
```

```bash
dotnet build
# Generates Dockerfile
# Builds image
# Starts container with port mappings
```

### Example 6: Selective Script Execution

Execute only specific scripts:

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
  
  <!-- Define scripts -->
  <DockerPreBuildScript>$(MSBuildProjectDirectory)\scripts\setup.sh</DockerPreBuildScript>
  <DockerPostBuildScript>$(MSBuildProjectDirectory)\scripts\cleanup.sh</DockerPostBuildScript>
  
  <!-- Enable only pre-build script -->
  <DockerExecutePreBuildScript>true</DockerExecutePreBuildScript>
  <DockerExecutePostBuildScript>false</DockerExecutePostBuildScript>
</PropertyGroup>
```

## Lifecycle Hooks

JD.MSBuild.Containers provides extensibility points at every stage:

```
Build:
  ├─ BeforeDockerGeneration
  ├─ DockerResolveInputs
  ├─ DockerComputeFingerprint
  ├─ DockerGenerateDockerfile
  ├─ AfterDockerGeneration
  ├─ DockerExecutePreBuildScript
  ├─ BeforeDockerBuild
  ├─ DockerBuild
  ├─ AfterDockerBuild
  ├─ DockerExecutePostBuildScript
  ├─ BeforeDockerRun
  ├─ DockerRun
  └─ AfterDockerRun

Publish:
  ├─ DockerExecutePrePublishScript
  ├─ DockerPublish (generates + builds)
  ├─ DockerExecutePostPublishScript
  └─ DockerPushImage (if enabled)

Clean:
  └─ DockerClean (removes generated files)
```

You can define custom targets that depend on or extend these hooks:

```xml
<Target Name="MyCustomPreBuild" BeforeTargets="DockerBuild">
  <Message Text="Running custom logic before Docker build" Importance="high" />
</Target>

<Target Name="MyCustomPostBuild" AfterTargets="AfterDockerBuild">
  <Message Text="Running custom logic after Docker build" Importance="high" />
</Target>
```

## Requirements

- **.NET SDK 8.0+**
- **Docker** (when building images)

## Comparison with Alternatives

| Feature | JD.MSBuild.Containers | Built-in SDK | Docker Desktop |
|---------|----------------------|--------------|----------------|
| Auto-generate Dockerfiles | ✅ | ❌ | ❌ |
| Granular control | ✅ | ❌ | ❌ |
| Build-only mode | ✅ | ❌ | N/A |
| Generate-only mode | ✅ | N/A | N/A |
| Pre/Post scripts | ✅ | ❌ | ❌ |
| MSBuild integration | ✅ | Limited | ❌ |
| Incremental builds | ✅ | ❌ | ❌ |
| Custom templates | ✅ | ❌ | N/A |
| Multi-stage support | ✅ | ❌ | ✅ |

## Code Coverage

This project maintains comprehensive code coverage with automated reporting:

- 📊 **Coverage reports** generated on every PR
- 📈 **Historical tracking** via [Codecov](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Containers)
- 💬 **PR comments** with coverage summaries
- 📦 **HTML reports** available as CI artifacts

See [CODE_COVERAGE.md](CODE_COVERAGE.md) for detailed configuration and usage.

## Contributing

Contributions are welcome! Please open an issue first to discuss changes.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- Architecture inspired by [JD.Efcpt.Build](https://github.com/jerrettdavis/JD.Efcpt.Build)
- Docker for container runtime
- Microsoft for .NET SDK and MSBuild

