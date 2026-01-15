using JD.MSBuild.Containers.Tasks.Utilities;
using JD.MSBuild.Containers.Tests.Infrastructure;
using TinyBDD;
using Xunit.Abstractions;

namespace JD.MSBuild.Containers.Tests;

/// <summary>
/// Tests for the ProcessRunner utility class.
/// </summary>
[Feature("ProcessRunner: execute external processes with consistent logging and error handling")]
public sealed class ProcessRunnerTests(ITestOutputHelper output)
{
    [Scenario("Successful process execution returns exit code 0")]
    [Fact]
    public void SuccessfulProcessExecution()
    {
        using var folder = new TestFolder();
        var engine = new BuildEngineStub();

        var result = ProcessRunner.Run(
            engine.TaskLoggingHelper,
            "dotnet",
            "--version",
            folder.Root);

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(result.StdOut);
    }

    [Scenario("Failing process returns non-zero exit code")]
    [Fact]
    public void FailingProcessExecution()
    {
        using var folder = new TestFolder();
        var engine = new BuildEngineStub();

        var result = ProcessRunner.Run(
            engine.TaskLoggingHelper,
            "dotnet",
            "nonexistent-command",
            folder.Root);

        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Scenario("RunOrThrow succeeds for valid commands")]
    [Fact]
    public void RunOrThrowOnSuccess()
    {
        using var folder = new TestFolder();
        var engine = new BuildEngineStub();

        var exception = Record.Exception(() =>
            ProcessRunner.RunOrThrow(
                engine.TaskLoggingHelper,
                "dotnet",
                "--info",
                folder.Root));

        Assert.Null(exception);
        Assert.NotEmpty(engine.Messages);
    }

    [Scenario("RunOrThrow throws for failing commands")]
    [Fact]
    public void RunOrThrowOnFailure()
    {
        using var folder = new TestFolder();
        var engine = new BuildEngineStub();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProcessRunner.RunOrThrow(
                engine.TaskLoggingHelper,
                "dotnet",
                "nonexistent-command",
                folder.Root));

        Assert.Contains("Process failed", exception.Message);
        Assert.NotEmpty(engine.Errors);
    }

    [Scenario("Process executes in specified working directory")]
    [Fact]
    public void ProcessWithWorkingDirectory()
    {
        using var folder = new TestFolder();
        folder.WriteFile("test.txt", "test content");
        var engine = new BuildEngineStub();

        var result = ProcessRunner.Run(
            engine.TaskLoggingHelper,
            OperatingSystem.IsWindows() ? "cmd" : "ls",
            OperatingSystem.IsWindows() ? "/c dir /b" : "-la",
            folder.Root);

        Assert.True(result.Success);
        Assert.Contains("test.txt", result.StdOut);
    }

    [Scenario("Process can use environment variables")]
    [Fact]
    public void ProcessWithEnvironmentVariables()
    {
        using var folder = new TestFolder();
        var engine = new BuildEngineStub();

        var envVars = new Dictionary<string, string>
        {
            ["TEST_VAR"] = "test_value"
        };

        var command = OperatingSystem.IsWindows()
            ? ("cmd", "/c echo %TEST_VAR%")
            : ("sh", "-c \"echo $TEST_VAR\"");

        var result = ProcessRunner.Run(
            engine.TaskLoggingHelper,
            command.Item1,
            command.Item2,
            folder.Root,
            envVars);

        Assert.True(result.Success);
        Assert.Contains("test_value", result.StdOut);
    }
}
