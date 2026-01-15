using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace JD.MSBuild.Containers.Tasks;

/// <summary>
/// Base class for all Docker-related MSBuild tasks providing common logging infrastructure.
/// </summary>
/// <remarks>
/// This abstract class provides a consistent logging interface for all Docker tasks,
/// supporting different verbosity levels and message categorization.
/// </remarks>
public abstract class DockerTaskBase : MSBuildTask
{
    /// <summary>
    /// Gets or sets the log verbosity level.
    /// </summary>
    /// <value>
    /// One of: "quiet", "minimal", "normal", "detailed", "diagnostic".
    /// Default is "minimal".
    /// </value>
    [Required]
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Logs a message with the specified importance based on verbosity settings.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="importance">The importance level of the message.</param>
    protected void LogMessage(string message, MessageImportance importance = MessageImportance.Normal)
    {
        if (ShouldLog(importance))
        {
            Log.LogMessage(importance, message);
        }
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The warning message.</param>
    protected void LogWarning(string message)
    {
        Log.LogWarning(message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    protected void LogError(string message)
    {
        Log.LogError(message);
    }

    /// <summary>
    /// Logs a detailed diagnostic message (only when verbosity is diagnostic).
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    protected void LogDiagnostic(string message)
    {
        if (LogVerbosity.Equals("diagnostic", StringComparison.OrdinalIgnoreCase))
        {
            Log.LogMessage(MessageImportance.Low, $"[DIAGNOSTIC] {message}");
        }
    }

    /// <summary>
    /// Determines whether a message with the given importance should be logged
    /// based on the current verbosity setting.
    /// </summary>
    /// <param name="importance">The message importance level.</param>
    /// <returns>True if the message should be logged; otherwise, false.</returns>
    private bool ShouldLog(MessageImportance importance)
    {
        return LogVerbosity.ToLowerInvariant() switch
        {
            "quiet" => importance == MessageImportance.High,
            "minimal" => importance >= MessageImportance.Normal,
            "normal" => importance >= MessageImportance.Normal,
            "detailed" => true,
            "diagnostic" => true,
            _ => importance >= MessageImportance.Normal
        };
    }
}
