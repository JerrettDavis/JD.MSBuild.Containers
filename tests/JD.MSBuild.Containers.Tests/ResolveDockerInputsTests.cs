using JD.MSBuild.Containers.Tasks;
using JD.MSBuild.Containers.Tests.Infrastructure;
using TinyBDD;
using Xunit.Abstractions;

namespace JD.MSBuild.Containers.Tests;

/// <summary>
/// Tests for the ResolveDockerInputs MSBuild task.
/// </summary>
[Feature("ResolveDockerInputs: analyze project structure and resolve Docker container configuration")]
public sealed class ResolveDockerInputsTests(ITestOutputHelper output)
{
    [Scenario("ASP.NET Core project is detected correctly")]
    [Fact]
    public void ResolveAspNetCoreApplication()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("WebApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var engine = new BuildEngineStub();
        var task = new ResolveDockerInputs
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            TargetFramework = "net8.0",
            RuntimeIdentifier = "linux-x64",
            OutputType = "Exe",
            IsWebApplication = true,
            AssemblyName = "WebApp",
            OutputPath = Path.Combine(folder.Root, "bin")
        };

        var success = task.Execute();

        Assert.True(success);
        Assert.Equal("aspnet", task.ProjectType);
        Assert.Contains("aspnet", task.BaseImage);
        Assert.Contains("sdk", task.SdkImage);
        Assert.Contains("dotnet", task.EntryPoint);
        Assert.Contains("8080", task.ExposedPorts);
    }

    [Scenario("Console application project is detected correctly")]
    [Fact]
    public void ResolveConsoleApplication()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("ConsoleApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var engine = new BuildEngineStub();
        var task = new ResolveDockerInputs
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            TargetFramework = "net8.0",
            RuntimeIdentifier = "linux-x64",
            OutputType = "Exe",
            IsWebApplication = false,
            AssemblyName = "ConsoleApp",
            OutputPath = Path.Combine(folder.Root, "bin")
        };

        var success = task.Execute();

        Assert.True(success);
        Assert.Equal("console", task.ProjectType);
        Assert.Contains("runtime", task.BaseImage);
        Assert.Contains("sdk", task.SdkImage);
        Assert.Contains("dotnet", task.EntryPoint);
        Assert.Empty(task.ExposedPorts);
    }

    [Scenario("Worker service is detected by package references")]
    [Fact]
    public void ResolveWorkerService()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("WorkerService.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var engine = new BuildEngineStub();
        var task = new ResolveDockerInputs
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            TargetFramework = "net8.0",
            RuntimeIdentifier = "linux-x64",
            OutputType = "Exe",
            IsWebApplication = false,
            AssemblyName = "WorkerService",
            OutputPath = Path.Combine(folder.Root, "bin"),
            PackageReferences = "Microsoft.Extensions.Hosting"
        };

        var success = task.Execute();

        Assert.True(success);
        Assert.Equal("worker", task.ProjectType);
        Assert.Contains("runtime", task.BaseImage);
    }

    [Scenario("Target framework version is extracted correctly")]
    [Fact]
    public void TargetFrameworkNet9()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("WebApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var engine = new BuildEngineStub();
        var task = new ResolveDockerInputs
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            TargetFramework = "net9.0",
            RuntimeIdentifier = "linux-x64",
            IsWebApplication = true,
            AssemblyName = "WebApp",
            OutputPath = folder.Root,
            OutputType = "Exe"
        };

        var success = task.Execute();

        Assert.True(success);
        Assert.Contains("9.0", task.BaseImage);
        Assert.Contains("9.0", task.SdkImage);
    }

    [Scenario("Missing project path fails validation")]
    [Fact]
    public void MissingProjectPathShouldFail()
    {
        using var folder = new TestFolder();
        var engine = new BuildEngineStub();

        var task = new ResolveDockerInputs
        {
            BuildEngine = engine,
            ProjectPath = "",
            TargetFramework = "net8.0",
            AssemblyName = "Test",
            OutputPath = folder.Root
        };

        var success = task.Execute();

        Assert.False(success);
        Assert.NotEmpty(engine.Errors);
    }

    [Scenario("Nonexistent project file fails validation")]
    [Fact]
    public void NonexistentProjectFileShouldFail()
    {
        using var folder = new TestFolder();
        var engine = new BuildEngineStub();

        var task = new ResolveDockerInputs
        {
            BuildEngine = engine,
            ProjectPath = Path.Combine(folder.Root, "NonExistent.csproj"),
            TargetFramework = "net8.0",
            AssemblyName = "Test",
            OutputPath = folder.Root
        };

        var success = task.Execute();

        Assert.False(success);
        Assert.NotEmpty(engine.Errors);
    }

    [Scenario("Missing target framework fails validation")]
    [Fact]
    public void MissingTargetFrameworkShouldFail()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("Test.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var engine = new BuildEngineStub();
        var task = new ResolveDockerInputs
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            TargetFramework = "",
            AssemblyName = "Test",
            OutputPath = folder.Root
        };

        var success = task.Execute();

        Assert.False(success);
        Assert.NotEmpty(engine.Errors);
    }

    [Scenario("Task logs informational messages")]
    [Fact]
    public void LoggingOutputs()
    {
        using var folder = new TestFolder();
        var projectPath = folder.WriteFile("WebApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var engine = new BuildEngineStub();
        var task = new ResolveDockerInputs
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            TargetFramework = "net8.0",
            IsWebApplication = true,
            AssemblyName = "WebApp",
            OutputPath = folder.Root,
            OutputType = "Exe"
        };

        task.Execute();

        Assert.NotEmpty(engine.Messages);
        var messages = engine.GetMessagesAsString();
        Assert.Contains("aspnet", messages, StringComparison.OrdinalIgnoreCase);
    }
}
