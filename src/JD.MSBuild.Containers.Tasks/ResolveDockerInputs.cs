using Microsoft.Build.Framework;

namespace JD.MSBuild.Containers.Tasks;

/// <summary>
/// MSBuild task that resolves Docker container inputs and determines the project type.
/// </summary>
/// <remarks>
/// <para>
/// This task analyzes the project structure and determines:
/// <list type="bullet">
///   <item><description>Project type (ASP.NET Core, console app, etc.)</description></item>
///   <item><description>Target framework and runtime identifier</description></item>
///   <item><description>Required base Docker images</description></item>
///   <item><description>Entry point and working directory configuration</description></item>
///   <item><description>Port mappings for web applications</description></item>
/// </list>
/// </para>
/// <para>
/// The resolved information is used by downstream tasks to generate appropriate Dockerfiles
/// and configure container settings. This task ensures that container configuration matches
/// the project's requirements and conventions.
/// </para>
/// </remarks>
public sealed class ResolveDockerInputs : DockerTaskBase
{
    /// <summary>
    /// Gets or sets the full path to the project file being containerized.
    /// </summary>
    [Required]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target framework for the project (e.g., "net8.0", "net9.0").
    /// </summary>
    [Required]
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the runtime identifier for the container (e.g., "linux-x64", "linux-arm64").
    /// </summary>
    public string RuntimeIdentifier { get; set; } = "linux-x64";

    /// <summary>
    /// Gets or sets the output type of the project (e.g., "Exe", "Library").
    /// </summary>
    public string OutputType { get; set; } = "Exe";

    /// <summary>
    /// Gets or sets a value indicating whether the project is an ASP.NET Core web application.
    /// </summary>
    public bool IsWebApplication { get; set; }

