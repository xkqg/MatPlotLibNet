// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Tests.Rendering;
using static MatPlotLibNet.Tests.Packaging.ReleaseFiles;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>
/// The version a release carries is said in many places (every project, the baseline the pack compares with, the MCP manifest, the documents that name
/// it, the frozen packages) and the places agree: one number, the baseline the release just before it, a property written once.
/// </summary>
public class ReleaseVersionContractTests
{
    /// <summary>The version the living packages carry — the one a release ships.</summary>
    private static string LiveVersion() => VersionOf(LivePackages()[0].Xml);

    [Fact]
    public void AFrozenPackage_KeepsTheVersionItWasFrozenAt()
    {
        // Frozen means the number stops moving. If it ever equals the live version again, either somebody bumped
        // it along with the rest out of habit, or it was never really frozen — and both read as "all fifteen agree"
        // to every other test in this file.
        var moved = PackableProjects()
            .Where(p => Frozen.ContainsKey(p.Path))
            .Where(p => string.Equals(VersionOf(p.Xml), LiveVersion(), StringComparison.Ordinal))
            .Select(p => p.Path)
            .ToArray();

        Assert.True(moved.Length == 0,
            $"These packages are declared frozen and carry the live version {LiveVersion()} anyway: "
            + string.Join(", ", moved));
    }

    [Fact]
    public void AFrozenPackage_IsNotComparedWithAReleaseItNeverHad()
    {
        // The pack baseline is the previous release of the LIVE line. A frozen package stopped at an older number
        // and never had that release, so restore would look on nuget.org for a version of it that does not exist
        // and fail every build that touches the package. It says so in its own project file, where the next
        // person to open it will read why.
        var compared = PackableProjects()
            .Where(p => Frozen.ContainsKey(p.Path))
            .Where(p => !p.Xml.Descendants("EnablePackageValidation").Any(n => n.Value.Trim() == "false"))
            .Select(p => p.Path)
            .ToArray();

        Assert.True(compared.Length == 0,
            "These packages are declared frozen and are still packed against the live line's previous release; "
            + "set <EnablePackageValidation>false</EnablePackageValidation> in them: " + string.Join(", ", compared));
    }

    // ---- the version, in every place it is written ----------------------------------------------------------

    [Fact]
    public void EveryPackableProject_CarriesTheSameVersion()
    {
        var byVersion = LivePackages()
            .GroupBy(p => VersionOf(p.Xml), StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ToArray();

        Assert.True(byVersion.Length == 1,
            "A release bumps every package together, or the odd one out ships an old number under a new tag. Found: "
            + string.Join(" | ", byVersion.Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.Path))}")));
    }

    // ---- a pack is compared with the release before it ---------------------------------------------------------

    /// <summary>Every value the shared build file gives one MSBuild property, in document order. More than one
    /// element means the setting lives in two places, which is how a baseline drifts.</summary>
    private static string[] SharedBuildProperty(string name) =>
        [.. XDocument.Load(Path.Combine(Root, "Directory.Build.props")).Descendants(name).Select(n => n.Value.Trim())];

    /// <summary>The switches that decide whether, and against what, a package is validated. A live package may
    /// not set any of them: the shared build file is the one place that does.</summary>
    private static readonly string[] PackageValidationSwitches =
    [
        "EnablePackageValidation",
        "PackageValidationBaselineVersion",
        "PackageValidationBaselinePath",
        "DisablePackageBaselineValidation",
    ];

    /// <summary>A .NET tool is a command line, not a library: the SDK switches package validation off for it on
    /// its own, because there is no API surface a consumer compiles against.</summary>
    private static bool IsToolPackage(XDocument csproj) =>
        csproj.Descendants("PackAsTool").Any(n => n.Value.Trim() == "true");

    private static System.Version ValidationBaseline()
    {
        var values = SharedBuildProperty("PackageValidationBaselineVersion");
        Assert.True(values.Length == 1 && System.Version.TryParse(values[0], out _),
            "Directory.Build.props must name the release every pack is compared with, once, as a version number "
            + $"(<PackageValidationBaselineVersion>); it names it {values.Length} time(s): {string.Join(", ", values)}");
        return System.Version.Parse(values[0]);
    }

