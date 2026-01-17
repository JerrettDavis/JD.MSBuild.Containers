# JD.MSBuild.Containers Samples

This directory contains **interactive, end-to-end samples** demonstrating how to use **JD.MSBuild.Containers** to containerize real .NET applications.

## 📦 Available Samples

### 1. **MinimalApi** - Minimal ASP.NET Core Web API
A lightweight Web API demonstrating:
- Minimal API endpoints
- Health checks
- JSON responses
- Automatic containerization

**Endpoints:**
- `GET /` - Service status
- `GET /health` - Health check
- `GET /weatherforecast` - Sample data

### 2. **WebApp** - ASP.NET Core Web Application
A Razor Pages web application demonstrating:
- Web UI containerization
- Static assets handling
- Health monitoring

### 3. **Worker** - Background Worker Service
A long-running background service demonstrating:
- Worker service containerization
- Scheduled background tasks
- Hosted service patterns

### 4. **ConsoleApp** - Console Application
A console application demonstrating:
- Command-line tool containerization
- Non-web workload patterns
- Batch processing scenarios

## 🚀 Quick Start

### Prerequisites

- .NET 10.0 SDK (or later)
- Docker Desktop or compatible runtime
- PowerShell 7+ (for Windows) or Bash (for Linux/macOS)

### Build Everything

**Windows (PowerShell):**
```powershell
.\build.ps1 -Target All
```

**Linux/macOS (Bash):**
```bash
./build.sh all
```

This will:
1. ✅ Build JD.MSBuild.Containers library
2. ✅ Run tests
3. ✅ Pack to local NuGet feed
4. ✅ Restore all samples
5. ✅ Build all samples
6. ✅ Generate Dockerfiles
7. ✅ Build Docker images

### Build Individual Components

**Just build the library:**
```bash
./build.sh build
```

**Just build samples:**
```bash
./build.sh build-samples
```

**Just containerize samples:**
```bash
./build.sh containerize
```

## 📖 How It Works

### Local NuGet Feed

All samples consume **JD.MSBuild.Containers** from a local NuGet feed (`./local-nuget`) created by the build script. This ensures:

- ✅ **Reproducible builds** - No external dependencies
- ✅ **Real-world validation** - Samples use the actual package
- ✅ **CI/CD ready** - Same workflow locally and in CI

### Automatic Dockerization

Each sample includes:

```xml
<PackageReference Include="JD.MSBuild.Containers" Version="*" />
```

With Docker properties:

```xml
<DockerEnabled>true</DockerEnabled>
<DockerImageName>my-sample</DockerImageName>
<DockerImageTag>latest</DockerImageTag>
```

When you run `dotnet publish`, the library automatically:
1. Generates an optimized multi-stage Dockerfile
2. Builds the Docker image
3. Tags it appropriately

**No manual Docker steps required!**

## 🧪 Running Samples

### MinimalApi

```bash
# Run without Docker
cd samples/MinimalApi
dotnet run

# Run with Docker
docker run -p 8080:8080 minimal-api-sample:latest

# Test
curl http://localhost:8080/health
```

### WebApp

```bash
# Run without Docker
cd samples/WebApp
dotnet run

# Run with Docker
docker run -p 8080:8080 webapp-sample:latest

# Test
curl http://localhost:8080/health
```

### Worker

```bash
# Run without Docker
cd samples/Worker
dotnet run

# Run with Docker
docker run worker-sample:latest

# View logs
docker logs <container-id>
```

### ConsoleApp

```bash
# Run without Docker
cd samples/ConsoleApp
dotnet run

# Run with Docker
docker run console-app-sample:latest
```

## 🔧 Configuration

Each sample can be customized via MSBuild properties. See individual sample READMEs for details.

### Common Properties

```xml
<!-- Enable Docker integration -->
<DockerEnabled>true</DockerEnabled>

<!-- Control what happens -->
<DockerGenerateDockerfile>true</DockerGenerateDockerfile>
<DockerBuildImage>true</DockerBuildImage>

<!-- Control when it happens -->
<DockerBuildOnPublish>true</DockerBuildOnPublish>

<!-- Configure the image -->
<DockerImageName>my-app</DockerImageName>
<DockerImageTag>1.0.0</DockerImageTag>
<DockerRegistry>myregistry.azurecr.io</DockerRegistry>
```

## 📚 Documentation

For complete documentation, see:

- **[User Guides](../docs/guides/)** - Getting started and concepts
- **[API Reference](../docs/api/)** - MSBuild properties and tasks
- **[Sample Docs](../docs/samples/)** - Detailed sample walkthroughs

## 🧑‍💻 Development Workflow

### Typical Workflow

1. **Make changes** to the library
2. **Run build script** to pack and test
3. **Samples automatically use** the updated package
4. **Validate** samples still work

### Testing Changes

```bash
# Quick validation
./build.sh pack
./build.sh build-samples

# Full validation
./build.sh all
```

## 🐛 Troubleshooting

### "Package not found" error

Ensure the local NuGet feed is populated:

```bash
./build.sh pack
```

### Docker build fails

Verify Docker is running:

```bash
docker --version
docker ps
```

### Sample won't restore

Clear NuGet cache and rebuild:

```bash
dotnet nuget locals all --clear
./build.sh clean
./build.sh all
```

## 🎯 Integration Tests

Integration tests for these samples are located in `/tests/JD.MSBuild.Containers.IntegrationTests`.

Tests validate:
- ✅ Samples build successfully
- ✅ Docker images are created
- ✅ Containers start and run
- ✅ Services respond correctly

Run integration tests:

```bash
dotnet test tests/JD.MSBuild.Containers.IntegrationTests
```

## 📝 License

All samples are licensed under the MIT License. See [LICENSE](../LICENSE) for details.
