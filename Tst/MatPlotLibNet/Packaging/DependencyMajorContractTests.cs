// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>
/// A major version of a dependency is part of the contract of every package whose public surface exposes that
/// dependency's types, or whose own dependants cannot follow it. Such a major does not ride in a minor release: it
/// is pinned, and the reason is written where the pin stands. This file keeps that decision from rotting in three
/// ways: a pin without its reason, a family of packages that moves only in part, and a pin whose premise has
/// quietly stopped being true.
/// </summary>
public class DependencyMajorContractTests
{
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

    /// <summary>One package reference in one project file, with the facts a pin is judged by.</summary>
    private readonly record struct PackageUse(string Project, string Package, string Version, bool ReasonWritten)
    {
        /// <summary>The major version the reference names, or null for a floating or bracketed version.</summary>
        public int? Major => int.TryParse(Regex.Match(Version, @"^\d+").Value, out var major) ? major : null;
    }

    /// <summary>A major that is held on purpose: the project that holds it, the package, and the major it stays on.</summary>
    private readonly record struct ContractPin(string Project, string Package, int Major);

    private static XElement[] ReferencesIn(XDocument csproj) =>
        [.. csproj.Descendants().Where(n => n.Name.LocalName is "PackageReference" or "PackageVersion")];

    /// <summary>A reference states its reason when a comment heads the run of references it belongs to: walking
    /// backwards from the reference, only other references may lie between it and the comment.</summary>
    private static bool HasReasonAbove(XElement reference)
    {
        foreach (var node in reference.NodesBeforeSelf().Reverse())
        {
            if (node is XComment)
            {
                return true;
            }

            if (node is not XElement { Name.LocalName: "PackageReference" })
            {
                return false;
            }
        }

        return false;
    }

