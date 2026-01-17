# Web App Sample (Razor Pages)

This sample demonstrates containerizing an ASP.NET Core Razor Pages web application with static assets.

## Overview

**Project Type**: ASP.NET Core Razor Pages  
**Location**: `samples/WebApp/`  
**Image Name**: `webapp-sample`  
**Default Port**: 8080

## Quick Start

```bash
cd samples/WebApp
dotnet publish --configuration Release
docker run -p 8080:8080 webapp-sample:latest
```

Browse to: `http://localhost:8080`

## What This Demonstrates

- Web UI containerization
- Static file handling (CSS, JS, images)
- Razor Pages server-side rendering
- Cookie-based sessions
- Form handling

## Project Structure

```
WebApp/
├── Pages/
│   ├── Index.cshtml
│   ├── Privacy.cshtml
│   └── Error.cshtml
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── lib/
├── Program.cs
├── WebApp.csproj
└── Dockerfile (auto-generated)
```

## Docker Configuration

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerImageName>webapp-sample</DockerImageName>
  <DockerImageTag>latest</DockerImageTag>
  <DockerExposePort>8080</DockerExposePort>
</PropertyGroup>
```

## Running the Sample

### Local Development
```bash
dotnet run
# Browse to http://localhost:5000
```

### Container
```bash
dotnet publish --configuration Release
docker run -d -p 8080:8080 --name webapp webapp-sample:latest

# Test
curl http://localhost:8080
curl http://localhost:8080/Privacy
```

## Static Assets

Static files are properly included in the container:

```csharp
// Program.cs
app.UseStaticFiles();
```

All files in `wwwroot/` are copied to the container and served correctly.

## Next Steps

- [Worker Sample](sample-worker.md)
- [Console App Sample](sample-console.md)
- [Best Practices](../articles/best-practices.md)
