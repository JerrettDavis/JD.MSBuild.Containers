# Core Concepts

This guide explains the fundamental concepts behind JD.MSBuild.Containers and how it integrates with MSBuild and Docker.

## What is JD.MSBuild.Containers?

JD.MSBuild.Containers is an **MSBuild task library** that extends the .NET build system to support Docker containerization. It provides:

- **MSBuild Tasks** - Custom tasks that execute during build/publish
- **MSBuild Targets** - Predefined build steps that orchestrate containerization
- **MSBuild Properties** - Configuration options for controlling behavior

## Core Concepts

### 1. MSBuild Integration

#### How MSBuild Works

MSBuild is the build engine for .NET. When you run `dotnet build`, MSBuild:

1. Loads your `.csproj` file
2. Imports all referenced `.targets` and `.props` files
3. Executes a series of predefined targets (BeforeBuild, CoreBuild, AfterBuild, etc.)
4. Runs tasks within each target

#### How JD.MSBuild.Containers Integrates

When you add the `JD.MSBuild.Containers` package:

1. **NuGet Import** - MSBuild automatically imports `JD.MSBuild.Containers.targets`
2. **Target Registration** - New targets are registered in the build pipeline
3. **Task Execution** - Custom tasks run at appropriate build stages
4. **Property Evaluation** - Configuration properties control task behavior

```xml
<!-- Your .csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Containers" Version="*" />
    <!-- This causes NuGet to import build/JD.MSBuild.Containers.targets -->
  </ItemGroup>
</Project>
```

### 2. Build Lifecycle

JD.MSBuild.Containers hooks into the MSBuild lifecycle at specific points:

```
MSBuild Lifecycle:
│
├─ BeforeBuild
│  └─ (Your custom targets)
│
├─ CoreBuild
│  ├─ Compile C# code
│  └─ Process resources
│
├─ AfterBuild
│  ├─ BeforeDockerGeneration          ← JD.MSBuild.Containers
│  ├─ DockerResolveInputs             ← JD.MSBuild.Containers
│  ├─ DockerComputeFingerprint        ← JD.MSBuild.Containers
│  ├─ DockerGenerateDockerfile        ← JD.MSBuild.Containers
│  ├─ AfterDockerGeneration           ← JD.MSBuild.Containers
│  ├─ DockerExecutePreBuildScript     ← JD.MSBuild.Containers (optional)
│  ├─ DockerBuild                     ← JD.MSBuild.Containers (if enabled)
│  └─ DockerExecutePostBuildScript    ← JD.MSBuild.Containers (optional)
│
├─ BeforePublish
│  └─ DockerExecutePrePublishScript   ← JD.MSBuild.Containers (optional)
│
├─ CorePublish
│
└─ AfterPublish
   ├─ DockerPublish                   ← JD.MSBuild.Containers
   ├─ DockerPushImage                 ← JD.MSBuild.Containers (if enabled)
   └─ DockerExecutePostPublishScript  ← JD.MSBuild.Containers (optional)
```

### 3. Task Architecture

#### Key MSBuild Tasks

**ResolveDockerInputs**
- **Purpose**: Analyzes project and determines Docker configuration
- **Input**: Project properties, SDK version, target framework
- **Output**: Base images, ports, entrypoint, working directory

**GenerateDockerfile**
- **Purpose**: Creates optimized Dockerfile
- **Input**: Resolved Docker inputs, project files
- **Output**: Dockerfile written to disk

**ComputeFingerprint**
- **Purpose**: Calculates hash of project files for incremental builds
- **Input**: Project files, dependencies
- **Output**: Fingerprint hash stored in obj/ directory

**ExecuteDockerBuild**
- **Purpose**: Invokes Docker CLI to build image
- **Input**: Dockerfile path, image name/tag, build arguments
- **Output**: Docker image

**ExecuteDockerRun**
- **Purpose**: Starts container from built image
- **Input**: Image name, port mappings, environment variables
- **Output**: Running container

