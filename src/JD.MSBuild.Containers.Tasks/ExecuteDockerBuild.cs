using JD.MSBuild.Containers.Tasks.Utilities;
using Microsoft.Build.Framework;

namespace JD.MSBuild.Containers.Tasks;

/// <summary>
/// MSBuild task that executes the Docker build command to create a container image.
/// </summary>
/// <remarks>
/// <para>
/// This task wraps the <c>docker build</c> command, providing MSBuild integration and
/// comprehensive error handling. It supports all common Docker build options including:
/// <list type="bullet">
///   <item><description>Custom Dockerfile paths</description></item>
///   <item><description>Build context configuration</description></item>
///   <item><description>Image tagging with multiple tags</description></item>
///   <item><description>Build arguments for parameterization</description></item>
///   <item><description>Target stage selection in multi-stage builds</description></item>
///   <item><description>Build cache management</description></item>
///   <item><description>Platform-specific builds</description></item>
/// </list>
/// </para>
/// <para>
/// The task automatically validates that Docker is installed and accessible before attempting
/// the build. Output from the Docker command is logged with appropriate importance levels,
/// and detailed diagnostic information is available when verbosity is increased.
/// </para>
/// <para>
/// For optimal performance, this task should be used in conjunction with
/// <see cref="ComputeDockerFingerprint"/> to enable incremental builds and avoid unnecessary
/// image rebuilds when inputs haven't changed.
/// </para>
/// </remarks>
public sealed class ExecuteDockerBuild : DockerTaskBase
{
    /// <summary>
    /// Gets or sets the path to the Dockerfile.
    /// </summary>
    [Required]
    public string DockerfilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the build context directory.
    /// </summary>
    [Required]
    public string BuildContext { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image name and tag (e.g., "myapp:latest").
    /// </summary>
    [Required]
    public string ImageTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional image tags (semicolon-delimited).
    /// </summary>
    public string AdditionalTags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets build arguments (semicolon-delimited key=value pairs).
    /// </summary>
    public string BuildArgs { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target build stage in a multi-stage Dockerfile.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target platform (e.g., "linux/amd64", "linux/arm64").
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use build cache.
    /// </summary>
    public bool NoCache { get; set; } = false;

    /// <summary>
    /// Gets or sets additional Docker build options.
    /// </summary>
    public string AdditionalOptions { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to pull the base image before building.
    /// </summary>
    public bool Pull { get; set; } = true;

    /// <summary>
    /// Gets or sets the working directory for the Docker command.
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Executes the Docker build task.
    /// </summary>
    /// <returns>True if the task succeeds; otherwise, false.</returns>
    public override sealed bool Execute()
    {
        try
        {
            LogMessage($"Building Docker image: {ImageTag}", MessageImportance.High);

            ValidateInputs();
            ValidateDockerInstalled();

            var args = BuildDockerArguments();
            var workingDir = string.IsNullOrWhiteSpace(WorkingDirectory) 
                ? BuildContext 
                : WorkingDirectory;

            LogMessage($"Docker build context: {BuildContext}", MessageImportance.Normal);
            LogDiagnostic($"Docker arguments: {args}");

            ProcessRunner.RunBuildOrThrow(
                Log,
                "docker",
                args,
                workingDir,
                errorMessage: $"Docker build failed for image: {ImageTag}",
                importance: MessageImportance.High);

            LogMessage($"Successfully built Docker image: {ImageTag}", MessageImportance.High);
            
            if (!string.IsNullOrWhiteSpace(AdditionalTags))
            {
                TagAdditionalImages();
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Docker build failed: {ex.Message}");
            LogDiagnostic($"Exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Validates that all required inputs are provided.
    /// </summary>
    private void ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(DockerfilePath))
        {
            throw new InvalidOperationException("DockerfilePath is required.");
        }

        if (!File.Exists(DockerfilePath))
        {
            throw new FileNotFoundException($"Dockerfile not found: {DockerfilePath}");
        }

        if (string.IsNullOrWhiteSpace(BuildContext))
        {
            throw new InvalidOperationException("BuildContext is required.");
        }

        if (!Directory.Exists(BuildContext))
        {
            throw new DirectoryNotFoundException($"Build context directory not found: {BuildContext}");
        }

        if (string.IsNullOrWhiteSpace(ImageTag))
        {
            throw new InvalidOperationException("ImageTag is required.");
        }

        LogDiagnostic("Validated Docker build inputs");
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
    /// Builds the Docker command arguments.
    /// </summary>
    private string BuildDockerArguments()
    {
        var args = new List<string> { "build" };

        args.Add("-f");
        args.Add($"\"{DockerfilePath}\"");

        args.Add("-t");
        args.Add($"\"{ImageTag}\"");

        if (!string.IsNullOrWhiteSpace(Target))
        {
            args.Add("--target");
            args.Add($"\"{Target}\"");
        }

        if (!string.IsNullOrWhiteSpace(Platform))
        {
            args.Add("--platform");
            args.Add($"\"{Platform}\"");
        }

        if (NoCache)
        {
            args.Add("--no-cache");
        }

        if (Pull)
        {
            args.Add("--pull");
        }

        if (!string.IsNullOrWhiteSpace(BuildArgs))
        {
            var buildArgPairs = BuildArgs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in buildArgPairs)
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    args.Add("--build-arg");
                    args.Add($"\"{parts[0].Trim()}={parts[1].Trim()}\"");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(AdditionalOptions))
        {
            args.Add(AdditionalOptions);
        }

        args.Add($"\"{BuildContext}\"");

        return string.Join(" ", args);
    }

    /// <summary>
    /// Tags the built image with additional tags if specified.
    /// </summary>
    private void TagAdditionalImages()
    {
        var tags = AdditionalTags.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var tag in tags)
        {
            var trimmedTag = tag.Trim();
            if (string.IsNullOrWhiteSpace(trimmedTag))
            {
                continue;
            }

            try
            {
                LogMessage($"Tagging image with additional tag: {trimmedTag}", MessageImportance.Normal);

                var args = $"tag \"{ImageTag}\" \"{trimmedTag}\"";
                
                ProcessRunner.RunOrThrow(
                    Log,
                    "docker",
                    args,
                    BuildContext,
                    importance: MessageImportance.Normal);

                LogMessage($"Successfully tagged: {trimmedTag}", MessageImportance.Normal);
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to tag image with {trimmedTag}: {ex.Message}");
            }
        }
    }
}
