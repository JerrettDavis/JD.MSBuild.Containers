# Sample Applications Overview

This section provides detailed documentation for the sample applications included with JD.MSBuild.Containers. Each sample demonstrates different aspects of containerization for various .NET application types.

## Available Samples

### [Web API Sample](sample-webapi.md)
**Type**: ASP.NET Core Minimal API  
**Demonstrates**:
- REST API containerization
- Health check endpoints
- JSON API responses
- Port exposure configuration
- Hot reload in containers

**Key Features**:
- WeatherForecast endpoint
- Built-in health checks
- Swagger/OpenAPI documentation
- Automatic port mapping

[View Sample Documentation →](sample-webapi.md)

---

### [Web App Sample](sample-webapp.md)
**Type**: ASP.NET Core Razor Pages  
**Demonstrates**:
- Web UI containerization
- Static asset handling
- Server-side rendering
- Session state in containers

**Key Features**:
- Razor Pages UI
- Static file serving
- Cookie-based authentication
- Responsive design

[View Sample Documentation →](sample-webapp.md)

---

### [Worker Sample](sample-worker.md)
**Type**: .NET Worker Service  
**Demonstrates**:
- Background service containerization
- Long-running tasks
- Graceful shutdown handling
- No exposed ports

**Key Features**:
- Scheduled background tasks
- IHostedService implementation
- Logging and monitoring
- Signal handling

[View Sample Documentation →](sample-worker.md)

---

### [Console App Sample](sample-console.md)
**Type**: .NET Console Application  
**Demonstrates**:
- CLI tool containerization
- Batch processing
- One-time execution
- Command-line arguments

**Key Features**:
- Command-line parsing
- Exit code handling
- Standard input/output
- Container orchestration patterns

[View Sample Documentation →](sample-console.md)

---

## Running All Samples

### Prerequisites

- .NET SDK 8.0 or later
- Docker Desktop
- Git (to clone repository)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/JerrettDavis/JD.MSBuild.Containers.git
cd JD.MSBuild.Containers

# Build everything (uses build script)
./build.sh all

# Or on Windows
.\build.ps1 -Target All
```

This will:
1. ✅ Build the JD.MSBuild.Containers library
2. ✅ Run tests
3. ✅ Pack to local NuGet feed
4. ✅ Restore all samples
5. ✅ Build all samples
6. ✅ Generate Dockerfiles
7. ✅ Build Docker images

### Build Individual Samples

```bash
# Navigate to sample directory
cd samples/MinimalApi

# Build and containerize
dotnet publish --configuration Release

# Run container
docker run -p 8080:8080 minimal-api-sample:latest
```

## Sample Architecture

All samples follow a consistent structure:

```
samples/
├── Directory.Build.props        # Shared Docker configuration
├── README.md                    # Sample overview
├── ConsoleApp/
│   ├── Program.cs
│   ├── ConsoleApp.csproj        # Docker configuration
│   └── Dockerfile               # Auto-generated
├── MinimalApi/
│   ├── Program.cs
│   ├── MinimalApi.csproj        # Docker configuration
│   └── Dockerfile               # Auto-generated
├── WebApp/
│   ├── Program.cs
│   ├── Pages/
│   ├── WebApp.csproj            # Docker configuration
│   └── Dockerfile               # Auto-generated
└── Worker/
    ├── Program.cs
    ├── Worker.cs
    ├── Worker.csproj            # Docker configuration
    └── Dockerfile               # Auto-generated
```

## Common Docker Configuration

All samples share common Docker settings via `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <!-- Enable Docker for all samples -->
    <DockerEnabled>true</DockerEnabled>
    
    <!-- Generate and build on publish -->
    <DockerGenerateDockerfile>true</DockerGenerateDockerfile>
    <DockerBuildImage>true</DockerBuildImage>
    <DockerBuildOnPublish>true</DockerBuildOnPublish>
    
    <!-- Use consistent base images -->
    <DockerBaseImageVersion>8.0</DockerBaseImageVersion>
  </PropertyGroup>
</Project>
```

## Local NuGet Feed

Samples consume JD.MSBuild.Containers from a local NuGet feed:

```xml
<!-- NuGet.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="../../local-nuget" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

This ensures samples always use the locally built version for testing.

## Testing Samples

### Integration Tests

Integration tests validate that samples build and run correctly:

```bash
# Run integration tests
dotnet test tests/JD.MSBuild.Containers.IntegrationTests
```

Tests verify:
- ✅ Dockerfiles are generated
- ✅ Docker images build successfully
- ✅ Containers start and run
- ✅ Services respond correctly
- ✅ Health checks pass

### Manual Testing

Test each sample manually:

```bash
# Build and run
cd samples/MinimalApi
dotnet publish
docker run -p 8080:8080 minimal-api-sample:latest

# Test in another terminal
curl http://localhost:8080/health
curl http://localhost:8080/weatherforecast

# Stop container
docker ps  # Get container ID
docker stop <container-id>
```

## Customizing Samples

Each sample can be customized for learning:

### Change Image Name

```xml
<PropertyGroup>
  <DockerImageName>my-custom-name</DockerImageName>
</PropertyGroup>
```

### Change Port

```xml
<PropertyGroup>
  <DockerExposePort>3000</DockerExposePort>
</PropertyGroup>
```

Then run with: `docker run -p 3000:3000 ...`

### Add Environment Variables

```xml
<PropertyGroup>
  <DockerEnvironmentVariables>MY_VAR=value;ANOTHER_VAR=value2</DockerEnvironmentVariables>
</PropertyGroup>
```

### Custom Base Image

```xml
<PropertyGroup>
  <DockerBaseImageRuntime>mcr.microsoft.com/dotnet/aspnet:8.0-alpine</DockerBaseImageRuntime>
</PropertyGroup>
```

## Troubleshooting

### Sample Won't Build

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Rebuild library and pack
cd ../../
./build.sh pack

# Try sample again
cd samples/MinimalApi
dotnet restore --force
dotnet build
```

### Docker Image Won't Build

```bash
# Check Docker is running
docker ps

# Try building Dockerfile manually
docker build -t test .

# Check for errors in Dockerfile
cat Dockerfile
```

### Container Won't Start

```bash
# Check container logs
docker logs <container-id>

# Try running interactively
docker run -it --rm minimal-api-sample:latest

# Check port conflicts
lsof -i :8080  # Linux/macOS
netstat -ano | findstr :8080  # Windows
```

## Learning Path

We recommend exploring samples in this order:

1. **[MinimalApi](sample-webapi.md)** - Start here for basic containerization
2. **[WebApp](sample-webapp.md)** - Learn about static assets and UI
3. **[Worker](sample-worker.md)** - Understand background services
4. **[ConsoleApp](sample-console.md)** - Master one-time execution patterns

## Next Steps

- Choose a sample and dive into its detailed documentation
- Experiment with customizations
- Try deploying samples to a cloud platform
- Use samples as templates for your own projects

## Contributing

Found an issue with a sample? Want to add a new one?

- [Report an Issue](https://github.com/JerrettDavis/JD.MSBuild.Containers/issues)
- [Contributing Guide](https://github.com/JerrettDavis/JD.MSBuild.Containers/blob/main/CONTRIBUTING.md)