**ExecuteDockerPush**
- **Purpose**: Pushes image to container registry
- **Input**: Image name with registry, credentials
- **Output**: Published image

### 4. Property-Based Configuration

All behavior is controlled via MSBuild properties:

#### Property Evaluation Order

1. **Default values** - Set in `JD.MSBuild.Containers.props`
2. **Directory.Build.props** - Project-wide defaults
3. **Project file (.csproj)** - Project-specific settings
4. **Command line** - Highest priority: `/p:PropertyName=Value`

Example:

```xml
<!-- Default in JD.MSBuild.Containers.props -->
<DockerEnabled Condition="'$(DockerEnabled)' == ''">false</DockerEnabled>

<!-- Override in your .csproj -->
<DockerEnabled>true</DockerEnabled>

<!-- Override from command line -->
dotnet build /p:DockerEnabled=false
```

#### Property Categories

**Enablement Properties**
- Control what features are enabled
- Examples: `DockerEnabled`, `DockerGenerateDockerfile`, `DockerBuildImage`

**Configuration Properties**
- Control how features work
- Examples: `DockerImageName`, `DockerRegistry`, `DockerExposePort`

**Timing Properties**
- Control when features execute
- Examples: `DockerBuildOnBuild`, `DockerBuildOnPublish`, `DockerPushOnPublish`

**Path Properties**
- Control file locations
- Examples: `DockerfileOutput`, `DockerBuildContext`, `DockerOutput`

### 5. Incremental Build Support

JD.MSBuild.Containers implements intelligent incremental builds using **fingerprinting**:

#### How Fingerprinting Works

1. **Compute Hash** - Calculate SHA256 hash of:
   - All project source files
   - All dependency assemblies
   - Docker configuration properties
   - `.csproj` file content

2. **Store Fingerprint** - Save hash to `obj/docker/fingerprint.txt`

3. **Compare on Next Build** - On subsequent builds:
   - Compute new hash
   - Compare with stored hash
   - Skip generation if hashes match

#### Benefits

- **Faster Builds** - Skip Dockerfile generation when nothing changed
- **Consistent Builds** - Same inputs always produce same outputs
- **CI/CD Optimization** - Reduce build times in pipelines

#### Controlling Fingerprinting

```xml
<!-- Disable fingerprinting (always regenerate) -->
<DockerUseFingerprinting>false</DockerUseFingerprinting>

<!-- Force regeneration (clears stored fingerprint) -->
dotnet clean
```

### 6. Multi-Stage Dockerfile Generation

Generated Dockerfiles use **multi-stage builds** for optimization:

```dockerfile
# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyApp.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Stage 4: Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

#### Why Multi-Stage?

- **Smaller Images** - Final image only contains runtime, not SDK
- **Better Caching** - Each stage caches independently
- **Security** - Build tools not present in production image
- **Flexibility** - Can target specific stages for debugging

#### Customizing Stages

```xml
<!-- Build only up to 'build' stage -->
<DockerBuildTarget>build</DockerBuildTarget>

<!-- Disable multi-stage (single stage) -->
<DockerUseMultiStage>false</DockerUseMultiStage>
```

### 7. Docker Build Context

The **build context** is the set of files sent to Docker daemon during build:

```
Project Directory (Build Context):
├── MyApp.csproj          ← Included
├── Program.cs            ← Included
├── Dockerfile            ← Included
├── obj/                  ← Excluded (.dockerignore)
├── bin/                  ← Excluded (.dockerignore)
└── .git/                 ← Excluded (.dockerignore)
```

#### Default Build Context

By default, the build context is your project directory (`$(MSBuildProjectDirectory)`).

#### Customizing Build Context

```xml
<!-- Use parent directory as context -->
<DockerBuildContext>$(MSBuildProjectDirectory)\..</DockerBuildContext>

