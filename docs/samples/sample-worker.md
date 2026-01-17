# Worker Service Sample

This sample demonstrates containerizing a .NET Worker Service for background processing.

## Overview

**Project Type**: .NET Worker Service  
**Location**: `samples/Worker/`  
**Image Name**: `worker-sample`  
**Default Port**: None (background service)

## Quick Start

```bash
cd samples/Worker
dotnet publish --configuration Release
docker run worker-sample:latest
```

## What This Demonstrates

- Background service containerization
- Long-running tasks
- IHostedService implementation
- Graceful shutdown handling
- Logging in containers

## Project Structure

```
Worker/
├── Worker.cs           # Background service implementation
├── Program.cs          # Host configuration
├── Worker.csproj       # Project with Docker config
└── Dockerfile          # Auto-generated
```

## Worker Implementation

```csharp
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

## Docker Configuration

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerImageName>worker-sample</DockerImageName>
  <DockerImageTag>latest</DockerImageTag>
  <!-- No port exposed for worker services -->
</PropertyGroup>
```

## Running the Sample

```bash
# Build image
dotnet publish --configuration Release

# Run container
docker run --name worker worker-sample:latest

# View logs
docker logs -f worker

# Stop gracefully
docker stop worker
```

## Graceful Shutdown

The worker handles cancellation tokens for graceful shutdown:

```bash
# Send SIGTERM (Docker stop)
docker stop worker

# Worker receives cancellation and completes current work
```

## Next Steps

- [Console App Sample](sample-console.md)
- [Sample Overview](sample-overview.md)
