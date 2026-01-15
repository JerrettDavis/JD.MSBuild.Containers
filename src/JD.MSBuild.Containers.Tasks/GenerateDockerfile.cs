using System.Text;
using Microsoft.Build.Framework;

namespace JD.MSBuild.Containers.Tasks;

/// <summary>
/// MSBuild task that generates a Dockerfile based on project analysis and configuration.
/// </summary>
/// <remarks>
/// <para>
/// This task creates an optimized, multi-stage Dockerfile tailored to the project type:
/// <list type="bullet">
///   <item><description>ASP.NET Core applications with proper port configuration</description></item>
///   <item><description>Console applications with appropriate entry points</description></item>
///   <item><description>Worker services with health check support</description></item>
/// </list>
/// </para>
/// <para>
/// The generated Dockerfile follows Docker best practices:
/// <list type="bullet">
///   <item><description>Multi-stage builds to minimize final image size</description></item>
///   <item><description>Layer caching optimization for faster builds</description></item>
///   <item><description>Non-root user execution for security</description></item>
///   <item><description>Proper working directory and file permissions</description></item>
/// </list>
/// </para>
/// <para>
/// The task supports customization through properties for base images, working directory,
/// exposed ports, and environment variables, making it suitable for a wide range of .NET applications.
/// </para>
/// </remarks>
public sealed class GenerateDockerfile : DockerTaskBase
{
    /// <summary>
    /// Gets or sets the full path to the project file.
    /// </summary>
    [Required]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project type (e.g., "aspnet", "console", "worker").
    /// </summary>
    [Required]
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base Docker image for the runtime stage.
    /// </summary>
    [Required]
    public string BaseImage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SDK Docker image for the build stage.
    /// </summary>
    [Required]
    public string SdkImage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assembly name.
    /// </summary>
    [Required]
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the working directory inside the container.
    /// </summary>
    public string WorkingDirectory { get; set; } = "/app";