<!-- Use solution directory as context -->
<DockerBuildContext>$(SolutionDir)</DockerBuildContext>
```

#### .dockerignore

JD.MSBuild.Containers respects `.dockerignore` files:

```
# .dockerignore
obj/
bin/
*.user
.vs/
.git/
```

### 8. Project Type Detection

JD.MSBuild.Containers automatically detects your project type:

#### Detection Logic

1. **Check SDK** - Analyze `<Project Sdk="...">` attribute
2. **Check Output Type** - Analyze `<OutputType>` property
3. **Check Dependencies** - Analyze package references

#### Project Types

**Web Application** (SDK: Microsoft.NET.Sdk.Web)
- Base image: `mcr.microsoft.com/dotnet/aspnet`
- Exposes port: `8080`
- Non-root user: `app`

**Worker Service** (SDK: Microsoft.NET.Sdk.Worker)
- Base image: `mcr.microsoft.com/dotnet/runtime`
- No exposed ports
- Non-root user: `app`

**Console Application** (OutputType: Exe)
- Base image: `mcr.microsoft.com/dotnet/runtime`
- No exposed ports
- Entrypoint: `dotnet MyApp.dll`

**Library** (OutputType: Library)
- Not containerizable by default
- Requires manual configuration

#### Overriding Detection

```xml
<!-- Force project type -->
<DockerProjectType>console</DockerProjectType>
<!-- Options: web, worker, console, library -->
```

### 9. Security Best Practices

JD.MSBuild.Containers follows Docker security best practices:

#### Non-Root User

Generated Dockerfiles create and use a non-root user:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN addgroup --system --gid 1001 app && \
    adduser --system --uid 1001 --gid 1001 app
USER app
WORKDIR /app
```

#### Minimal Base Images

Uses official Microsoft minimal base images:
- `mcr.microsoft.com/dotnet/aspnet:8.0` (for web apps)
- `mcr.microsoft.com/dotnet/runtime:8.0` (for console/worker)

#### Layer Optimization

Optimizes Docker layers for better caching:
- Dependencies copied and restored first
- Source code copied after
- Reduces rebuild time when only code changes

### 10. Extensibility Points

Extend JD.MSBuild.Containers through custom targets:

```xml
<!-- Run before Dockerfile generation -->
<Target Name="MyPreDockerGeneration" BeforeTargets="DockerGenerateDockerfile">
  <Message Text="Preparing for Docker generation..." Importance="high" />
</Target>

<!-- Run after Dockerfile generation -->
<Target Name="MyPostDockerGeneration" AfterTargets="DockerGenerateDockerfile">
  <Exec Command="echo Dockerfile generated >> build.log" />
</Target>

<!-- Run before Docker build -->
<Target Name="MyPreDockerBuild" BeforeTargets="DockerBuild">
  <Message Text="About to build Docker image..." Importance="high" />
</Target>
```

## Understanding the Build Flow

### Scenario 1: Generate-Only (Default)

```bash
dotnet build
```

**What Happens:**
1. MSBuild compiles your C# code
2. `DockerResolveInputs` analyzes project
3. `DockerComputeFingerprint` checks if regeneration needed
4. `DockerGenerateDockerfile` creates Dockerfile
5. **No Docker build occurs**

**Result:** Dockerfile created, ready to commit or build manually

### Scenario 2: Full Automation

```bash
dotnet publish
```

**What Happens:**
1. MSBuild compiles your C# code
2. MSBuild publishes output
3. `DockerResolveInputs` analyzes project
4. `DockerGenerateDockerfile` creates Dockerfile
5. `DockerExecutePrePublishScript` runs (if configured)
6. `DockerBuild` executes `docker build`
7. `DockerPushImage` pushes to registry (if configured)
8. `DockerExecutePostPublishScript` runs (if configured)

**Result:** Docker image built and optionally pushed

## Next Steps

- [Workflows](workflows.md) - See how to use these concepts in practice
- [Best Practices](best-practices.md) - Learn recommended patterns
- [API Reference](../api/index.md) - Explore all properties and tasks
