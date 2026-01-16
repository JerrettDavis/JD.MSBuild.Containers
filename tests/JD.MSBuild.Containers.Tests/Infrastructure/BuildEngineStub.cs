using System.Collections;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JD.MSBuild.Containers.Tests.Infrastructure;

/// <summary>
/// Mock implementation of IBuildEngine for testing MSBuild tasks.
/// </summary>
/// <remarks>
/// This class captures all log messages, warnings, and errors emitted by tasks,
/// allowing tests to verify logging behavior and inspect diagnostic output.
/// </remarks>
internal sealed class BuildEngineStub : IBuildEngine
{
    private readonly Lazy<TaskLoggingHelper> _loggingHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildEngineStub"/> class.
    /// </summary>
    public BuildEngineStub()
    {
        _loggingHelper = new Lazy<TaskLoggingHelper>(() =>
        {
            var task = new StubTask { BuildEngine = this };
            return new TaskLoggingHelper(task);
        });
    }

    /// <summary>
    /// Gets the list of error events logged during task execution.
    /// </summary>
    public List<BuildErrorEventArgs> Errors { get; } = [];

    /// <summary>
    /// Gets the list of warning events logged during task execution.
    /// </summary>
    public List<BuildWarningEventArgs> Warnings { get; } = [];

    /// <summary>
    /// Gets the list of message events logged during task execution.
    /// </summary>
    public List<BuildMessageEventArgs> Messages { get; } = [];

    /// <summary>
    /// Gets a TaskLoggingHelper instance for tasks that need it.
    /// </summary>
    public TaskLoggingHelper TaskLoggingHelper => _loggingHelper.Value;

    /// <summary>
    /// Gets a value indicating whether to continue on error.
    /// </summary>
    public bool ContinueOnError => false;

    /// <summary>
    /// Gets the line number of the task node.
    /// </summary>
    public int LineNumberOfTaskNode => 0;

    /// <summary>
    /// Gets the column number of the task node.
    /// </summary>
    public int ColumnNumberOfTaskNode => 0;

    /// <summary>
    /// Gets the project file of the task node.
    /// </summary>
    public string ProjectFileOfTaskNode => string.Empty;

    /// <summary>
    /// Builds a project file (not implemented in stub).
    /// </summary>
    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
    {
        return true;
    }

    /// <summary>
    /// Logs a custom event.
    /// </summary>
    public void LogCustomEvent(CustomBuildEventArgs e)
    {
        Messages.Add(new BuildMessageEventArgs(e.Message, string.Empty, string.Empty, MessageImportance.Low));
    }

    /// <summary>
    /// Logs an error event.
    /// </summary>
    public void LogErrorEvent(BuildErrorEventArgs e)
    {
        Errors.Add(e);
    }

    /// <summary>
    /// Logs a message event.
    /// </summary>
    public void LogMessageEvent(BuildMessageEventArgs e)
    {
        Messages.Add(e);
    }

    /// <summary>
    /// Logs a warning event.
    /// </summary>
    public void LogWarningEvent(BuildWarningEventArgs e)
    {
        Warnings.Add(e);
    }

    /// <summary>
    /// Gets all messages as a single string for easy assertion.
    /// </summary>
    public string GetMessagesAsString()
    {
        return string.Join(Environment.NewLine, Messages.Select(m => m.Message));
    }

    /// <summary>
    /// Gets all errors as a single string for easy assertion.
    /// </summary>
    public string GetErrorsAsString()
    {
        return string.Join(Environment.NewLine, Errors.Select(e => e.Message));
    }

    /// <summary>
    /// Gets all warnings as a single string for easy assertion.
    /// </summary>
    public string GetWarningsAsString()
    {
        return string.Join(Environment.NewLine, Warnings.Select(w => w.Message));
    }

    /// <summary>
    /// Checks if any error was logged.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Checks if any warning was logged.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Minimal task implementation to satisfy TaskLoggingHelper requirements.
    /// </summary>
    private sealed class StubTask : ITask
    {
        public IBuildEngine? BuildEngine { get; set; }
        public ITaskHost? HostObject { get; set; }
        public bool Execute() => true;
    }
}