    [Fact]
    public void EveryLivePackage_IsPackedAgainstThePreviousRelease()
    {
        // Until this was on, a pack compared nothing: a public member could be renamed, removed or narrowed in a
        // minor release and the first person to find out was a consumer whose build stopped compiling after an
        // update. With package validation on, `dotnet pack` takes the previous release from nuget.org, compares
        // the public API of every target framework it shipped, and fails on a break; an addition passes.
        var enabled = SharedBuildProperty("EnablePackageValidation");
        Assert.True(enabled.Length == 1 && enabled[0] == "true",
            "Directory.Build.props must switch package validation on exactly once "
            + $"(<EnablePackageValidation>true); found: [{string.Join(", ", enabled)}]");

        _ = ValidationBaseline();

        // One baseline in one place. A package that carries its own copy, or switches validation off, is the
        // first one to be left behind when the baseline moves.
        var overriding = LivePackages()
            .Where(p => !IsToolPackage(p.Xml))
            .Where(p => PackageValidationSwitches.Any(name => p.Xml.Descendants(name).Any()))
            .Select(p => p.Path)
            .ToArray();

        Assert.True(overriding.Length == 0,
            "These packages set their own package-validation switches instead of inheriting the shared ones: "
            + string.Join(", ", overriding));
    }

    [Fact]
    public void ThePackBaseline_IsOlderThanTheVersionBeingReleased()
    {
        // A baseline equal to the new version compares the package with itself and can never fail; a baseline
        // above it compares with the future. That is exactly what a version sweep that rewrites every occurrence
        // of the old number does to the one line that was meant to keep it.
        var baseline = ValidationBaseline();
        var version = System.Version.Parse(LiveVersion());

        Assert.True(baseline < version,
            $"packs are compared with {baseline}, which is not older than the version being released ({version})");
    }

    [Fact]
    public void ThePackBaseline_IsTheReleaseJustBeforeThisOne()
    {
        // "The previous release" is not "some older one": a baseline left at the release before last silently
        // lets everything that release shipped be broken. The changelog lists the releases in order, so the
        // section under the current version names the one the baseline has to be.
        var releases = SourceFile.SectionsOf(Read("CHANGELOG.md")).Select(section => Regex.Match(section.Heading, @"^\[(\d+\.\d+\.\d+)\]"))
            .Where(heading => heading.Success)
            .Select(heading => heading.Groups[1].Value)
            .ToArray();
        int at = Array.IndexOf(releases, LiveVersion());

        Assert.True(at >= 0 && at + 1 < releases.Length,
            $"the changelog has no release section under {LiveVersion()} to take the previous release from");

        Assert.True(System.Version.Parse(releases[at + 1]) == ValidationBaseline(),
            $"packs are compared with {ValidationBaseline()}, and the release before {LiveVersion()} is {releases[at + 1]}: "
            + "move <PackageValidationBaselineVersion> in Directory.Build.props together with the version");
    }

    /// <summary>A package whose version is written once, as a property of <c>Directory.Build.props</c>, the project that references it
    /// by that property, and the oldest version a bump may set (or none). SourceLink was restated as a literal <c>Update</c> line in eight
    /// packages as well: nine copies of one number, so a bump that missed one left that package on the old version while the other eight
    /// moved, and nothing said so; 10.0.303 is the first SourceLink outside every range CVE-2026-62900 names (the comment above the
    /// reference carries the reasoning). The core wrote the Tensors reference as a floating "10.*", which is resolved only inside the
    /// project that writes it: a project that references the core resolved it to whatever 10.0.x its machine had cached while the core
    /// was built on the newest, and the Wpf and Uno suites refused to build with -warnaserror (MSB3277: two versions of one assembly).</summary>
    public static TheoryData<string, string, string, string?> PackagesWrittenOnce { get; } = new()
    {
        { "Microsoft.SourceLink.GitHub", "SourceLinkVersion", "Directory.Build.props", "10.0.303" },
        { "System.Numerics.Tensors", "SystemNumericsTensorsVersion", "Src/MatPlotLibNet/MatPlotLibNet.csproj", null },
    };

