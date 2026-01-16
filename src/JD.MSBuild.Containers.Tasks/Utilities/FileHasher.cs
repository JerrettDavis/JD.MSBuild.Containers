using System.IO.Hashing;
using System.Text;

namespace JD.MSBuild.Containers.Tasks.Utilities;

/// <summary>
/// Provides fast, non-cryptographic hashing utilities using XxHash64 for file fingerprinting.
/// </summary>
/// <remarks>
/// <para>
/// This class uses XxHash64, a high-speed non-cryptographic hash algorithm, which is ideal
/// for checksums, fingerprinting, and detecting file changes in build scenarios.
/// </para>
/// <para>
/// XxHash64 is significantly faster than cryptographic hashes like SHA-256 while still
/// providing excellent distribution and low collision rates for practical use cases.
/// </para>
/// </remarks>
internal static class FileHasher
{
    /// <summary>
    /// Computes the XxHash64 hash of a file.
    /// </summary>
    /// <param name="path">The full path to the file to hash.</param>
    /// <returns>A lowercase hexadecimal string representation of the 64-bit hash.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
    /// <remarks>
    /// The returned hash is a 16-character hexadecimal string representing the 64-bit hash value.
    /// This format is suitable for storage in fingerprint files and comparison operations.
    /// </remarks>
    public static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = new XxHash64();
        hash.Append(stream);
        return hash.GetCurrentHashAsUInt64().ToString("x16");
    }

    /// <summary>
    /// Computes the XxHash64 hash of a byte array.
    /// </summary>
    /// <param name="bytes">The byte array to hash.</param>
    /// <returns>A lowercase hexadecimal string representation of the 64-bit hash.</returns>
    /// <remarks>
    /// This method is useful for hashing in-memory data such as configuration content
    /// or computed values without writing them to disk first.
    /// </remarks>
    public static string HashBytes(byte[] bytes)
    {
        return XxHash64.HashToUInt64(bytes).ToString("x16");
    }

    /// <summary>
    /// Computes the XxHash64 hash of a string using UTF-8 encoding.
    /// </summary>
    /// <param name="content">The string content to hash.</param>
    /// <returns>A lowercase hexadecimal string representation of the 64-bit hash.</returns>
    /// <remarks>
    /// The string is converted to bytes using UTF-8 encoding before hashing.
    /// This ensures consistent results across different platforms and cultures.
    /// </remarks>
    public static string HashString(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return HashBytes(bytes);
    }

    /// <summary>
    /// Computes the XxHash64 hash of multiple files combined.
    /// </summary>
    /// <param name="paths">An enumerable collection of file paths to hash.</param>
    /// <returns>A lowercase hexadecimal string representation of the combined 64-bit hash.</returns>
    /// <exception cref="FileNotFoundException">Thrown when any specified file does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading any file.</exception>
    /// <remarks>
    /// <para>
    /// This method hashes multiple files in order and returns a single hash value representing
    /// the combined content. The order of files matters - different orders will produce different hashes.
    /// </para>
    /// <para>
    /// This is useful for creating fingerprints of directories or collections of related files,
    /// such as all template files or all project source files.
    /// </para>
    /// </remarks>
    public static string HashFiles(IEnumerable<string> paths)
    {
        var hash = new XxHash64();

        foreach (var path in paths)
        {
            using var stream = File.OpenRead(path);
            hash.Append(stream);
        }

        return hash.GetCurrentHashAsUInt64().ToString("x16");
    }

    /// <summary>
    /// Computes the XxHash64 hash of a directory's contents recursively.
    /// </summary>
    /// <param name="directoryPath">The directory path to hash.</param>
    /// <param name="pattern">Optional file pattern to filter files (e.g., "*.cs"). Defaults to all files.</param>
    /// <returns>A lowercase hexadecimal string representation of the combined 64-bit hash.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    /// <remarks>
    /// <para>
    /// This method recursively hashes all files in a directory and its subdirectories.
    /// Files are processed in alphabetical order to ensure deterministic results.
    /// </para>
    /// <para>
    /// This is useful for detecting changes to template directories, configuration folders,
    /// or any collection of files that should trigger a rebuild when modified.
    /// </para>
    /// </remarks>
    public static string HashDirectory(string directoryPath, string pattern = "*")
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            return HashString(string.Empty);
        }

        return HashFiles(files);
    }
}
