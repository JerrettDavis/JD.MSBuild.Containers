using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JD.MSBuild.Containers.Tasks.Utilities;

/// <summary>
/// Encapsulates the result of a process execution.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StdOut">Standard output from the process.</param>
/// <param name="StdErr">Standard error output from the process.</param>
public readonly record struct ProcessResult(
    int ExitCode,
    string StdOut,
    string StdErr
)
{
    /// <summary>
    /// Gets a value indicating whether the process completed successfully (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Helper for running external processes with consistent logging and error handling.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a unified process execution mechanism used by Docker-related tasks,
/// eliminating code duplication and ensuring consistent process handling across the library.
/// </para>
/// <para>
/// On Windows, .cmd and .bat files are automatically wrapped with cmd.exe for proper execution.
/// Shell scripts on Unix-like systems are executed directly if they have execute permissions.
/// </para>
/// </remarks>
internal static class ProcessRunner
{
    /// <summary>
    /// Runs a process and returns the result without throwing on non-zero exit code.
    /// </summary>
    /// <param name="log">Task logging helper for diagnostic output.</param>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <param name="importance">Message importance level for logging command execution.</param>
    /// <returns>A <see cref="ProcessResult"/> containing exit code and captured output.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the process fails to start.</exception>
    public static ProcessResult Run(
        TaskLoggingHelper log,
        string fileName,
        string args,
        string workingDir,
        IDictionary<string, string>? environmentVariables = null,
        MessageImportance importance = MessageImportance.High)
    {
        var (normalizedFileName, normalizedArgs) = NormalizeCommand(fileName, args);
        
        log.LogMessage(importance, $"> {normalizedFileName} {normalizedArgs}");

        var psi = new ProcessStartInfo
        {
            FileName = normalizedFileName,
            Arguments = normalizedArgs,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (environmentVariables != null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                psi.Environment[key] = value;
            }
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {normalizedFileName}");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Runs a process and throws if it fails (non-zero exit code).
    /// </summary>
    /// <param name="log">Task logging helper for diagnostic output.</param>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <param name="importance">Message importance level for logging command execution.</param>
    /// <exception cref="InvalidOperationException">Thrown when the process exits with a non-zero code.</exception>
    public static void RunOrThrow(
        TaskLoggingHelper log,
        string fileName,
        string args,
        string workingDir,
        IDictionary<string, string>? environmentVariables = null,
        MessageImportance importance = MessageImportance.High)
    {
        var result = Run(log, fileName, args, workingDir, environmentVariables, importance);

        if (!string.IsNullOrWhiteSpace(result.StdOut))
        {
            log.LogMessage(MessageImportance.High, result.StdOut);
        }

        if (!string.IsNullOrWhiteSpace(result.StdErr))
        {
            log.LogError(result.StdErr);
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Process failed with exit code {result.ExitCode}: {fileName} {args}");
        }
    }

    /// <summary>
    /// Runs a process with detailed output logging suitable for build operations.
    /// </summary>
    /// <param name="log">Task logging helper for diagnostic output.</param>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="errorMessage">Custom error message for failures.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <param name="importance">Message importance level for logging command execution.</param>
    /// <exception cref="InvalidOperationException">Thrown when the process exits with a non-zero code.</exception>
    public static void RunBuildOrThrow(
        TaskLoggingHelper log,
        string fileName,
        string args,
        string workingDir,
        string? errorMessage = null,
        IDictionary<string, string>? environmentVariables = null,
        MessageImportance importance = MessageImportance.High)
    {
        var result = Run(log, fileName, args, workingDir, environmentVariables, importance);

        if (!result.Success)
        {
            if (!string.IsNullOrWhiteSpace(result.StdOut))
            {
                log.LogError(result.StdOut);
            }

            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                log.LogError(result.StdErr);
            }

            throw new InvalidOperationException(
                errorMessage ?? $"Build failed with exit code {result.ExitCode}");
        }

        if (!string.IsNullOrWhiteSpace(result.StdOut))
        {
            log.LogMessage(MessageImportance.Normal, result.StdOut);
        }

        if (!string.IsNullOrWhiteSpace(result.StdErr))
        {
            log.LogMessage(MessageImportance.Normal, result.StdErr);
        }
    }

    /// <summary>
    /// Normalizes a command for execution, handling platform-specific requirements.
    /// </summary>
    /// <param name="fileName">The executable or script file to run.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A tuple containing the normalized file name and arguments.</returns>
    /// <remarks>
    /// On Windows, .cmd and .bat files must be invoked through cmd.exe.
    /// On Unix-like systems, shell scripts can be executed directly if they have proper permissions.
    /// </remarks>
    private static (string FileName, string Args) NormalizeCommand(string fileName, string args)
    {
        if (OperatingSystem.IsWindows())
        {
            if (fileName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            {
                return ("cmd.exe", $"/c \"{fileName}\" {args}");
            }
        }

        return (fileName, args);
    }
}