    [Theory]
    [MemberData(nameof(PackagesWrittenOnce))]
    public void APackageVersion_LivesInOnePlace(string package, string property, string referencedIn, string? firstAllowed)
    {
        var restated = RepositoryProjects()
            .Where(file => file.Text.Xml().Descendants()
                .Where(n => n.Name.LocalName is "PackageReference" or "PackageVersion")
                .Where(n => string.Equals((string?)n.Attribute("Include") ?? (string?)n.Attribute("Update"), package, StringComparison.Ordinal))
                .Select(n => (string?)n.Attribute("Version") ?? (string?)n.Attribute("VersionOverride"))
                .Any(version => version is not null && !version.StartsWith("$(", StringComparison.Ordinal)))
            .Select(file => file.Path)
            .ToArray();

        Assert.True(restated.Length == 0, $"These projects write a literal {package} version instead of $({property}): " + string.Join(", ", restated));

        var defined = SharedBuildProperty(property);
        Assert.True(defined.Length == 1 && System.Version.TryParse(defined[0], out _),
            $"Directory.Build.props must define the {package} version once, as an exact version number (<{property}>); "
            + $"it defines it {defined.Length} time(s): {string.Join(", ", defined)}");

        var reference = XDocument.Load(Path.Combine(Root, referencedIn)).Descendants("PackageReference")
            .Single(n => (string?)n.Attribute("Include") == package);
        Assert.Equal($"$({property})", (string?)reference.Attribute("Version"));

        Assert.True(firstAllowed is null || System.Version.Parse(defined[0]) >= System.Version.Parse(firstAllowed),
            $"{package} {defined[0]} is older than {firstAllowed}, the oldest version this row allows");
    }

    [Fact]
    public void TheMcpServerManifest_CarriesThePackageIdAndTheVersionOfItsProject()
    {
        var project = PackableProjects().Single(p => p.Path.EndsWith("MatPlotLibNet.Mcp.csproj", StringComparison.Ordinal));
        string version = VersionOf(project.Xml);
        string packageId = project.Xml.Descendants("PackageId").First().Value.Trim();

        using var manifest = JsonDocument.Parse(Read("Src", "MatPlotLibNet.Mcp", ".mcp", "server.json"));
        var package = manifest.RootElement.GetProperty("packages")[0];

        // The manifest is what an MCP host reads to resolve the server; a version it carries that the package does
        // not have resolves to nothing.
        Assert.Equal(version, manifest.RootElement.GetProperty("version").GetString());
        Assert.Equal(version, package.GetProperty("version").GetString());
        Assert.Equal(packageId, package.GetProperty("identifier").GetString());
        Assert.Equal("nuget", package.GetProperty("registryType").GetString());
        Assert.Equal("stdio", package.GetProperty("transport").GetProperty("type").GetString());
    }

    [Fact]
    public void TheVersionTheArchitectureDocumentNamesOnItsFirstLine_IsTheVersionThePackagesCarry()
    {
        // The first line of the architecture document says "(v1.18.2)" long after the packages moved on: nothing tied
        // the hand-written number to the one the csproj files carry.
        string firstLine = RepositoryTree.FileAt("Src", "MatPlotLibNet", "ARCHITECTURE.md").Lines[0];
        var named = Regex.Match(firstLine, @"\(v(\d+\.\d+\.\d+)\)");

        Assert.True(named.Success, $"The first line of ARCHITECTURE.md names no version as \"(vX.Y.Z)\": {firstLine.Trim()}");
        Assert.True(named.Groups[1].Value == LiveVersion(),
            $"ARCHITECTURE.md names v{named.Groups[1].Value} on its first line; the packages carry {LiveVersion()}.");
    }

    [Fact]
    public void TheNewestVersionTheReadmeDescribes_IsTheVersionThePackagesCarry()
    {
        // "Where this is going" opens each release with its number in bold. The first of them is the newest, and it
        // must be the release being built, or the page tells a reader the library stopped a release ago.
        string readme = Read("README.md");
        var newest = Regex.Match(SourceFile.SectionOf(readme, "Where this is going"), @"\*\*(\d+\.\d+\.\d+)\*\*");
        Assert.True(newest.Success, "\"Where this is going\" opens no release with its bold version number.");
        Assert.True(newest.Groups[1].Value == LiveVersion(),
            $"README.md describes {newest.Groups[1].Value} as the newest release; the packages carry {LiveVersion()}.");
    }

    [Fact]
    public void TheLineTheSecurityPolicySupports_IsTheMinorLineThePackagesCarry()
    {
        // SECURITY.md listed 0.3.x as the supported line while the packages carried 1.19.0: the table is a hand-kept
        // copy of the version, so it is held to the one the csproj files carry.
        string policy = Read("SECURITY.md");
        var supported = Regex.Match(policy, @"^\|\s*(\d+\.\d+)\.x\s*\|\s*:white_check_mark:", RegexOptions.Multiline);
        var live = Regex.Match(LiveVersion(), @"^\d+\.\d+").Value;

        Assert.True(supported.Success, "SECURITY.md lists no supported line as \"| X.Y.x | :white_check_mark: |\".");
        Assert.True(supported.Groups[1].Value == live,
            $"SECURITY.md supports {supported.Groups[1].Value}.x; the packages carry {LiveVersion()}.");
    }
}
