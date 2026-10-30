// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The managed SkiaSharp and HarfBuzzSharp packages carry no native binary of their own: each platform's library
/// comes from a per-platform assets package, and a program that renders must bring all three or it dies on the platform it
/// lacks ("Unable to load shared library 'libSkiaSharp'", which is what the picture-comparison suite did on Linux). The six
/// references are written once, in <c>SkiaNativeAssets.props</c>, and every host that renders (the MCP tool, the Skia test
/// project, the picture-comparison suite) imports that file, so the set and its versions cannot drift apart.</summary>
public class SkiaNativeAssetsContractTests
{
    private const string PropsFile = "SkiaNativeAssets.props";

    private static readonly string Root = RepoRoot();

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

    /// <summary>The hosts that load libSkiaSharp and libHarfBuzzSharp themselves and so have to bring the natives.</summary>
    public static TheoryData<string> Hosts { get; } = new(
        "Src/MatPlotLibNet.Mcp/MatPlotLibNet.Mcp.csproj",
        "Tst/MatPlotLibNet.Skia/MatPlotLibNet.Skia.Tests.csproj",
        "Tst/MatPlotLibNet.Fidelity/MatPlotLibNet.Fidelity.Tests.csproj");

    private static IEnumerable<string> ProjectFiles() =>
        Directory.EnumerateFiles(Root, "*.csproj", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !file.Contains("node_modules", StringComparison.Ordinal));

    [Fact]
    public void TheSharedProps_DeclareThreeSkiaAndThreeHarfBuzzAssets()
    {
        string path = Path.Combine(Root, PropsFile);
        Assert.True(File.Exists(path), $"{PropsFile} is missing from the repository root.");

        var names = Regex.Matches(File.ReadAllText(path), @"<PackageReference Include=""([^""]+NativeAssets[^""]+)""")
            .Select(match => match.Groups[1].Value).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(
        [
            "HarfBuzzSharp.NativeAssets.Linux", "HarfBuzzSharp.NativeAssets.Win32", "HarfBuzzSharp.NativeAssets.macOS",
            "SkiaSharp.NativeAssets.Linux.NoDependencies", "SkiaSharp.NativeAssets.Win32", "SkiaSharp.NativeAssets.macOS",
        ], names);
    }

    [Fact]
    public void NoProjectDeclaresANativeAssetsPackageItself()
    {
        var offenders = ProjectFiles()
            .Where(file => Regex.IsMatch(File.ReadAllText(file), @"<PackageReference Include=""[^""]*NativeAssets[^""]*"""))
            .Select(file => Path.GetRelativePath(Root, file).Replace('\\', '/'))
            .ToArray();

        Assert.True(offenders.Length == 0, $"These projects list native assets themselves instead of importing {PropsFile}: {string.Join(", ", offenders)}.");
    }

    [Theory]
    [MemberData(nameof(Hosts))]
    public void EveryHostThatLoadsSkiaNatively_ImportsTheSharedProps(string project)
    {
        string text = File.ReadAllText(Path.Combine(Root, project));

        Assert.Matches(@"<Import Project=""[^""]*" + Regex.Escape(PropsFile) + @"""", text);
    }
}
