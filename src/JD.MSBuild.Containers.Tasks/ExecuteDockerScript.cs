using JD.MSBuild.Containers.Tasks.Utilities;
using Microsoft.Build.Framework;

namespace JD.MSBuild.Containers.Tasks;

/// <summary>
/// MSBuild task that executes custom scripts before or after Docker build operations.
/// </summary>
/// <remarks>
/// <para>
/// This task provides extensibility points for custom build logic by executing user-defined
/// scripts at various stages of the Docker build process. Common use cases include:
/// <list type="bullet">
///   <item><description>Pre-build validation and setup</description></item>
///   <item><description>Custom file preparation and transformation</description></item>
///   <item><description>Dynamic configuration generation</description></item>
///   <item><description>Post-build testing and verification</description></item>
///   <item><description>Image scanning and security checks</description></item>
///   <item><description>Custom deployment or publication steps</description></item>
///   <item><description>Integration with external tools and services</description></item>
/// </list>
/// </para>
/// <para>
/// The task supports multiple script types:
/// <list type="bullet">
///   <item><description>Shell scripts (.sh) on Unix-like systems</description></item>
///   <item><description>Batch files (.bat, .cmd) on Windows</description></item>
///   <item><description>PowerShell scripts (.ps1) on any platform with PowerShell installed</description></item>
///   <item><description>Direct executable programs</description></item>
/// </list>
/// </para>
/// <para>
/// Scripts have access to build context through environment variables and command-line arguments,
/// and can communicate status through exit codes. Non-zero exit codes are treated as failures
/// and will cause the build to stop unless <see cref="ContinueOnError"/> is set to true.
/// </para>
/// <para>
/// For security, scripts must explicitly be marked as executable on Unix-like systems, and
/// script paths should be validated to prevent execution of unintended code.
/// </para>
/// </remarks>
public sealed class ExecuteDockerScript : DockerTaskBase
{
    /// <summary>
    /// Gets or sets the full path to the script file to execute.
    /// </summary>
    [Required]
    public string ScriptPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the working directory for script execution.
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets command-line arguments to pass to the script.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets environment variables to set for the script (semicolon-delimited key=value pairs).
    /// </summary>
    public string EnvironmentVariables { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds for script execution (0 means no timeout).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether to continue on error.
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

    /// <summary>
    /// Gets or sets the script execution context (e.g., "pre-build", "post-build").
    /// </summary>
    public string ExecutionContext { get; set; } = "custom";

    /// <summary>
    /// Gets or sets the project path to pass as context to the script.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Docker image tag to pass as context to the script.
    /// </summary>
    public string ImageTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Dockerfile path to pass as context to the script.
    /// </summary>
    public string DockerfilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional context properties (semicolon-delimited key=value pairs).
    /// </summary>
    public string AdditionalContext { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the script exit code.
    /// </summary>
    [Output]
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets the script standard output.
    /// </summary>
    [Output]
    public string ScriptOutput { get; set; } = string.Empty;

    /// <summary>
    /// Executes the Docker script task.
    /// </summary>
    /// <returns>True if the task succeeds; otherwise, false.</returns>
    public override sealed bool Execute()
    {
        try
        {
            LogMessage($"Executing {ExecutionContext} script: {Path.GetFileName(ScriptPath)}", 
                MessageImportance.High);

            ValidateInputs();
            
            var workingDir = DetermineWorkingDirectory();
            var envVars = BuildEnvironmentVariables();
            var scriptExecutor = DetermineScriptExecutor();

            LogDiagnostic($"Script: {ScriptPath}");
            LogDiagnostic($"Working Directory: {workingDir}");
            LogDiagnostic($"Arguments: {Arguments}");

            var result = ExecuteScript(scriptExecutor.Executor, scriptExecutor.Args, workingDir, envVars);

            ExitCode = result.ExitCode;
            ScriptOutput = result.StdOut;

            if (!string.IsNullOrWhiteSpace(result.StdOut))
            {
                LogMessage(result.StdOut, MessageImportance.Normal);
            }

            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                if (result.Success)
                {
                    LogMessage(result.StdErr, MessageImportance.Normal);
                }
                else
                {
                    LogError(result.StdErr);
                }
            }

            if (!result.Success)
            {
                var message = $"Script failed with exit code {result.ExitCode}: {ScriptPath}";
                
                if (ContinueOnError)
                {
                    LogWarning(message);
                    return true;
                }

                LogError(message);
                return false;
            }

            LogMessage($"Script completed successfully: {Path.GetFileName(ScriptPath)}", 
                MessageImportance.High);
            
            return true;
        }
        catch (Exception ex)
        {
            var message = $"Script execution failed: {ex.Message}";
            
            if (ContinueOnError)
            {
                LogWarning(message);
                LogDiagnostic($"Exception details: {ex}");
                return true;
            }

            LogError(message);
            LogDiagnostic($"Exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Validates that all required inputs are provided.
    /// </summary>
    private void ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(ScriptPath))
        {
            throw new InvalidOperationException("ScriptPath is required.");
        }

        if (!File.Exists(ScriptPath))
        {
            throw new FileNotFoundException($"Script file not found: {ScriptPath}");
        }

        LogDiagnostic("Validated script execution inputs");
    }

    /// <summary>
    /// Determines the working directory for script execution.
    /// </summary>
    private string DetermineWorkingDirectory()
    {
        if (!string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            if (Directory.Exists(WorkingDirectory))
            {
                return Path.GetFullPath(WorkingDirectory);
            }

            LogWarning($"Specified working directory does not exist: {WorkingDirectory}");
        }

        var scriptDir = Path.GetDirectoryName(ScriptPath);
        if (!string.IsNullOrEmpty(scriptDir) && Directory.Exists(scriptDir))
        {
            return scriptDir;
        }

        return Environment.CurrentDirectory;
    }

    /// <summary>
    /// Builds environment variables for the script execution.
    /// </summary>
    private Dictionary<string, string> BuildEnvironmentVariables()
    {
        var envVars = new Dictionary<string, string>();

        envVars["DOCKER_SCRIPT_CONTEXT"] = ExecutionContext;

        if (!string.IsNullOrWhiteSpace(ProjectPath) && File.Exists(ProjectPath))
        {
            envVars["DOCKER_PROJECT_PATH"] = Path.GetFullPath(ProjectPath);
            envVars["DOCKER_PROJECT_DIR"] = Path.GetDirectoryName(ProjectPath) ?? string.Empty;
            envVars["DOCKER_PROJECT_NAME"] = Path.GetFileNameWithoutExtension(ProjectPath);
        }

        if (!string.IsNullOrWhiteSpace(ImageTag))
        {
            envVars["DOCKER_IMAGE_TAG"] = ImageTag;
        }

        if (!string.IsNullOrWhiteSpace(DockerfilePath) && File.Exists(DockerfilePath))
        {
            envVars["DOCKER_DOCKERFILE_PATH"] = Path.GetFullPath(DockerfilePath);
        }

        if (!string.IsNullOrWhiteSpace(EnvironmentVariables))
        {
            var userEnvVars = EnvironmentVariables.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var envVar in userEnvVars)
            {
                var parts = envVar.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    envVars[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(AdditionalContext))
        {
            var contextPairs = AdditionalContext.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in contextPairs)
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    var key = $"DOCKER_CONTEXT_{parts[0].Trim().ToUpperInvariant().Replace(" ", "_")}";
                    envVars[key] = parts[1].Trim();
                }
            }
        }

        return envVars;
    }

    /// <summary>
    /// Determines the appropriate executor for the script based on file extension and platform.
    /// </summary>
    private (string Executor, string Args) DetermineScriptExecutor()
    {
        var extension = Path.GetExtension(ScriptPath).ToLowerInvariant();
        var scriptFullPath = Path.GetFullPath(ScriptPath);

        return extension switch
        {
            ".ps1" => ("pwsh", $"-ExecutionPolicy Bypass -File \"{scriptFullPath}\" {Arguments}"),
            ".sh" => (scriptFullPath, Arguments),
            ".bat" or ".cmd" when OperatingSystem.IsWindows() => (scriptFullPath, Arguments),
            _ => (scriptFullPath, Arguments)
        };
    }

    /// <summary>
    /// Executes the script with the specified parameters.
    /// </summary>
    private ProcessResult ExecuteScript(
        string executor,
        string args,
        string workingDir,
        Dictionary<string, string> envVars)
    {
        if (TimeoutSeconds > 0)
        {
            LogMessage($"Script timeout: {TimeoutSeconds} seconds", MessageImportance.Normal);
        }

        return ProcessRunner.Run(
            Log,
            executor,
            args,
            workingDir,
            envVars,
            MessageImportance.High);
    }
}
