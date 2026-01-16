using JD.MSBuild.Containers.Tasks.Utilities;
using Microsoft.Build.Framework;

namespace JD.MSBuild.Containers.Tasks;

/// <summary>
/// MSBuild task that executes the Docker run command to start a container from an image.
/// </summary>
/// <remarks>
/// <para>
/// This task wraps the <c>docker run</c> command, providing MSBuild integration for testing
/// and running containerized applications during the build process. It supports common Docker
/// run options including:
/// <list type="bullet">
///   <item><description>Port mapping for network access</description></item>
///   <item><description>Volume mounting for persistent data</description></item>
///   <item><description>Environment variable configuration</description></item>
///   <item><description>Network configuration</description></item>
///   <item><description>Container naming</description></item>
///   <item><description>Detached and interactive modes</description></item>
///   <item><description>Resource limits and constraints</description></item>
/// </list>
/// </para>
/// <para>
/// The task can run containers in detached mode (background) or attached mode (foreground),
/// and supports both short-lived command execution and long-running service containers.
/// </para>
/// <para>
/// For web applications, this task is particularly useful for local testing and validation
/// of containerized deployments before pushing images to registries or production environments.
/// </para>
/// </remarks>
public sealed class ExecuteDockerRun : DockerTaskBase
{
    /// <summary>
    /// Gets or sets the Docker image name and tag to run.
    /// </summary>
    [Required]
    public string ImageTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets port mappings (semicolon-delimited, e.g., "8080:80;8443:443").
    /// </summary>
    public string PortMappings { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets volume mounts (semicolon-delimited, e.g., "/host/path:/container/path").
    /// </summary>
    public string VolumeMounts { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets environment variables (semicolon-delimited key=value pairs).
    /// </summary>
    public string EnvironmentVariables { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the network to connect to (e.g., "bridge", "host", custom network name).
    /// </summary>
    public string Network { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to run in detached mode (background).
    /// </summary>
    public bool Detached { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to remove the container after it exits.
    /// </summary>
    public bool RemoveOnExit { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to run in interactive mode with TTY.
    /// </summary>
    public bool Interactive { get; set; } = false;

    /// <summary>
    /// Gets or sets additional Docker run options.
    /// </summary>
    public string AdditionalOptions { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command to run inside the container (overrides image default).
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the working directory for the Docker command.
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds to wait for the container to start.
    /// </summary>
    public int StartTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the container ID output from the run command.
    /// </summary>
    [Output]
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// Executes the Docker run task.
    /// </summary>
    /// <returns>True if the task succeeds; otherwise, false.</returns>
    public override sealed bool Execute()
    {
        try
        {
            LogMessage($"Running Docker container from image: {ImageTag}", MessageImportance.High);

            ValidateInputs();
            ValidateDockerInstalled();

            var args = BuildDockerArguments();
            var workingDir = string.IsNullOrWhiteSpace(WorkingDirectory)
                ? Environment.CurrentDirectory
                : WorkingDirectory;

            LogDiagnostic($"Docker run arguments: {args}");

            var result = ProcessRunner.Run(
                Log,
                "docker",
                args,
                workingDir,
                importance: MessageImportance.High);

            if (!result.Success)
            {
                LogError($"Docker run failed with exit code {result.ExitCode}");
                
                if (!string.IsNullOrWhiteSpace(result.StdOut))
                {
                    LogError(result.StdOut);
                }
                
                if (!string.IsNullOrWhiteSpace(result.StdErr))
                {
                    LogError(result.StdErr);
                }

                return false;
            }

            if (Detached && !string.IsNullOrWhiteSpace(result.StdOut))
            {
                ContainerId = result.StdOut.Trim();
                LogMessage($"Container started with ID: {ContainerId}", MessageImportance.High);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(result.StdOut))
                {
                    LogMessage(result.StdOut, MessageImportance.Normal);
                }
                
                LogMessage("Container completed successfully", MessageImportance.High);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Docker run failed: {ex.Message}");
            LogDiagnostic($"Exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Validates that all required inputs are provided.
    /// </summary>
    private void ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(ImageTag))
        {
            throw new InvalidOperationException("ImageTag is required.");
        }

        if (Interactive && Detached)
        {
            LogWarning("Cannot run in both interactive and detached modes. Detached mode will be disabled.");
            Detached = false;
        }

        LogDiagnostic("Validated Docker run inputs");
    }

    /// <summary>
    /// Validates that Docker is installed and accessible.
    /// </summary>
    private void ValidateDockerInstalled()
    {
        try
        {
            var result = ProcessRunner.Run(
                Log,
                "docker",
                "--version",
                Environment.CurrentDirectory,
                importance: MessageImportance.Low);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    "Docker is not installed or not accessible. Please install Docker and ensure it's in your PATH.");
            }

            LogDiagnostic($"Docker version: {result.StdOut.Trim()}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to verify Docker installation: {ex.Message}. Please ensure Docker is installed and accessible.",
                ex);
        }
    }

    /// <summary>
    /// Builds the Docker run command arguments.
    /// </summary>
    private string BuildDockerArguments()
    {
        var args = new List<string> { "run" };

        if (Detached)
        {
            args.Add("-d");
        }

        if (RemoveOnExit)
        {
            args.Add("--rm");
        }

        if (Interactive)
        {
            args.Add("-it");
        }

        if (!string.IsNullOrWhiteSpace(ContainerName))
        {
            args.Add("--name");
            args.Add($"\"{ContainerName}\"");
        }

        if (!string.IsNullOrWhiteSpace(Network))
        {
            args.Add("--network");
            args.Add($"\"{Network}\"");
        }

        if (!string.IsNullOrWhiteSpace(PortMappings))
        {
            var ports = PortMappings.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var port in ports)
            {
                args.Add("-p");
                args.Add($"\"{port.Trim()}\"");
            }
        }

        if (!string.IsNullOrWhiteSpace(VolumeMounts))
        {
            var volumes = VolumeMounts.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var volume in volumes)
            {
                args.Add("-v");
                args.Add($"\"{volume.Trim()}\"");
            }
        }

        if (!string.IsNullOrWhiteSpace(EnvironmentVariables))
        {
            var envVars = EnvironmentVariables.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var envVar in envVars)
            {
                var parts = envVar.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    args.Add("-e");
                    args.Add($"\"{parts[0].Trim()}={parts[1].Trim()}\"");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(AdditionalOptions))
        {
            args.Add(AdditionalOptions);
        }

        args.Add($"\"{ImageTag}\"");

        if (!string.IsNullOrWhiteSpace(Command))
        {
            args.Add(Command);
        }

        return string.Join(" ", args);
    }
}
