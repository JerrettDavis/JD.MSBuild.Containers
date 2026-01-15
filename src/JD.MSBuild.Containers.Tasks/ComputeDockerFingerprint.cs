using System.Text;
using JD.MSBuild.Containers.Tasks.Utilities;
using Microsoft.Build.Framework;

namespace JD.MSBuild.Containers.Tasks;

/// <summary>
/// MSBuild task that computes a deterministic fingerprint for Docker inputs to enable incremental builds.
/// </summary>
/// <remarks>
/// <para>
/// This task computes a fingerprint that uniquely identifies the current state of all Docker-related inputs.
/// When any input changes, the fingerprint changes, triggering a rebuild. When inputs are unchanged,
/// the build can be skipped, significantly improving build performance.
/// </para>
/// <para>
/// The fingerprint is derived from multiple sources:
/// <list type="bullet">
///   <item><description>Project file content</description></item>
///   <item><description>Dockerfile content (if present)</description></item>
///   <item><description>All project source files</description></item>
///   <item><description>Package references and versions</description></item>
///   <item><description>Target framework and configuration</description></item>
///   <item><description>Base image specifications</description></item>
///   <item><description>Environment variable configurations</description></item>
/// </list>
/// </para>
/// <para>
/// The computed fingerprint uses XxHash64, a fast non-cryptographic hash algorithm that provides
/// excellent distribution and collision resistance while being significantly faster than cryptographic
/// hashes like SHA-256.
/// </para>
/// <para>
/// The fingerprint is stored in a file specified by <see cref="FingerprintFile"/>. On subsequent builds,
/// the task compares the new fingerprint with the stored value. If they match, <see cref="HasChanged"/>
/// is set to false, allowing the build system to skip expensive Docker operations.
/// </para>
/// </remarks>
public sealed class ComputeDockerFingerprint : DockerTaskBase
{
    /// <summary>
    /// Gets or sets the full path to the project file.
    /// </summary>
    [Required]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Dockerfile path (if it exists).
    /// </summary>
    public string DockerfilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project directory containing source files.
    /// </summary>
    [Required]
    public string ProjectDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target framework.
    /// </summary>
    [Required]
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the build configuration (e.g., "Debug", "Release").
    /// </summary>
    public string Configuration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the base Docker image.
    /// </summary>
    public string BaseImage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SDK Docker image.
    /// </summary>
    public string SdkImage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets package references (semicolon-delimited).
    /// </summary>
    public string PackageReferences { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets environment variables (semicolon-delimited key=value pairs).
    /// </summary>
    public string EnvironmentVariables { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the file that stores the computed fingerprint.
    /// </summary>
    [Required]
    public string FingerprintFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to include generated files in the fingerprint.
    /// </summary>
    public bool IncludeGeneratedFiles { get; set; } = false;

    /// <summary>
    /// Gets or sets the computed fingerprint value.
    /// </summary>
    [Output]
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the fingerprint has changed since the last build.
    /// </summary>
    [Output]
    public bool HasChanged { get; set; }

    /// <summary>
    /// Executes the task to compute the Docker fingerprint.
    /// </summary>
    /// <returns>True if the task succeeds; otherwise, false.</returns>
    public override sealed bool Execute()
    {
        try
        {
            LogMessage("Computing Docker fingerprint for incremental build...", MessageImportance.High);

            ValidateInputs();
            
            var manifest = BuildFingerprintManifest();
            Fingerprint = FileHasher.HashString(manifest);

            LogDiagnostic($"Computed fingerprint: {Fingerprint}");
            LogDiagnostic($"Fingerprint manifest length: {manifest.Length} characters");

            HasChanged = CheckIfChanged();

            if (HasChanged)
            {
                WriteFingerprintFile();
                LogMessage("Docker inputs have changed. Rebuild required.", MessageImportance.High);
            }
            else
            {
                LogMessage("Docker inputs unchanged. Build can be skipped.", MessageImportance.High);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to compute Docker fingerprint: {ex.Message}");
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

        if (string.IsNullOrWhiteSpace(ProjectDirectory))
        {
            throw new InvalidOperationException("ProjectDirectory is required.");
        }

        if (!Directory.Exists(ProjectDirectory))
        {
            throw new DirectoryNotFoundException($"Project directory not found: {ProjectDirectory}");
        }

        if (string.IsNullOrWhiteSpace(FingerprintFile))
        {
            throw new InvalidOperationException("FingerprintFile is required.");
        }

        LogDiagnostic($"Validated inputs for fingerprint computation");
    }

    /// <summary>
    /// Builds a manifest string containing all inputs that affect the Docker build.
    /// </summary>
    private string BuildFingerprintManifest()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# JD.MSBuild.Containers Docker Fingerprint Manifest");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:O}");
        sb.AppendLine();

        AppendFileHash(sb, "ProjectFile", ProjectPath);

        if (!string.IsNullOrWhiteSpace(DockerfilePath) && File.Exists(DockerfilePath))
        {
            AppendFileHash(sb, "Dockerfile", DockerfilePath);
        }

        AppendValue(sb, "TargetFramework", TargetFramework);
        AppendValue(sb, "Configuration", Configuration);
        AppendValue(sb, "BaseImage", BaseImage);
        AppendValue(sb, "SdkImage", SdkImage);
        AppendValue(sb, "PackageReferences", PackageReferences);
        AppendValue(sb, "EnvironmentVariables", EnvironmentVariables);

        AppendSourceFilesHash(sb);

        LogDiagnostic("Fingerprint manifest built successfully");
        return sb.ToString();
    }

    /// <summary>
    /// Appends a file hash to the manifest.
    /// </summary>
    private void AppendFileHash(StringBuilder sb, string label, string filePath)
    {
        try
        {
            var hash = FileHasher.HashFile(filePath);
            sb.AppendLine($"{label}={hash}");
            LogDiagnostic($"Hashed {label}: {filePath} -> {hash}");
        }
        catch (Exception ex)
        {
            LogWarning($"Failed to hash {label} at {filePath}: {ex.Message}");
            sb.AppendLine($"{label}=<error>");
        }
    }

    /// <summary>
    /// Appends a configuration value to the manifest.
    /// </summary>
    private void AppendValue(StringBuilder sb, string label, string value)
    {
        var hash = FileHasher.HashString(value ?? string.Empty);
        sb.AppendLine($"{label}={hash}");
        LogDiagnostic($"Hashed {label} -> {hash}");
    }

    /// <summary>
    /// Appends a combined hash of all source files to the manifest.
    /// </summary>
    private void AppendSourceFilesHash(StringBuilder sb)
    {
        try
        {
            var sourceFiles = Directory.GetFiles(ProjectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(f => ShouldIncludeFile(f))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sourceFiles.Count > 0)
            {
                var hash = FileHasher.HashFiles(sourceFiles);
                sb.AppendLine($"SourceFiles={hash}");
                LogDiagnostic($"Hashed {sourceFiles.Count} source files -> {hash}");
            }
            else
            {
                sb.AppendLine("SourceFiles=<none>");
                LogDiagnostic("No source files found to hash");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Failed to hash source files: {ex.Message}");
            sb.AppendLine("SourceFiles=<error>");
        }
    }

    /// <summary>
    /// Determines whether a file should be included in the fingerprint.
    /// </summary>
    private bool ShouldIncludeFile(string filePath)
    {
        var relativePath = Path.GetRelativePath(ProjectDirectory, filePath);

        if (relativePath.Contains("obj", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Contains("bin", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!IncludeGeneratedFiles && IsGeneratedFile(filePath))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a file is likely to be generated code.
    /// </summary>
    private static bool IsGeneratedFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        
        return fileName.Contains(".g.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.Contains(".designer.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.Contains("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.Contains("TemporaryGeneratedFile", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the fingerprint has changed by comparing with the stored value.
    /// </summary>
    private bool CheckIfChanged()
    {
        if (!File.Exists(FingerprintFile))
        {
            LogDiagnostic("Fingerprint file does not exist. This is a new build.");
            return true;
        }

        try
        {
            var storedFingerprint = File.ReadAllText(FingerprintFile).Trim();
            
            if (storedFingerprint == Fingerprint)
            {
                LogDiagnostic($"Fingerprint matches stored value: {storedFingerprint}");
                return false;
            }

            LogDiagnostic($"Fingerprint changed. Old: {storedFingerprint}, New: {Fingerprint}");
            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"Failed to read fingerprint file: {ex.Message}. Assuming changed.");
            return true;
        }
    }

    /// <summary>
    /// Writes the computed fingerprint to the fingerprint file.
    /// </summary>
    private void WriteFingerprintFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(FingerprintFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                LogDiagnostic($"Created directory for fingerprint file: {directory}");
            }

            File.WriteAllText(FingerprintFile, Fingerprint);
            LogDiagnostic($"Wrote fingerprint to: {FingerprintFile}");
        }
        catch (Exception ex)
        {
            LogWarning($"Failed to write fingerprint file: {ex.Message}");
        }
    }
}