    private static PackageUse[] AllUses() =>
        [.. new[] { "Src", "Tst", "Samples", "Benchmarks" }
            .Select(folder => Path.Combine(Root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(file => ReferencesIn(XDocument.Load(file)).Select(reference => new PackageUse(
                Path.GetRelativePath(Root, file).Replace('\\', '/'),
                (string?)reference.Attribute("Include") ?? (string?)reference.Attribute("Update") ?? string.Empty,
                (string?)reference.Attribute("Version") ?? string.Empty,
                HasReasonAbove(reference))))];

    private static XDocument Project(string relativePath) => XDocument.Load(Path.Combine(Root, relativePath));

    private static string TargetFrameworks(string relativePath)
    {
        var xml = Project(relativePath);
        return xml.Descendants().Where(n => n.Name.LocalName is "TargetFramework" or "TargetFrameworks")
            .Select(n => n.Value.Trim()).First();
    }

    // ---- the playground rides the runtime the repository builds on ---------------------------------------------

    [Fact]
    public void ThePlayground_RunsOnTheNewestRuntimeTheCorePackageBuildsFor()
    {
        // The Pages job installs .NET 8 and 10 and publishes the playground; the core package multi-targets both.
        // A browser sample has no consumer to keep on the older line, so it follows the newest target, and its two
        // WebAssembly packages must name the same major as its target framework or the publish mixes runtimes.
        const string playground = "Samples/MatPlotLibNet.Playground/MatPlotLibNet.Playground.csproj";
        var core = TargetFrameworks("Src/MatPlotLibNet/MatPlotLibNet.csproj").Split(';');
        var newest = core.OrderByDescending(tfm => int.Parse(Regex.Match(tfm, @"\d+").Value)).First();

        Assert.Equal(newest, TargetFrameworks(playground));

        var major = Regex.Match(newest, @"\d+").Value + ".";
        var wasm = AllUses().Where(u => u.Project == playground).ToArray();
        Assert.Equal(2, wasm.Length);
        foreach (var use in wasm)
        {
            Assert.True(use.Version.StartsWith(major, StringComparison.Ordinal),
                $"{use.Package} {use.Version} does not follow the playground's target framework {newest}");
        }
    }

    // ---- two majors of one package in one repository are a decision, and say so -------------------------------

    [Fact]
    public void APackageUsedAtTwoMajors_ExplainsItWhereEachReferenceStands()
    {
        var undocumented = AllUses()
            .Where(u => u.Major is not null)
            .GroupBy(u => u.Package, StringComparer.Ordinal)
            .Where(group => group.Select(u => u.Major).Distinct().Count() > 1)
            .SelectMany(group => group)
            .Where(u => !u.ReasonWritten)
            .Select(u => $"{u.Package} {u.Version} in {u.Project}")
            .ToArray();

        Assert.True(undocumented.Length == 0,
            "These references use a package at a major the repository also uses another way, with no comment "
            + "above them saying why the two coexist: " + string.Join("; ", undocumented));
    }

    // ---- a major held back for the packages' consumers --------------------------------------------------------

    /// <summary>The majors held back for consumers. SkiaSharp 4 ships no net8.0 asset in the WPF and Uno view
    /// packages, and Avalonia.Skia 12 is built on 3.x; HotChocolate's types appear in the GraphQL package's public
    /// constructor; Uno 6 ships no net8.0 asset at all and the Uno control derives from an Uno type.</summary>
    private static ContractPin[] Pins() =>
    [
        new("Src/MatPlotLibNet.Skia/MatPlotLibNet.Skia.csproj", "SkiaSharp", 3),
        new("Src/MatPlotLibNet.Skia/MatPlotLibNet.Skia.csproj", "SkiaSharp.HarfBuzz", 3),
        new("Src/MatPlotLibNet.Avalonia/MatPlotLibNet.Avalonia.csproj", "SkiaSharp", 3),
        new("Src/MatPlotLibNet.Wpf/MatPlotLibNet.Wpf.csproj", "SkiaSharp.Views.WPF", 3),
        new("Src/MatPlotLibNet.Uno/MatPlotLibNet.Uno.csproj", "SkiaSharp.Views.Uno.WinUI", 3),
        new("Src/MatPlotLibNet.Uno/MatPlotLibNet.Uno.csproj", "Uno.WinUI", 5),
        new("Src/MatPlotLibNet.Uno/MatPlotLibNet.Uno.csproj", "Uno.WinUI.Graphics2DSK", 5),
        new("Src/MatPlotLibNet.Mcp/MatPlotLibNet.Mcp.csproj", "SkiaSharp.NativeAssets.Win32", 3),
        new("Src/MatPlotLibNet.Mcp/MatPlotLibNet.Mcp.csproj", "HarfBuzzSharp.NativeAssets.Win32", 8),
        new("Src/MatPlotLibNet.GraphQL/MatPlotLibNet.GraphQL.csproj", "HotChocolate.AspNetCore", 15),
    ];

    [Fact]
    public void AMajorHeldBackForConsumers_IsStillHeldAndStatesItsReason()
    {
        var uses = AllUses();
        var failures = new List<string>();
        foreach (var pin in Pins())
        {
            var use = uses.Where(u => u.Project == pin.Project && u.Package == pin.Package).Cast<PackageUse?>().FirstOrDefault();
            if (use is null)
            {
                failures.Add($"{pin.Package} is no longer referenced by {pin.Project}; drop it from the pin list");
            }
            else if (use.Value.Major != pin.Major)
            {
                failures.Add($"{pin.Package} in {pin.Project} is {use.Value.Version}, not major {pin.Major}");
            }
            else if (!use.Value.ReasonWritten)
            {
                failures.Add($"{pin.Package} in {pin.Project} holds major {pin.Major} with no comment saying why");
            }
        }

        Assert.True(failures.Count == 0, string.Join("; ", failures));
    }

    [Fact]
    public void TheSkiaSharpFamily_MovesAsOne()
    {
        // The managed SkiaSharp packages, the views, the HarfBuzz shaper and the natives are one release line
        // (SkiaSharp 3 goes with HarfBuzzSharp 8, SkiaSharp 4 with HarfBuzzSharp 14). Moving one of them alone
        // restores fine and fails at run time with a missing native or a missing member, so a mixed set is red here.
        var family = AllUses().Where(u => u.Major is not null
            && (u.Package.StartsWith("SkiaSharp", StringComparison.Ordinal)
                || u.Package.StartsWith("HarfBuzzSharp", StringComparison.Ordinal))).ToArray();
        Assert.NotEmpty(family);

        var skia = family.Where(u => u.Package.StartsWith("SkiaSharp", StringComparison.Ordinal)).Select(u => u.Major).Distinct().ToArray();
        var harfBuzz = family.Where(u => u.Package.StartsWith("HarfBuzzSharp", StringComparison.Ordinal)).Select(u => u.Major).Distinct().ToArray();

        Assert.True(skia.Length == 1, "SkiaSharp packages sit on majors " + string.Join(", ", skia));
        Assert.True(harfBuzz.Length == 1, "HarfBuzzSharp packages sit on majors " + string.Join(", ", harfBuzz));

        int expectedHarfBuzz = skia[0] switch { 3 => 8, 4 => 14, _ => -1 };
        Assert.True(harfBuzz[0] == expectedHarfBuzz,
            $"SkiaSharp {skia[0]}.x goes with HarfBuzzSharp {expectedHarfBuzz}.x, but the HarfBuzzSharp packages sit on {harfBuzz[0]}.x");
    }

    [Fact]
    public void TheSkiaSharpMajorPin_HoldsWhileAViewPackageStillTargetsNet8()
    {
        // The reason SkiaSharp stays on 3.x: SkiaSharp.Views.WPF 4.x and SkiaSharp.Views.Uno.WinUI 4.x ship assets
        // for net9.0 and net10.0 only, and the Wpf and Uno packages target net8.0. When neither package targets
        // net8.0 any more the reason is gone and this test says so, instead of the pin living on as habit.
        var holders = new[]
        {
            "Src/MatPlotLibNet.Wpf/MatPlotLibNet.Wpf.csproj",
            "Src/MatPlotLibNet.Uno/MatPlotLibNet.Uno.csproj",
        }.Where(project => TargetFrameworks(project).Contains("net8.0", StringComparison.Ordinal)).ToArray();

        Assert.True(holders.Length > 0,
            "Neither the Wpf nor the Uno package targets net8.0 any more, so SkiaSharp 4 is no longer held back by "
            + "their view packages: re-measure the family on 4.x and lift the pin.");
    }

    [Fact]
    public void TheUnoMajorPin_HoldsWhileTheUnoPackageTargetsNet8()
    {
        // Uno.WinUI and Uno.WinUI.Graphics2DSK 6.x ship assets for net9.0 and net10.0 only; the Uno control derives
        // from an Uno type, so the Uno line is the package's public contract.
        Assert.Contains("net8.0", TargetFrameworks("Src/MatPlotLibNet.Uno/MatPlotLibNet.Uno.csproj"), StringComparison.Ordinal);
    }
}
