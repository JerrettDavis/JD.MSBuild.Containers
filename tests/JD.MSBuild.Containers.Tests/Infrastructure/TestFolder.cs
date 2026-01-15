namespace JD.MSBuild.Containers.Tests.Infrastructure;

/// <summary>
/// Helper class for creating temporary test folders that are automatically cleaned up.
/// </summary>
/// <remarks>
/// This class provides a convenient way to create isolated temporary directories for tests,
/// ensuring that all test artifacts are properly cleaned up after the test completes,
/// even if the test fails.
/// </remarks>
internal sealed class TestFolder : IDisposable
{
    /// <summary>
    /// Gets the root path of the temporary test folder.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestFolder"/> class.
    /// Creates a unique temporary directory for the test.
    /// </summary>
    public TestFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "containers-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    /// <summary>
    /// Creates a subdirectory within the test folder.
    /// </summary>
    /// <param name="relative">Relative path from the root.</param>
    /// <returns>The full path to the created directory.</returns>
    public string CreateDir(string relative)
    {
        var dir = Path.Combine(Root, relative);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Writes a file within the test folder.
    /// </summary>
    /// <param name="relative">Relative path from the root.</param>
    /// <param name="contents">File contents to write.</param>
    /// <returns>The full path to the created file.</returns>
    public string WriteFile(string relative, string contents)
    {
        var path = Path.Combine(Root, relative);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(path, contents);
        return path;
    }

    /// <summary>
    /// Copies a file into the test folder.
    /// </summary>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="relative">Relative destination path within the test folder.</param>
    /// <returns>The full path to the copied file.</returns>
    public string CopyFile(string sourcePath, string relative)
    {
        var destPath = Path.Combine(Root, relative);
        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.Copy(sourcePath, destPath, overwrite: true);
        return destPath;
    }

    /// <summary>
    /// Gets the full path for a relative path within the test folder.
    /// </summary>
    /// <param name="relative">Relative path from the root.</param>
    /// <returns>The full path.</returns>
    public string GetPath(string relative)
    {
        return Path.Combine(Root, relative);
    }

    /// <summary>
    /// Disposes the test folder by deleting it and all its contents.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch
        {
            // Swallow cleanup failures to avoid masking test failures
        }
    }
}

/// <summary>
/// Provides helper paths for test assets and repository locations.
/// </summary>
internal static class TestPaths
{
    /// <summary>
    /// Gets the repository root directory path.
    /// </summary>
    public static string RepoRoot => _repoRoot.Value;

    /// <summary>
    /// Gets the path to the test assets directory.
    /// </summary>
    public static string TestAssets => Path.Combine(RepoRoot, "tests", "TestAssets");

    /// <summary>
    /// Gets the path to a test asset file or directory.
    /// </summary>
    /// <param name="relative">Relative path within TestAssets.</param>
    /// <returns>The full path to the asset.</returns>
    public static string Asset(string relative) => Path.Combine(TestAssets, relative);

    private static readonly Lazy<string> _repoRoot = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "JD.MSBuild.Containers.sln");
            if (File.Exists(candidate))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Unable to locate repo root (JD.MSBuild.Containers.sln).");
    });
}
