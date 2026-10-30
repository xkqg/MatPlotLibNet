// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Models;

/// <summary><see cref="SeriesCountContractTests"/> pins the number the assembly ships; this holds the repository's
/// documents to it. The count stands in the README, the architecture notes, the cookbook, the package readmes and the
/// roadmap, and the only thing that kept those in step was a red message that named the documents to look at.
/// The changelog is history and says what each release counted then, so it is not read.</summary>
public class SeriesCountInDocumentsContractTests
{
    private static readonly string Root = RepoRoot();

    private static readonly string[] SkippedFolders =
        ["node_modules", "bin", "obj", "_site", ".git", ".path-manifests", "BenchmarkDotNet.Artifacts"];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static IEnumerable<string> Documents() =>
        Directory.EnumerateFiles(Root, "*.md", SearchOption.AllDirectories)
            .Where(f => !string.Equals(Path.GetFileName(f), "CHANGELOG.md", StringComparison.Ordinal))
            .Where(f => !Path.GetRelativePath(Root, f).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => SkippedFolders.Contains(part, StringComparer.Ordinal)));

    [Fact]
    public void EveryDocument_StatesTheNumberOfSeriesTheAssemblyShips()
    {
        int shipped = SeriesCountContractTests.ConcreteSeries().Length;

        var stale = Documents()
            .SelectMany(f => SeriesCountPhrases.In(File.ReadAllText(f))
                .Where(p => p.Count != shipped)
                .Select(p => $"{Path.GetRelativePath(Root, f).Replace('\\', '/')}: \"{p.Phrase}\""))
            .ToArray();

        Assert.True(stale.Length == 0,
            $"the assembly ships {shipped} series types; these documents say otherwise:\n" + string.Join("\n", stale));
    }

    [Fact]
    public void TheDocuments_StateTheSeriesCountAtAll()
    {
        // A reader that finds nothing would pass the test above for any count; the README states it, and it is read.
        Assert.Contains(Documents().SelectMany(f => SeriesCountPhrases.In(File.ReadAllText(f))),
            p => p.Count == SeriesCountContractTests.ConcreteSeries().Length);
    }
}