    /// <summary>
    /// Gets or sets the project assembly name.
    /// </summary>
    [Required]
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output directory for build artifacts.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional package references as semicolon-delimited items.
    /// </summary>
    public string PackageReferences { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved project type (e.g., "aspnet", "console", "worker").
    /// </summary>
    [Output]
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved base Docker image for the runtime.
    /// </summary>
    [Output]
    public string BaseImage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved SDK base image for multi-stage builds.
    /// </summary>
    [Output]
    public string SdkImage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container entry point command.
    /// </summary>
    [Output]
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the working directory inside the container.
    /// </summary>
    [Output]
    public string WorkingDirectory { get; set; } = "/app";

    /// <summary>
    /// Gets or sets the exposed ports (semicolon-delimited for web applications).
    /// </summary>
    [Output]
    public string ExposedPorts { get; set; } = string.Empty;

    /// <summary>
    /// Executes the task to resolve Docker container inputs.
    /// </summary>
    /// <returns>True if the task succeeds; otherwise, false.</returns>
    public override sealed bool Execute()
    {
        try
        {
            LogMessage($"Resolving Docker inputs for project: {Path.GetFileName(ProjectPath)}", 
                MessageImportance.High);

            ValidateInputs();
            DetermineProjectType();
            ResolveBaseImages();
            ConfigureEntryPoint();
            ConfigurePorts();

            LogMessage($"Project Type: {ProjectType}", MessageImportance.High);
            LogMessage($"Base Image: {BaseImage}", MessageImportance.Normal);
            LogMessage($"SDK Image: {SdkImage}", MessageImportance.Normal);
            LogMessage($"Entry Point: {EntryPoint}", MessageImportance.Normal);

            if (!string.IsNullOrEmpty(ExposedPorts))
            {
                LogMessage($"Exposed Ports: {ExposedPorts}", MessageImportance.Normal);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to resolve Docker inputs: {ex.Message}");
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

        if (!File.Exists(ProjectPath))
        {
            throw new FileNotFoundException($"Project file not found: {ProjectPath}");
        }

        if (string.IsNullOrWhiteSpace(TargetFramework))
        {
            throw new InvalidOperationException("TargetFramework is required.");
        }

        if (string.IsNullOrWhiteSpace(AssemblyName))
        {
            throw new InvalidOperationException("AssemblyName is required.");
        }

        LogDiagnostic($"Validated inputs: ProjectPath={ProjectPath}, TargetFramework={TargetFramework}");
    }

    /// <summary>
    /// Determines the project type based on project properties and package references.
    /// </summary>
    private void DetermineProjectType()
    {
        LogDiagnostic($"Determining project type (IsWebApplication={IsWebApplication}, OutputType={OutputType})");

        if (IsWebApplication)
        {
            ProjectType = "aspnet";
            return;
        }

        var packages = PackageReferences.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        var packageSet = new HashSet<string>(packages, StringComparer.OrdinalIgnoreCase);

        if (packageSet.Contains("Microsoft.Extensions.Hosting") ||
            packageSet.Contains("Microsoft.Extensions.Hosting.WindowsServices"))
        {
            ProjectType = "worker";
            return;
        }

        ProjectType = OutputType.Equals("Exe", StringComparison.OrdinalIgnoreCase) ? "console" : "library";
        
        LogDiagnostic($"Determined project type: {ProjectType}");
    }

    /// <summary>
    /// Resolves the appropriate base Docker images for the target framework.
    /// </summary>
    private void ResolveBaseImages()
    {
        var frameworkVersion = ExtractFrameworkVersion(TargetFramework);
        var imageTag = $"{frameworkVersion}";

        SdkImage = $"mcr.microsoft.com/dotnet/sdk:{imageTag}";

        BaseImage = ProjectType switch
        {
            "aspnet" => $"mcr.microsoft.com/dotnet/aspnet:{imageTag}",
            "worker" => $"mcr.microsoft.com/dotnet/runtime:{imageTag}",
            "console" => $"mcr.microsoft.com/dotnet/runtime:{imageTag}",
            _ => $"mcr.microsoft.com/dotnet/runtime:{imageTag}"
        };

        LogDiagnostic($"Resolved images - Base: {BaseImage}, SDK: {SdkImage}");
    }

    /// <summary>
    /// Configures the container entry point based on project type.
    /// </summary>
    private void ConfigureEntryPoint()
    {
        var dllName = $"{AssemblyName}.dll";
        EntryPoint = ProjectType switch
        {
            "aspnet" => $"dotnet {dllName}",
            "worker" => $"dotnet {dllName}",
            "console" => $"dotnet {dllName}",
            _ => $"dotnet {dllName}"
        };

        LogDiagnostic($"Configured entry point: {EntryPoint}");
    }

    /// <summary>
    /// Configures exposed ports for web applications.
    /// </summary>
    private void ConfigurePorts()
    {
        if (ProjectType == "aspnet")
        {
            var frameworkVersion = ExtractFrameworkVersion(TargetFramework);
            
            if (int.TryParse(frameworkVersion.Split('.')[0], out var majorVersion) && majorVersion >= 8)
            {
                ExposedPorts = "8080;8081";
            }
            else
            {
                ExposedPorts = "80;443";
            }

            LogDiagnostic($"Configured ports for ASP.NET Core: {ExposedPorts}");
        }
    }

    /// <summary>
    /// Extracts the numeric version from a target framework moniker (e.g., "net8.0" -> "8.0").
    /// </summary>
    private static string ExtractFrameworkVersion(string targetFramework)
    {
        var tfm = targetFramework.ToLowerInvariant();
        
        if (tfm.StartsWith("net"))
        {
            var version = tfm.Substring(3);
            
            if (version.Length >= 2 && char.IsDigit(version[0]))
            {
                if (version.Contains('.'))
                {
                    return version;
                }
                
                if (version.Length == 2)
                {
                    return $"{version[0]}.{version[1]}";
                }
                
                return $"{version[0]}.0";
            }
        }

        return "8.0";
    }
}
