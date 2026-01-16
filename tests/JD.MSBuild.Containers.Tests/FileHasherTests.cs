using JD.MSBuild.Containers.Tasks.Utilities;
using JD.MSBuild.Containers.Tests.Infrastructure;
using TinyBDD;
using Xunit.Abstractions;

namespace JD.MSBuild.Containers.Tests;

/// <summary>
/// Tests for the FileHasher utility class.
/// </summary>
[Feature("FileHasher: fast XxHash64-based file and string hashing for fingerprinting")]
public sealed class FileHasherTests(ITestOutputHelper output)
{
    [Scenario("HashFile produces consistent output")]
    [Fact]
    public void HashFileShouldBeConsistent()
    {
        using var folder = new TestFolder();
        var path = folder.WriteFile("test.txt", "Hello, World!");

        var hash1 = FileHasher.HashFile(path);
        var hash2 = FileHasher.HashFile(path);

        Assert.Equal(hash1, hash2);
        Assert.Equal(16, hash1.Length);
    }

    [Scenario("HashFile detects content changes")]
    [Fact]
    public void HashFileShouldDetectChanges()
    {
        using var folder = new TestFolder();
        var path = folder.WriteFile("test.txt", "Original content");

        var hash1 = FileHasher.HashFile(path);
        File.WriteAllText(path, "Modified content");
        var hash2 = FileHasher.HashFile(path);

        Assert.NotEqual(hash1, hash2);
    }

    [Scenario("HashString produces consistent output")]
    [Fact]
    public void HashStringShouldBeConsistent()
    {
        var hash1 = FileHasher.HashString("Test content");
        var hash2 = FileHasher.HashString("Test content");

        Assert.Equal(hash1, hash2);
        Assert.Equal(16, hash1.Length);
    }

    [Scenario("HashString detects differences")]
    [Fact]
    public void HashStringShouldDetectDifferences()
    {
        var hash1 = FileHasher.HashString("First string");
        var hash2 = FileHasher.HashString("Second string");

        Assert.NotEqual(hash1, hash2);
    }

    [Scenario("HashString handles empty string")]
    [Fact]
    public void HashEmptyStringShouldSucceed()
    {
        var hash = FileHasher.HashString("");

        Assert.Equal(16, hash.Length);
    }

    [Scenario("HashFiles combines multiple files")]
    [Fact]
    public void HashFilesShouldCombineMultipleFiles()
    {
        using var folder = new TestFolder();
        folder.WriteFile("file1.txt", "Content 1");
        folder.WriteFile("file2.txt", "Content 2");
        folder.WriteFile("file3.txt", "Content 3");

        var paths = new[]
        {
            folder.GetPath("file1.txt"),
            folder.GetPath("file2.txt"),
            folder.GetPath("file3.txt")
        };

        var hash = FileHasher.HashFiles(paths);

        Assert.Equal(16, hash.Length);
    }

    [Scenario("HashFiles order matters")]
    [Fact]
    public void HashFilesOrderMatters()
    {
        using var folder = new TestFolder();
        folder.WriteFile("a.txt", "AAA");
        folder.WriteFile("b.txt", "BBB");

        var hash1 = FileHasher.HashFiles(new[]
        {
            folder.GetPath("a.txt"),
            folder.GetPath("b.txt")
        });

        var hash2 = FileHasher.HashFiles(new[]
        {
            folder.GetPath("b.txt"),
            folder.GetPath("a.txt")
        });

        Assert.NotEqual(hash1, hash2);
    }

    [Scenario("HashDirectory hashes all files")]
    [Fact]
    public void HashDirectoryShouldHashAllFiles()
    {
        using var folder = new TestFolder();
        var subDir = folder.CreateDir("src");
        folder.WriteFile("src/file1.cs", "class A { }");
        folder.WriteFile("src/file2.cs", "class B { }");

        var hash = FileHasher.HashDirectory(subDir);

        Assert.Equal(16, hash.Length);
    }

    [Scenario("HashDirectory is consistent")]
    [Fact]
    public void HashDirectoryShouldBeConsistent()
    {
        using var folder = new TestFolder();
        folder.WriteFile("test1.cs", "Test 1");
        folder.WriteFile("test2.cs", "Test 2");

        var hash1 = FileHasher.HashDirectory(folder.Root, "*.cs");
        var hash2 = FileHasher.HashDirectory(folder.Root, "*.cs");

        Assert.Equal(hash1, hash2);
    }

    [Scenario("HashDirectory filters by pattern")]
    [Fact]
    public void HashDirectoryShouldFilterByPattern()
    {
        using var folder = new TestFolder();
        folder.WriteFile("code.cs", "C# code");
        folder.WriteFile("data.txt", "Text data");

        var hash1 = FileHasher.HashDirectory(folder.Root, "*.cs");
        var hash2 = FileHasher.HashDirectory(folder.Root, "*.txt");

        Assert.NotEqual(hash1, hash2);
    }

    [Scenario("HashFile throws for nonexistent file")]
    [Fact]
    public void HashNonexistentFileShouldThrow()
    {
        using var folder = new TestFolder();

        Assert.Throws<FileNotFoundException>(() =>
            FileHasher.HashFile(folder.GetPath("nonexistent.txt")));
    }

    [Scenario("HashDirectory throws for nonexistent directory")]
    [Fact]
    public void HashNonexistentDirectoryShouldThrow()
    {
        using var folder = new TestFolder();

        Assert.Throws<DirectoryNotFoundException>(() =>
            FileHasher.HashDirectory(folder.GetPath("nonexistent")));
    }
}
