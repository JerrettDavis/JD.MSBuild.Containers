using JD.MSBuild.Containers.Tasks;
using JD.MSBuild.Containers.Tests.Infrastructure;
using TinyBDD;
using Xunit.Abstractions;

namespace JD.MSBuild.Containers.Tests;

/// <summary>
/// Tests for the GenerateDockerfile MSBuild task.
/// </summary>
[Feature("GenerateDockerfile: generate optimized multi-stage Dockerfiles for .NET projects")]
public sealed class GenerateDockerfileTests(ITestOutputHelper output)
{
    [Scenario("Basic Dockerfile is generated correctly")]
    [Fact]
    public void GenerateBasicDockerfile()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("TestApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var outputPath = folder.GetPath("Dockerfile");
        var engine = new BuildEngineStub();

        var task = new GenerateDockerfile
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            ProjectType = "console",
            BaseImage = "mcr.microsoft.com/dotnet/runtime:8.0",
            SdkImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            AssemblyName = "TestApp",
            WorkingDirectory = "/app",
            OutputPath = outputPath,
            TargetFramework = "net8.0"
        };

        var success = task.Execute();
        var content = File.ReadAllText(outputPath);

        Assert.True(success);
        Assert.True(File.Exists(outputPath));
        Assert.Contains("FROM", content);
        Assert.Contains("AS build", content);
        Assert.Contains("AS publish", content);
        Assert.Contains("AS final", content);
        Assert.Contains("dotnet restore", content);
        Assert.Contains("dotnet build", content);
        Assert.Contains("dotnet publish", content);
        Assert.Contains("ENTRYPOINT", content);
        Assert.Contains("TestApp.dll", content);
    }

    [Scenario("ASP.NET Dockerfile includes port configuration")]
    [Fact]
    public void GenerateAspNetDockerfile()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("WebApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var outputPath = folder.GetPath("Dockerfile");
        var engine = new BuildEngineStub();

        var task = new GenerateDockerfile
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            ProjectType = "aspnet",
            BaseImage = "mcr.microsoft.com/dotnet/aspnet:8.0",
            SdkImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            AssemblyName = "WebApp",
            ExposedPorts = "8080;8081",
            OutputPath = outputPath,
            TargetFramework = "net8.0"
        };

        var success = task.Execute();
        var content = File.ReadAllText(outputPath);

        Assert.True(success);
        Assert.Contains("EXPOSE 8080", content);
        Assert.Contains("EXPOSE 8081", content);
        Assert.Contains("ASPNETCORE_HTTP_PORTS", content);
        Assert.Contains("ASPNETCORE_HTTPS_PORTS", content);
    }

    [Scenario("Custom environment variables are included")]
    [Fact]
    public void GenerateWithCustomEnvironmentVariables()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("TestApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var outputPath = folder.GetPath("Dockerfile");
        var engine = new BuildEngineStub();

        var task = new GenerateDockerfile
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            ProjectType = "console",
            BaseImage = "mcr.microsoft.com/dotnet/runtime:8.0",
            SdkImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            AssemblyName = "TestApp",
            EnvironmentVariables = "ENV_VAR1=value1;ENV_VAR2=value2",
            OutputPath = outputPath,
            TargetFramework = "net8.0"
        };

        var success = task.Execute();
        var content = File.ReadAllText(outputPath);

        Assert.True(success);
        Assert.Contains("ENV ENV_VAR1=value1", content);
        Assert.Contains("ENV ENV_VAR2=value2", content);
    }

    [Scenario(".dockerignore file is generated when enabled")]
    [Fact]
    public void GenerateWithDockerIgnore()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("TestApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var outputPath = folder.GetPath("Dockerfile");
        var engine = new BuildEngineStub();

        var task = new GenerateDockerfile
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            ProjectType = "console",
            BaseImage = "mcr.microsoft.com/dotnet/runtime:8.0",
            SdkImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            AssemblyName = "TestApp",
            OutputPath = outputPath,
            TargetFramework = "net8.0",
            GenerateDockerIgnore = true
        };

        var success = task.Execute();
        var dockerIgnorePath = Path.Combine(folder.Root, ".dockerignore");

        Assert.True(success);
        Assert.True(File.Exists(dockerIgnorePath));

        var content = File.ReadAllText(dockerIgnorePath);
        Assert.Contains("**/bin/", content);
        Assert.Contains("**/obj/", content);
    }

    [Scenario("Dockerfile contains metadata comments")]
    [Fact]
    public void DockerfileContainsMetadata()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("MyApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var outputPath = folder.GetPath("Dockerfile");
        var engine = new BuildEngineStub();

        var task = new GenerateDockerfile
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            ProjectType = "console",
            BaseImage = "mcr.microsoft.com/dotnet/runtime:8.0",
            SdkImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            AssemblyName = "MyApp",
            OutputPath = outputPath,
            TargetFramework = "net8.0"
        };

        var success = task.Execute();
        var content = File.ReadAllText(outputPath);

        Assert.True(success);
        Assert.Contains("# This Dockerfile was auto-generated", content);
        Assert.Contains("# Project:", content);
        Assert.Contains("# Project Type:", content);
        Assert.Contains("# Generated:", content);
        Assert.Contains("MyApp.csproj", content);
    }

    [Scenario("Missing project path fails validation")]
    [Fact]
    public void MissingProjectPathShouldFail()
    {
        using var folder = new TestFolder();
        var engine = new BuildEngineStub();

        var task = new GenerateDockerfile
        {
            BuildEngine = engine,
            ProjectPath = "",
            ProjectType = "console",
            BaseImage = "mcr.microsoft.com/dotnet/runtime:8.0",
            SdkImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            AssemblyName = "Test",
            OutputPath = folder.GetPath("Dockerfile")
        };

        var success = task.Execute();

        Assert.False(success);
        Assert.NotEmpty(engine.Errors);
    }

    [Scenario("Dockerfile uses build configuration ARG")]
    [Fact]
    public void DockerfileShouldUseArgForConfiguration()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("TestApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var outputPath = folder.GetPath("Dockerfile");
        var engine = new BuildEngineStub();

        var task = new GenerateDockerfile
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            ProjectType = "console",
            BaseImage = "mcr.microsoft.com/dotnet/runtime:8.0",
            SdkImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            AssemblyName = "TestApp",
            OutputPath = outputPath,
            TargetFramework = "net8.0"
        };

        var success = task.Execute();
        var content = File.ReadAllText(outputPath);

        Assert.True(success);
        Assert.Contains("ARG BUILD_CONFIGURATION=Release", content);
        Assert.Contains("$BUILD_CONFIGURATION", content);
    }

    [Scenario("Dockerfile optimizes for layer caching")]
    [Fact]
    public void DockerfileShouldOptimizeForLayerCaching()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("TestApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var outputPath = folder.GetPath("Dockerfile");
        var engine = new BuildEngineStub();

        var task = new GenerateDockerfile
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            ProjectType = "console",
            BaseImage = "mcr.microsoft.com/dotnet/runtime:8.0",
            SdkImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            AssemblyName = "TestApp",
            OutputPath = outputPath,
            TargetFramework = "net8.0"
        };

        var success = task.Execute();
        var content = File.ReadAllText(outputPath);

        Assert.True(success);
        // Project file should be copied before source files for layer caching
        var copyProjectIndex = content.IndexOf("COPY [");
        var copySourceIndex = content.IndexOf("COPY . .");
        Assert.True(copyProjectIndex < copySourceIndex,
            "Project file should be copied before source files for layer caching");

        // Restore should happen before copying all source files
        var restoreIndex = content.IndexOf("dotnet restore");
        Assert.True(restoreIndex < copySourceIndex,
            "Restore should happen before copying all source files");
    }
}
