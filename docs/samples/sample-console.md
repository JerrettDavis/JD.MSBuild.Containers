# Console Application Sample

This sample demonstrates containerizing a .NET console application for batch processing and CLI tools.

## Overview

**Project Type**: .NET Console Application  
**Location**: `samples/ConsoleApp/`  
**Image Name**: `console-app-sample`  
**Default Port**: None

## Quick Start

```bash
cd samples/ConsoleApp
dotnet publish --configuration Release
docker run console-app-sample:latest
```

## What This Demonstrates

- Console app containerization
- Command-line argument handling
- One-time execution pattern
- Exit code handling
- Standard input/output in containers

## Project Structure

```
ConsoleApp/
├── Program.cs              # Console application logic
├── ConsoleApp.csproj       # Project with Docker config
└── Dockerfile              # Auto-generated
```

## Application Code

```csharp
// Program.cs
Console.WriteLine("Hello from containerized console app!");
Console.WriteLine($"Arguments: {string.Join(", ", args)}");

// Simulate some work
await Task.Delay(2000);

Console.WriteLine("Processing complete.");
return 0; // Exit code
```

## Docker Configuration

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerImageName>console-app-sample</DockerImageName>
  <DockerImageTag>latest</DockerImageTag>
</PropertyGroup>
```

## Running the Sample

### Basic Execution

```bash
dotnet publish --configuration Release
docker run console-app-sample:latest
```

### With Arguments

```bash
docker run console-app-sample:latest arg1 arg2 arg3
```

### With Input Redirection

```bash
echo "input data" | docker run -i console-app-sample:latest
```

### Capturing Exit Code

```bash
docker run console-app-sample:latest
echo "Exit code: $?"
```

## Use Cases

Console app containers are useful for:

- **Batch Jobs**: Process files, generate reports
- **CI/CD Tools**: Custom build steps, deployment scripts
- **Database Migrations**: One-time schema updates
- **Data Processing**: ETL pipelines
- **Administrative Tasks**: Cleanup, maintenance

## Kubernetes Job Example

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: console-job
spec:
  template:
    spec:
      containers:
      - name: console
        image: console-app-sample:latest
        args: ["--process-all", "--verbose"]
      restartPolicy: Never
  backoffLimit: 4
```

## Next Steps

- [Sample Overview](sample-overview.md)
- [Basic Tutorial](../tutorials/tutorial-basic.md)
- [Workflows](../articles/workflows.md)
