# Introduction to JD.MSBuild.Containers

## Overview

**JD.MSBuild.Containers** is a comprehensive MSBuild integration library that brings Docker containerization directly into your .NET build pipeline. By integrating with MSBuild, it eliminates the need for manual Dockerfile creation and Docker CLI commands, enabling a seamless "code-to-container" workflow.

## Vision and Scope

The vision of JD.MSBuild.Containers is to make containerization as natural as building your .NET application. Instead of maintaining separate Docker configurations and build scripts, everything is managed through familiar MSBuild properties in your `.csproj` files.

### Design Philosophy

1. **Zero Manual Steps** - Containerization should happen automatically during normal build workflows
2. **Granular Control** - Every feature can be independently enabled or disabled
3. **MSBuild Native** - Works seamlessly with `dotnet build`, `dotnet publish`, and MSBuild
4. **CI/CD Ready** - Designed for continuous integration and deployment pipelines
5. **Reproducible Builds** - Fingerprinting ensures consistent builds across environments

## When to Use JD.MSBuild.Containers

### Ideal Use Cases

JD.MSBuild.Containers is perfect for:

- **Modern .NET Applications** - ASP.NET Core web apps, APIs, worker services, and console applications
- **CI/CD Pipelines** - GitHub Actions, Azure DevOps, GitLab CI, and other automation systems
- **Microservices** - Multiple containerized services that need consistent build processes
- **Development Teams** - Teams that want standardized containerization without Docker expertise
- **Iterative Development** - Fast inner-loop development with automatic container rebuilds

### When NOT to Use

Consider alternatives if:

