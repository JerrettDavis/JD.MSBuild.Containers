# Web API Sample (MinimalApi)

This sample demonstrates how to containerize an ASP.NET Core Minimal API application using JD.MSBuild.Containers.

## Overview

**Project Type**: ASP.NET Core Minimal API  
**Location**: `samples/MinimalApi/`  
**Image Name**: `minimal-api-sample`  
**Default Port**: 8080

## What This Sample Demonstrates

- ✅ RESTful API containerization
- ✅ Swagger/OpenAPI integration
- ✅ Health check endpoints
- ✅ JSON response handling
- ✅ Automatic port exposure
- ✅ Hot reload in development

## Project Structure

```
MinimalApi/
├── Program.cs              # Application entry point
├── MinimalApi.csproj       # Project file with Docker config
├── Dockerfile              # Auto-generated
├── appsettings.json        # Configuration
└── appsettings.Development.json
```

## Docker Configuration

The `MinimalApi.csproj` includes Docker settings:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Docker Configuration -->
    <DockerEnabled>true</DockerEnabled>
    <DockerImageName>minimal-api-sample</DockerImageName>
    <DockerImageTag>latest</DockerImageTag>
    <DockerBuildOnPublish>true</DockerBuildOnPublish>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Containers" Version="*" />
  </ItemGroup>
</Project>
```

## API Endpoints

### GET /
Returns service status.

**Response**:
```json
{
  "message": "Minimal API is running",
  "timestamp": "2026-01-17T05:30:00Z"
}
```

### GET /health
Health check endpoint for monitoring.

**Response**:
```json
{
  "status": "Healthy"
}
```

### GET /weatherforecast
Returns sample weather data.

**Response**:
```json
[
  {
    "date": "2026-01-18",
    "temperatureC": 25,
    "temperatureF": 76,
    "summary": "Warm"
  }
]
```

## Building and Running

### Local Development (Without Docker)

```bash
cd samples/MinimalApi
dotnet run
```

Browse to: `http://localhost:5000`

### Build Docker Image

```bash
# Generate Dockerfile and build image
dotnet publish --configuration Release

# Verify image was created
docker images | grep minimal-api-sample
```

### Run Container

```bash
# Run in detached mode
docker run -d -p 8080:8080 --name minimalapi minimal-api-sample:latest

# View logs
docker logs minimalapi

# Test endpoints
curl http://localhost:8080/health
curl http://localhost:8080/weatherforecast

# Stop container
docker stop minimalapi
docker rm minimalapi
```

### Run with Environment Variables

```bash
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Logging__LogLevel__Default=Debug \
  --name minimalapi \
  minimal-api-sample:latest
```

## Swagger UI

The API includes Swagger/OpenAPI documentation:

### Accessing Swagger

When running locally:
```
http://localhost:5000/swagger
```

When running in container:
```
http://localhost:8080/swagger
```

### Enabling Swagger in Production

By default, Swagger is only enabled in Development. To enable in containers:

```csharp
// Program.cs
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Or set environment variable:
```bash
docker run -e ASPNETCORE_ENVIRONMENT=Development minimal-api-sample:latest
```

## Health Checks

The API implements ASP.NET Core health checks:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health");
```

### Testing Health Endpoint

```bash
# Should return "Healthy"
curl http://localhost:8080/health

# In Docker health check
docker run --health-cmd="curl --fail http://localhost:8080/health || exit 1" \
  --health-interval=30s \
  --health-timeout=3s \
  --health-retries=3 \
  minimal-api-sample:latest
```

## Generated Dockerfile

The auto-generated Dockerfile includes:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MinimalApi.csproj", "./"]
RUN dotnet restore "MinimalApi.csproj"
COPY . .
RUN dotnet build "MinimalApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MinimalApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MinimalApi.dll"]
```

## Customization Examples

### Custom Port

```xml
<PropertyGroup>
  <DockerExposePort>5000</DockerExposePort>
</PropertyGroup>
```

Run with: `docker run -p 5000:5000 minimal-api-sample:latest`

### Custom Image Tag

```xml
<PropertyGroup>
  <DockerImageTag>v1.0.0</DockerImageTag>
</PropertyGroup>
```

### Alpine Base Image

```xml
<PropertyGroup>
  <DockerBaseImageRuntime>mcr.microsoft.com/dotnet/aspnet:8.0-alpine</DockerBaseImageRuntime>
</PropertyGroup>
```

## Integration with Docker Compose

Example `docker-compose.yml`:

```yaml
version: '3.8'

services:
  api:
    image: minimal-api-sample:latest
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Logging__LogLevel__Default=Information
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 3s
      retries: 3
```

Run with:
```bash
docker-compose up -d
```

## Deployment Examples

### Azure Container Instances

```bash
az container create \
  --resource-group myResourceGroup \
  --name minimalapi \
  --image minimal-api-sample:latest \
  --dns-name-label minimalapi-unique \
  --ports 8080
```

### Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: minimalapi
spec:
  replicas: 3
  selector:
    matchLabels:
      app: minimalapi
  template:
    metadata:
      labels:
        app: minimalapi
    spec:
      containers:
      - name: api
        image: minimal-api-sample:latest
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
---
apiVersion: v1
kind: Service
metadata:
  name: minimalapi-service
spec:
  selector:
    app: minimalapi
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: LoadBalancer
```

Apply with:
```bash
kubectl apply -f minimalapi-deployment.yaml
```

## Monitoring

### Logging

View container logs:
```bash
docker logs -f minimalapi
```

### Performance Testing

Using Apache Bench:
```bash
ab -n 1000 -c 10 http://localhost:8080/weatherforecast
```

Using hey:
```bash
hey -n 1000 -c 10 http://localhost:8080/weatherforecast
```

## Troubleshooting

### Port Already in Use

If port 8080 is already in use:
```bash
docker run -p 8081:8080 minimal-api-sample:latest
```

### Cannot Connect to API

Check if container is running:
```bash
docker ps | grep minimal-api-sample
```

Check container logs:
```bash
docker logs minimalapi
```

Test from inside container:
```bash
docker exec -it minimalapi curl http://localhost:8080/health
```

### Slow Container Startup

Increase health check initial delay:
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 3s
  retries: 3
  start_period: 40s  # Give container time to start
```

## Next Steps

- Try the [Web App Sample](sample-webapp.md) for UI containerization
- Explore [Worker Sample](sample-worker.md) for background services
- Learn about [Best Practices](../articles/best-practices.md)
- Review the [API Reference](../api/index.md)

## Related Documentation

- [Getting Started](../articles/getting-started.md)
- [Basic Tutorial](../tutorials/tutorial-basic.md)
- [Workflows](../articles/workflows.md)
