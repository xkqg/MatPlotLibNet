// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The one reader of the repository tree: a walk never goes into a folder that holds build output, and each list of files is
/// read once per process, however many tests ask for it.</summary>
public sealed class RepositoryTreeTests : IDisposable
{
    private static readonly string[] Skipped = ["node_modules", "bin", "obj", "dist", ".git"];

    private readonly string _root = Path.Combine(Path.GetTempPath(), "mplnet-tree-" + Guid.NewGuid().ToString("N"));

    public RepositoryTreeTests()
    {
        foreach (string file in new[]
        {
            "a/one.cs", "a/bin/Debug/net10.0/built.cs", "a/obj/gen/generated.cs", "a/deep/er/two.cs", "a/deep/er/obj/three.cs",
            "b/node_modules/pkg/lib/index.js", "b/dist/bundle.js", "c/.git/HEAD", "c/ok/four.cs", "five.cs",
        })
        {
            string path = Path.Combine(_root, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file);
        }
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void AWalk_ListsEveryFileOutsideThoseFolders_AndNoOtherFile() =>
        Assert.Equal(["a/deep/er/two.cs", "a/one.cs", "c/ok/four.cs", "five.cs"], WalkedFiles());

    [Fact]
    public void AWalk_EntersEveryFolderOutsideThoseFolders() =>
        Assert.Equal(["a", "a/deep", "a/deep/er", "b", "c", "c/ok"], TreeWalk.Of(_root, Skipped).Folders.Order(StringComparer.Ordinal));

    [Fact]
    public void AWalkOfAFolderThatIsNotThere_SeesNothing()
    {
        var walk = TreeWalk.Of(Path.Combine(_root, "missing"), Skipped);

        Assert.Empty(walk.Files);
        Assert.Empty(walk.Folders);
    }

    [Theory]
    [InlineData("Src")]
    [InlineData("Tst")]
    [InlineData("")]
    public void TheWalksOfTheRepository_NeverEnterAFolderThatHoldsBuildOutput(string top)
    {
        var walk = RepositoryTree.WalkOf(top);

        Assert.Contains(walk.Folders, folder => folder.Length > 0);
        var entered = walk.Folders.Where(folder => folder.Split('/').Any(part => Skipped.Contains(part))).ToList();
        Assert.True(entered.Count == 0, $"the walk of '{top}' entered:\n" + string.Join('\n', entered));
        var listed = walk.Files.Where(file => file.Split('/').Any(part => Skipped.Contains(part))).ToList();
        Assert.True(listed.Count == 0, $"the walk of '{top}' listed:\n" + string.Join('\n', listed));
    }

    [Fact]
    public void EachListOfFiles_IsReadOnce()
    {
        Assert.Same(RepositoryTree.SourceFiles(), RepositoryTree.SourceFiles());
        Assert.Same(RepositoryTree.ComponentFiles(), RepositoryTree.ComponentFiles());
        Assert.Same(RepositoryTree.TestFiles(), RepositoryTree.TestFiles());
        Assert.Same(RepositoryTree.Documents(), RepositoryTree.Documents());
        Assert.Same(RepositoryTree.BuildFiles(), RepositoryTree.BuildFiles());
        Assert.Same(RepositoryTree.CurrentStateDocuments(), RepositoryTree.CurrentStateDocuments());
        Assert.Same(RepositoryTree.Files("Src/MatPlotLibNet.DataFrame", "*.csproj"), RepositoryTree.Files("Src/MatPlotLibNet.DataFrame", "*.csproj"));
    }

    [Fact]
    public void AFolderOfTheRepository_ListsItsFilesByPattern_WithTheTextOfEach()
    {
        var files = RepositoryTree.Files("Src/MatPlotLibNet.DataFrame", "*.cs");

        Assert.NotEmpty(files);
        Assert.All(files, file =>
        {
            Assert.StartsWith("Src/MatPlotLibNet.DataFrame/", file.Path, StringComparison.Ordinal);
            Assert.EndsWith(".cs", file.Path, StringComparison.Ordinal);
            Assert.DoesNotContain("\\", file.Path, StringComparison.Ordinal);
            Assert.Equal(File.ReadAllText(Path.Combine(RepositoryRoot.Folder, file.Path)), file.Text);
        });
        Assert.Contains(files, file => file.Path == "Src/MatPlotLibNet.DataFrame/DataFrameIndicatorExtensions.cs");
    }

    [Fact]
    public void AFolderThatIsNotThere_ListsNoFiles() =>
        Assert.Empty(RepositoryTree.Files("NoSuchFolder/Nested", "*"));

    [Fact]
    public void AFolderName_IsReadAsAWholeName_NotAsThePrefixOfAnother()
    {
        var files = RepositoryTree.Files("Src/MatPlotLibNet", "*.csproj");

        Assert.Equal(["Src/MatPlotLibNet/MatPlotLibNet.csproj"], files.Select(file => file.Path));
    }

    private string[] WalkedFiles() => [.. TreeWalk.Of(_root, Skipped).Files.Order(StringComparer.Ordinal)];
}
