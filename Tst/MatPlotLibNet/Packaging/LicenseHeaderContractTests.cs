// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The repository is MIT licensed and says so in its LICENSE file. A source file that names another licence
/// in its own header contradicts it, and the header is what a reader of that one file sees: twenty-nine files of the
/// Blazor components and the three npm bindings once carried a GPL line, copied from a template.</summary>
public class LicenseHeaderContractTests
{
    private static readonly string Root = RepoRoot();

    private static readonly string[] Extensions = [".cs", ".razor", ".ts", ".tsx", ".vue", ".js"];

    private static readonly string[] SkippedFolders = ["node_modules", "bin", "obj", "dist", ".git"];

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

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(Path.Combine(Root, "Src"), "*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => SkippedFolders.Contains(part, StringComparer.Ordinal)));

    [Fact]
    public void EverySourceFileThatNamesALicence_NamesTheMitLicence()
    {
        var wrong = new List<string>();

        foreach (var file in SourceFiles())
        {
            var header = File.ReadLines(file).Take(5).FirstOrDefault(l => l.Contains("Licensed under", StringComparison.Ordinal));
            if (header is not null && !header.Contains("MIT License", StringComparison.Ordinal))
            {
                wrong.Add($"{Path.GetRelativePath(Root, file).Replace('\\', '/')}: {header.Trim()}");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} source file(s) name a licence other than the repository's MIT licence:\n" + string.Join("\n", wrong));
    }
}