- **Complex Docker Requirements** - Highly customized Dockerfiles with advanced multi-stage builds
- **Non-.NET Workloads** - This library is designed specifically for .NET applications
- **Docker Compose Orchestration** - If you need complex multi-container orchestration (though you can still use it for individual services)
- **Legacy .NET Framework** - Only .NET Core/.NET 5+ applications are supported

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Your .NET Project (.csproj)               │
│  <DockerEnabled>true</DockerEnabled>                        │
│  <DockerImageName>myapp</DockerImageName>                   │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              JD.MSBuild.Containers.targets                  │
│  (Imported automatically via NuGet)                         │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    MSBuild Tasks                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Resolve    │→ │   Generate   │→ │    Build     │      │
│  │    Inputs    │  │  Dockerfile  │  │    Image     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Docker CLI                               │
│  docker build -t myapp:latest .                             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  Container Image                            │
│  myapp:latest                                               │
└─────────────────────────────────────────────────────────────┘
```

### Component Breakdown

#### 1. MSBuild Integration Layer

The library integrates into your project through NuGet package references. When you add `JD.MSBuild.Containers` to your project, MSBuild targets and props files are automatically imported.

#### 2. Input Resolution

The `ResolveDockerInputs` task analyzes your project:
- Detects project type (Web, Worker, Console)
- Determines appropriate base images
- Resolves .NET SDK and runtime versions
- Collects project dependencies

#### 3. Dockerfile Generation

The `GenerateDockerfile` task creates optimized Dockerfiles:
- Multi-stage builds (SDK + Runtime)
- Layer optimization for caching
- Security best practices (non-root user)
- Correct working directories and entrypoints

#### 4. Docker Build Execution

The `ExecuteDockerBuild` task invokes Docker CLI:
- Executes `docker build` with appropriate arguments
- Handles platform-specific builds
- Manages build arguments and tags
- Reports build progress and errors

#### 5. Optional Features

Additional tasks handle optional features:
- `ExecuteDockerRun` - Start containers after build
- `ExecuteDockerPush` - Push images to registries
- `ExecuteDockerScript` - Run custom pre/post scripts

## Configuration Modes

JD.MSBuild.Containers supports multiple operational modes:

### Generate-Only Mode (Default)

```xml
<DockerEnabled>true</DockerEnabled>
<!-- DockerGenerateDockerfile defaults to true -->
<!-- DockerBuildImage defaults to false -->
```

**Behavior**: Generates Dockerfile during `dotnet build`, but doesn't build the image.

**Use Case**: Review and commit Dockerfiles to source control, build images manually or in separate CI steps.

### Build-Only Mode

```xml
<DockerEnabled>true</DockerEnabled>
<DockerGenerateDockerfile>false</DockerGenerateDockerfile>
<DockerBuildImage>true</DockerBuildImage>
```

**Behavior**: Uses existing Dockerfile, builds Docker image during `dotnet publish`.

**Use Case**: Maintain hand-crafted Dockerfiles, but automate the build process.

### Full Automation Mode

```xml
<DockerEnabled>true</DockerEnabled>
<DockerGenerateDockerfile>true</DockerGenerateDockerfile>
<DockerBuildImage>true</DockerBuildImage>
<DockerBuildOnPublish>true</DockerBuildOnPublish>
```

**Behavior**: Generates Dockerfile and builds image during `dotnet publish`.

**Use Case**: Complete automation for CI/CD pipelines.

## Integration Points

JD.MSBuild.Containers integrates with MSBuild through several extension points:

### Build Target Hooks

- `BeforeDockerGeneration` - Execute custom logic before Dockerfile generation
- `AfterDockerGeneration` - Execute custom logic after Dockerfile generation
- `BeforeDockerBuild` - Execute custom logic before Docker build
- `AfterDockerBuild` - Execute custom logic after Docker build

### Publish Target Hooks

- `BeforeDockerPublish` - Execute custom logic before publish
- `AfterDockerPublish` - Execute custom logic after publish

### Clean Target

- `DockerClean` - Removes generated Docker files during `dotnet clean`

## Key Benefits

### For Developers

- **Less Context Switching** - Stay in C# and MSBuild, no Docker expertise required
- **Faster Inner Loop** - Automatic rebuilds during development
- **Consistent Environments** - Same container configuration across team

### For DevOps Engineers

- **Standardized Builds** - All projects follow the same containerization pattern
- **Easy CI/CD Integration** - Works with existing MSBuild-based pipelines
- **Reproducible Artifacts** - Fingerprinting ensures deterministic builds

### For Teams

- **Lower Barrier to Entry** - New team members don't need Docker expertise
- **Better Maintainability** - Container configuration lives with code
- **Faster Onboarding** - Consistent tooling across all projects

## Comparison with Alternatives

### vs. Manual Dockerfiles

| Aspect | JD.MSBuild.Containers | Manual Dockerfiles |
|--------|----------------------|-------------------|
| Setup Time | Minutes | Hours |
| Maintenance | Automatic updates | Manual updates |
| Consistency | Enforced by library | Manual enforcement |
| Learning Curve | Minimal | Steep |
| Flexibility | High (via templates) | Maximum |

### vs. Built-in SDK Support

| Aspect | JD.MSBuild.Containers | .NET SDK (limited) |
|--------|----------------------|-------------------|
| Dockerfile Generation | ✅ Full control | ❌ Not supported |
| Custom Base Images | ✅ Yes | Limited |
| Pre/Post Scripts | ✅ Yes | ❌ No |
| Incremental Builds | ✅ Yes | ❌ No |
| Multi-mode Support | ✅ Yes | Limited |

### vs. Docker Desktop Extensions

| Aspect | JD.MSBuild.Containers | Docker Desktop |
|--------|----------------------|---------------|
| MSBuild Integration | ✅ Native | ❌ No |
| CLI Workflow | ✅ Yes | ❌ GUI only |
| CI/CD Friendly | ✅ Yes | ❌ No |
| Automation | ✅ Full | Manual |

## What's Next?

Now that you understand what JD.MSBuild.Containers is and how it works, continue to:

- [Getting Started](getting-started.md) - Install and configure your first project
- [Core Concepts](concepts.md) - Deep dive into how it works
- [Tutorials](../tutorials/tutorial-basic.md) - Step-by-step walkthroughs