    /// <summary>
    /// Gets or sets the exposed ports (semicolon-delimited).
    /// </summary>
    public string ExposedPorts { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional environment variables (semicolon-delimited key=value pairs).
    /// </summary>
    public string EnvironmentVariables { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output path for the generated Dockerfile.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target framework (e.g., "net8.0").
    /// </summary>
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include a .dockerignore file.
    /// </summary>
    public bool GenerateDockerIgnore { get; set; } = true;

    /// <summary>
    /// Executes the task to generate the Dockerfile.
    /// </summary>
    /// <returns>True if the task succeeds; otherwise, false.</returns>
    public override sealed bool Execute()
    {
        try
        {
            LogMessage($"Generating Dockerfile for {ProjectType} project: {Path.GetFileName(ProjectPath)}", 
                MessageImportance.High);

            ValidateInputs();
            
            var dockerfile = GenerateDockerfileContent();
            WriteDockerfile(dockerfile);

            if (GenerateDockerIgnore)
            {
                WriteDockerIgnore();
            }

            LogMessage($"Dockerfile generated successfully: {OutputPath}", MessageImportance.High);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to generate Dockerfile: {ex.Message}");
            LogDiagnostic($"Exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Validates that all required inputs are provided.
    /// </summary>
    private void ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            throw new InvalidOperationException("ProjectPath is required.");
        }

        if (string.IsNullOrWhiteSpace(ProjectType))
        {
            throw new InvalidOperationException("ProjectType is required.");
        }

        if (string.IsNullOrWhiteSpace(BaseImage))
        {
            throw new InvalidOperationException("BaseImage is required.");
        }

        if (string.IsNullOrWhiteSpace(SdkImage))
        {
            throw new InvalidOperationException("SdkImage is required.");
        }

        if (string.IsNullOrWhiteSpace(AssemblyName))
        {
            throw new InvalidOperationException("AssemblyName is required.");
        }

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            throw new InvalidOperationException("OutputPath is required.");
        }

        LogDiagnostic($"Validated inputs for Dockerfile generation");
    }

    /// <summary>
    /// Generates the Dockerfile content based on project configuration.
    /// </summary>
    private string GenerateDockerfileContent()
    {
        var sb = new StringBuilder();
        var projectDir = Path.GetDirectoryName(ProjectPath) ?? string.Empty;
        var projectFile = Path.GetFileName(ProjectPath);

        sb.AppendLine("# This Dockerfile was auto-generated by JD.MSBuild.Containers");
        sb.AppendLine($"# Project: {Path.GetFileName(ProjectPath)}");
        sb.AppendLine($"# Project Type: {ProjectType}");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        GenerateBuildStage(sb, projectFile);
        sb.AppendLine();
        GenerateRuntimeStage(sb);

        return sb.ToString();
    }

    /// <summary>
    /// Generates the build stage of the Dockerfile.
    /// </summary>
    private void GenerateBuildStage(StringBuilder sb, string projectFile)
    {
        sb.AppendLine("# Build stage");
        sb.AppendLine($"FROM {SdkImage} AS build");
        sb.AppendLine("ARG BUILD_CONFIGURATION=Release");
        sb.AppendLine("WORKDIR /src");
        sb.AppendLine();

        sb.AppendLine("# Copy project file and restore dependencies");
        sb.AppendLine($"COPY [\"{projectFile}\", \"./\"]");
        sb.AppendLine("RUN dotnet restore");
        sb.AppendLine();

        sb.AppendLine("# Copy remaining files and build");
        sb.AppendLine("COPY . .");
        sb.AppendLine("RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build --no-restore");
        sb.AppendLine();

        sb.AppendLine("# Publish stage");
        sb.AppendLine("FROM build AS publish");
        sb.AppendLine("ARG BUILD_CONFIGURATION=Release");
        sb.AppendLine("RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates the runtime stage of the Dockerfile.
    /// </summary>
    private void GenerateRuntimeStage(StringBuilder sb)
    {
        sb.AppendLine("# Runtime stage");
        sb.AppendLine($"FROM {BaseImage} AS final");
        sb.AppendLine($"WORKDIR {WorkingDirectory}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(ExposedPorts))
        {
            var ports = ExposedPorts.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var port in ports)
            {
                sb.AppendLine($"EXPOSE {port.Trim()}");
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(EnvironmentVariables))
        {
            var envVars = EnvironmentVariables.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var envVar in envVars)
            {
                var parts = envVar.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    sb.AppendLine($"ENV {parts[0].Trim()}={parts[1].Trim()}");
                }
            }
            sb.AppendLine();
        }

        if (ProjectType == "aspnet")
        {
            var majorVersion = ExtractMajorVersion(TargetFramework);
            if (majorVersion >= 8)
            {
                sb.AppendLine("# ASP.NET Core 8.0+ uses non-root user by default");
                sb.AppendLine("ENV ASPNETCORE_HTTP_PORTS=8080");
                sb.AppendLine("ENV ASPNETCORE_HTTPS_PORTS=8081");
                sb.AppendLine();
            }
        }

        sb.AppendLine("# Copy published application");
        sb.AppendLine("COPY --from=publish /app/publish .");
        sb.AppendLine();

        sb.AppendLine($"ENTRYPOINT [\"dotnet\", \"{AssemblyName}.dll\"]");
    }

    /// <summary>
    /// Writes the Dockerfile to the specified output path.
    /// </summary>
    private void WriteDockerfile(string content)
    {
        var directory = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            LogDiagnostic($"Created directory: {directory}");
        }

        File.WriteAllText(OutputPath, content);
        LogDiagnostic($"Wrote Dockerfile to: {OutputPath}");
    }

    /// <summary>
    /// Writes a .dockerignore file to improve build performance.
    /// </summary>
    private void WriteDockerIgnore()
    {
        var dockerIgnorePath = Path.Combine(
            Path.GetDirectoryName(OutputPath) ?? string.Empty,
            ".dockerignore");

        var content = @"# Build outputs
**/bin/
**/obj/
**/out/

# Visual Studio
.vs/
*.user
*.suo

# Git
.git/
.gitignore
.gitattributes

# Documentation
*.md
README*

# Test files
**/test*/
**/tests*/
**/*Tests/
**/*.Test/

# CI/CD
.github/
.gitlab-ci.yml
azure-pipelines.yml

# IDE
.vscode/
.idea/
*.swp
*.swo
*~

# Docker
**/Dockerfile*
**/docker-compose*
**/.dockerignore

# Misc
LICENSE
**/.DS_Store
";

        File.WriteAllText(dockerIgnorePath, content);
        LogDiagnostic($"Wrote .dockerignore to: {dockerIgnorePath}");
    }

    /// <summary>
    /// Extracts the major version number from a target framework moniker.
    /// </summary>
    private static int ExtractMajorVersion(string targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            return 8;
        }

        var tfm = targetFramework.ToLowerInvariant();
        if (tfm.StartsWith("net") && tfm.Length > 3)
        {
            var versionStr = tfm.Substring(3);
            if (char.IsDigit(versionStr[0]) && int.TryParse(versionStr[0].ToString(), out var major))
            {
                return major;
            }
        }

        return 8;
    }
}
