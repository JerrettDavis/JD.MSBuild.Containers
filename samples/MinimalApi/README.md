# Minimal API Sample

A minimal ASP.NET Core Web API demonstrating containerization with **JD.MSBuild.Containers**.

## Overview

This sample demonstrates:
- Minimal API endpoints
- Health checks
- Automatic Dockerfile generation
- Container image building

## Endpoints

- `GET /` - Service status and metadata
- `GET /health` - Health check endpoint
- `GET /weatherforecast` - Weather forecast data

## Running Locally

### Without Docker

```bash
cd samples/MinimalApi
dotnet run
```

Access at `http://localhost:5000`

### With Docker (using JD.MSBuild.Containers)

```bash
# Build and containerize
dotnet publish

# Run the container
docker run -p 8080:8080 minimal-api-sample:latest
```

Access at `http://localhost:8080`

## Configuration

The project uses the following Docker configuration:

```xml
<DockerImageName>minimal-api-sample</DockerImageName>
<DockerImageTag>latest</DockerImageTag>
<DockerExposePort>8080</DockerExposePort>
```

## Verification

Test the running container:

```bash
# Health check
curl http://localhost:8080/health

# Service status
curl http://localhost:8080/

# Weather forecast
curl http://localhost:8080/weatherforecast
```
