# JD.MSBuild.Containers Documentation

Welcome to the **JD.MSBuild.Containers** documentation site. This library provides enterprise-grade MSBuild integration for Docker containerization, enabling automatic Dockerfile generation and Docker builds directly from your .NET project files.

## What is JD.MSBuild.Containers?

**JD.MSBuild.Containers** is an MSBuild-integrated containerization library that automates Docker workflows for .NET applications. It eliminates manual Dockerfile creation and Docker CLI steps by integrating container operations directly into your MSBuild pipeline.

### Key Features

- ✅ **Automatic Dockerfile Generation** - Create optimized multi-stage Dockerfiles during build
- ✅ **Granular Control** - Enable/disable features independently (generate, build, run, push)
- ✅ **Zero Manual Steps** - Everything happens during `dotnet build` or `dotnet publish`
- ✅ **CI/CD Ready** - Full GitHub Actions and Azure DevOps support
- ✅ **Incremental Builds** - Smart fingerprinting skips unchanged files
- ✅ **Pre/Post Scripts** - Execute custom scripts at any lifecycle stage
- ✅ **Multi-Mode Support** - Generate-only, build-only, or full automation

## Quick Links

### Getting Started

- [Introduction](articles/introduction.md) - Learn about the library and its architecture
- [Getting Started](articles/getting-started.md) - Install and configure your first project
- [Core Concepts](articles/concepts.md) - Understand how JD.MSBuild.Containers works

### Guides & Tutorials

- [Basic Tutorial](tutorials/tutorial-basic.md) - Step-by-step guide for beginners
- [Advanced Tutorial](tutorials/tutorial-advanced.md) - Complex scenarios and customization
- [Workflows](articles/workflows.md) - Local development and CI/CD workflows
- [Best Practices](articles/best-practices.md) - Recommended patterns and approaches

### Samples

- [Sample Overview](samples/sample-overview.md) - Browse all available samples
- [Web API Sample](samples/sample-webapi.md) - ASP.NET Core Minimal API
- [Web App Sample](samples/sample-webapp.md) - Razor Pages application
- [Worker Sample](samples/sample-worker.md) - Background worker service
- [Console App Sample](samples/sample-console.md) - Command-line application

### Reference

- [API Reference](api/index.md) - Complete API documentation
- MSBuild Properties - See API Reference
- MSBuild Tasks - See API Reference

## Quick Start Example

Add the package to your project:

```bash
dotnet add package JD.MSBuild.Containers
```

Enable Docker in your `.csproj`:

```xml
<PropertyGroup>
  <DockerEnabled>true</DockerEnabled>
  <DockerImageName>myapp</DockerImageName>
  <DockerImageTag>latest</DockerImageTag>
</PropertyGroup>
```

Build your project:

```bash
dotnet build
```

A Dockerfile will be automatically generated. To build the Docker image:

```bash
dotnet publish
```

## Support & Contributing

- **GitHub Repository**: [JerrettDavis/JD.MSBuild.Containers](https://github.com/JerrettDavis/JD.MSBuild.Containers)
- **Issues**: [Report bugs or request features](https://github.com/JerrettDavis/JD.MSBuild.Containers/issues)
- **Contributing**: See [CONTRIBUTING.md](https://github.com/JerrettDavis/JD.MSBuild.Containers/blob/main/CONTRIBUTING.md)

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/JerrettDavis/JD.MSBuild.Containers/blob/main/LICENSE) file for details.
