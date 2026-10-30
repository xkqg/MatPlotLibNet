// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>
/// A release is carried by six hand-kept lists — the CI solution filter, two workflow test blocks, two coverage
/// runners and the version in every csproj — and nothing checked that they agree. They already had drifted:
/// <c>ci.yml</c> ran eight test projects and skipped DataFrame, <c>publish.yml</c> ran eight and skipped Skia, so
/// two suites guarded nothing on the very run that publishes. A list that must be edited by hand is only as good
/// as the memory of whoever edits it; this file turns each of those agreements into a failing test instead.
/// </summary>
public class ReleaseContractTests
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

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));

    /// <summary>Every project the CI solution filter carries, as repo-relative paths with forward slashes.</summary>
    private static string[] CiFilterProjects()
    {
        using var doc = JsonDocument.Parse(Read("MatPlotLibNet.CI.slnf"));
        return [.. doc.RootElement.GetProperty("solution").GetProperty("projects")
            .EnumerateArray().Select(p => p.GetString()!.Replace('\\', '/'))];
    }

    private static string[] CiFilterTestProjects() =>
        [.. CiFilterProjects().Where(p => p.StartsWith("Tst/", StringComparison.Ordinal))];

    /// <summary>Every `dotnet run --project Tst/...` line in a workflow, as the project path it names.</summary>
    private static string[] TestProjectsRunBy(string workflow) =>
        [.. Regex.Matches(Read(".github", "workflows", workflow), @"dotnet run --project (Tst/[^\s]+\.csproj)")
            .Select(m => m.Groups[1].Value)];

    /// <summary>The suites a workflow runs against output it did NOT build itself — those are the ones the CI
    /// solution filter has to carry. A line without `--no-build` builds its own project (the Windows MAUI job
    /// does exactly that, because MAUI needs workloads the Linux job has not got).</summary>
    private static string[] TestProjectsRunWithoutBuilding(string workflow) =>
        [.. Regex.Matches(Read(".github", "workflows", workflow), @"dotnet run --project (Tst/\S+\.csproj).*--no-build")
            .Select(m => m.Groups[1].Value)];

    /// <summary>Every packable project under Src/ — the ones that become a NuGet package.</summary>
    private static (string Path, XDocument Xml)[] PackableProjects() =>
        [.. Directory.EnumerateFiles(Path.Combine(Root, "Src"), "*.csproj", SearchOption.AllDirectories)
            .Select(file => (Path: Path.GetRelativePath(Root, file).Replace('\\', '/'), Xml: XDocument.Load(file)))
            .Where(p => p.Xml.Descendants("PackageId").Any())
            .OrderBy(p => p.Path, StringComparer.Ordinal)];

    private static string VersionOf(XDocument csproj) =>
        csproj.Descendants("Version").FirstOrDefault()?.Value.Trim()
        ?? throw new InvalidOperationException("no <Version>");

    // ---- packages that are finished, and say so ---------------------------------------------------------------

    /// <summary>Packages that are deliberately standing still: they keep the version they were last released at,
    /// nothing builds them, nothing publishes them, and nothing measures them. A frozen package is not a forgotten
    /// one, and the difference has to be written down or six months from now nobody can tell them apart — which is
    /// why every test that would otherwise go red for a frozen package reads this list and says the reason.</summary>
    private static readonly IReadOnlyDictionary<string, string> Frozen = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Src/MatPlotLibNet.Notebooks/MatPlotLibNet.Notebooks.csproj"] =
            "it renders into Polyglot Notebooks, whose runtime Microsoft has ended; MatPlotLibNet.Verso is the successor",
    };

    private static (string Path, XDocument Xml)[] LivePackages() =>
        [.. PackableProjects().Where(p => !Frozen.ContainsKey(p.Path))];

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
    public void AFrozenPackage_IsBuiltByNothingAndPushedByNothing()
    {
        // The other half of standing still: a package that is still in the CI filter is still packed by
        // publish-core and still pushed, so the freeze would last exactly until the next release.
        var filter = CiFilterProjects();
        string publish = Read(".github", "workflows", "publish.yml");

        var stillMoving = Frozen.Keys
            .Where(path => filter.Contains(path, StringComparer.Ordinal)
                        || publish.Contains(path, StringComparison.Ordinal))
            .ToArray();

        Assert.True(stillMoving.Length == 0,
            "These packages are declared frozen and a workflow still builds or pushes them: "
            + string.Join(", ", stillMoving));
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

    // ---- the gate can only see what it is told about ----------------------------------------------------------

    /// <summary>The assemblies the coverage collector is allowed to record, read back from the
    /// <c>&lt;ModulePath&gt;</c> regexes as the plain assembly names they match.</summary>
    private static string[] AssembliesTheCollectorRecords() =>
        [.. XDocument.Load(Path.Combine(Root, "tools", "coverage", "coverage.runsettings"))
            .Descendants("ModulePath")
            .Select(m => Regex.Match(m.Value.Trim(), @"^\.\*(.+)\\\.dll\$$").Groups[1].Value
                              .Replace("\\.", ".", StringComparison.Ordinal))
            .Where(name => name.Length > 0)];

    /// <summary>The suites the coverage runner actually executes.</summary>
    private static string[] SuitesTheCoverageRunnerExecutes() =>
        [.. Regex.Matches(Read("tools", "coverage", "run.sh"), @"""(Tst/\S+\.csproj)""")
            .Select(m => m.Groups[1].Value)];

    [Fact]
    public void EveryPackageCiBuilds_IsBothMeasuredAndExercised()
    {
        // The gate reads a Cobertura file, and a class that is in no Cobertura file cannot fail it. Two separate
        // things have to be true before the gate can see an assembly at all: the collector's allowlist has to name
        // it, and some suite the coverage runner executes has to load it. Either one alone is a green that means
        // nothing — an allowlisted assembly no test process loads produces no rows, and a suite for an assembly
        // outside the allowlist produces none either. This file's own summary has claimed since it was written
        // that it pins the two coverage runners; until now no test in the solution read either of them.
        var recorded = AssembliesTheCollectorRecords();
        var exercised = SuitesTheCoverageRunnerExecutes();
        var built = CiFilterProjects();

        var unseen = LivePackages()
            .Select(p => p.Xml.Descendants("PackageId").First().Value.Trim())
            .Where(id => built.Contains($"Src/{id}/{id}.csproj", StringComparer.Ordinal))
            .Select(id => (Id: id, Missing: string.Join(" and ", new[]
            {
                recorded.Contains(id, StringComparer.Ordinal) ? null : "no <ModulePath> in coverage.runsettings",
                exercised.Contains($"Tst/{id}/{id}.Tests.csproj", StringComparer.Ordinal) ? null : "no suite in run.sh",
            }.Where(reason => reason is not null))))
            .Where(p => p.Missing.Length > 0)
            .ToArray();

        Assert.True(unseen.Length == 0,
            "CI builds these packages and the coverage gate never sees a class of them: "
            + string.Join(", ", unseen.Select(p => $"{p.Id} ({p.Missing})")));
    }

    [Fact]
    public void EveryRecordedPackage_IsLoadedByItsSuite_OrItsSuiteIsRecordedInItsPlace()
    {
        // The allowlist names an assembly and a suite runs, but the collector records a module only while the test
        // process loads it. A suite that LINKS a package's source files compiles them into its own *.Tests.dll, which
        // the runsettings leaves out, so none of the package's classes ever reached a Cobertura file: Avalonia was in
        // the allowlist, ran its suite and was invisible to the gate. A suite must either reference the package
        // project (the package assembly is then the module) or, where the package cannot be referenced from a plain
        // net10.0 suite (MAUI, whose target frameworks are platform ones), the linked suite's own assembly has to be
        // on the allowlist.
        var recorded = AssembliesTheCollectorRecords();

        var unseen = new List<string>();
        foreach (var id in LivePackages().Select(p => p.Xml.Descendants("PackageId").First().Value.Trim())
                     .Where(id => recorded.Contains(id, StringComparer.Ordinal)))
        {
            string suitePath = Path.Combine(Root, "Tst", id, $"{id}.Tests.csproj");
            if (!File.Exists(suitePath))
            {
                continue;
            }

            var suite = XDocument.Load(suitePath);
            var referenced = suite.Descendants("ProjectReference")
                .Select(r => (r.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/'))
                .Any(r => r.EndsWith($"Src/{id}/{id}.csproj", StringComparison.Ordinal));
            var linked = suite.Descendants("Compile")
                .Select(c => (c.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/'))
                .Any(c => c.Contains($"Src/{id}/", StringComparison.Ordinal));

            if (!referenced && !(linked && recorded.Contains($"{id}.Tests", StringComparer.Ordinal)))
            {
                unseen.Add(linked
                    ? $"{id} (its suite links the sources into {id}.Tests.dll, which the runsettings does not record)"
                    : $"{id} (its suite neither references nor links the package)");
            }
        }

        Assert.True(unseen.Count == 0,
            "These packages are on the coverage allowlist and their suites never load them, so no class of them is measured: "
            + string.Join(", ", unseen));
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
        var releases = Regex.Matches(Read("CHANGELOG.md"), @"^## \[(\d+\.\d+\.\d+)\]", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value)
            .ToArray();
        int at = Array.IndexOf(releases, LiveVersion());

        Assert.True(at >= 0 && at + 1 < releases.Length,
            $"the changelog has no release section under {LiveVersion()} to take the previous release from");

        Assert.True(System.Version.Parse(releases[at + 1]) == ValidationBaseline(),
            $"packs are compared with {ValidationBaseline()}, and the release before {LiveVersion()} is {releases[at + 1]}: "
            + "move <PackageValidationBaselineVersion> in Directory.Build.props together with the version");
    }

    // ---- SourceLink: one version, one place --------------------------------------------------------------------

    private const string SourceLinkPackage = "Microsoft.SourceLink.GitHub";

    /// <summary>The oldest SourceLink that is outside every range CVE-2026-62900 names. The comment above the
    /// reference in <c>Directory.Build.props</c> carries the reasoning; this is the number a bump may not go below.</summary>
    private static readonly System.Version SourceLinkFirstPatched = new(10, 0, 303);

    /// <summary>Every project file the repository builds: the packages, their suites, the samples and the benchmarks.</summary>
    private static string[] RepositoryProjects() =>
        [.. new[] { "Src", "Tst", "Samples", "Benchmarks" }
            .Select(folder => Path.Combine(Root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(file => file, StringComparer.Ordinal)];

    [Fact]
    public void TheSourceLinkVersion_LivesInOnePlace()
    {
        // The reference is declared once, in the shared build file, and every project inherits it. It used to be
        // restated as a literal `Update` line in eight packages as well: nine copies of one number, so a bump that
        // missed one left that package on the old version while the other eight moved, and nothing said so.
        var restated = RepositoryProjects()
            .Where(file => XDocument.Load(file).Descendants()
                .Where(n => n.Name.LocalName is "PackageReference" or "PackageVersion")
                .Where(n => string.Equals((string?)n.Attribute("Include") ?? (string?)n.Attribute("Update"),
                                          SourceLinkPackage, StringComparison.Ordinal))
                .Select(n => (string?)n.Attribute("Version") ?? (string?)n.Attribute("VersionOverride"))
                .Any(version => version is not null && !version.StartsWith("$(", StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(Root, file).Replace('\\', '/'))
            .ToArray();

        Assert.True(restated.Length == 0,
            $"These projects write a literal {SourceLinkPackage} version instead of inheriting the one in "
            + "Directory.Build.props: " + string.Join(", ", restated));

        var defined = SharedBuildProperty("SourceLinkVersion");
        Assert.True(defined.Length == 1 && System.Version.TryParse(defined[0], out _),
            "Directory.Build.props must define the SourceLink version once, as a version number "
            + $"(<SourceLinkVersion>); it defines it {defined.Length} time(s): {string.Join(", ", defined)}");

        var reference = XDocument.Load(Path.Combine(Root, "Directory.Build.props")).Descendants("PackageReference")
            .Single(n => (string?)n.Attribute("Include") == SourceLinkPackage);
        Assert.Equal("$(SourceLinkVersion)", (string?)reference.Attribute("Version"));

        Assert.True(System.Version.Parse(defined[0]) >= SourceLinkFirstPatched,
            $"{SourceLinkPackage} {defined[0]} is older than {SourceLinkFirstPatched}, the first release outside "
            + "the ranges CVE-2026-62900 names");
    }

    [Fact]
    public void TheCoverageCollector_IsDotnetCoverage_AndNoProjectCarriesACoverletPackage()
    {
        // Coverage is collected by `dotnet-coverage collect` around the built test executable; both runners say so
        // and neither passes `CollectCoverage`. A coverlet package in a suite is therefore never switched on: the
        // only thing it still does is write a source-root mapping file next to the test binaries on every build.
        // Three suites carried `coverlet.msbuild` as a floating "6.*" for exactly that, and the line sat there
        // looking like the coverage mechanism, which sent the next reader to bump it instead of to the runner.
        // A package that nothing triggers is deleted, not kept current.
        foreach (string runner in new[] { "run.sh", "run.ps1" })
        {
            Assert.True(Read("tools", "coverage", runner).Contains("dotnet-coverage collect", StringComparison.Ordinal),
                $"tools/coverage/{runner} no longer collects with dotnet-coverage; this contract assumed it did.");
        }

        var carriers = RepositoryProjects()
            .Where(file => XDocument.Load(file).Descendants()
                .Where(n => n.Name.LocalName is "PackageReference" or "PackageVersion")
                .Select(n => (string?)n.Attribute("Include") ?? (string?)n.Attribute("Update") ?? string.Empty)
                .Any(id => id.StartsWith("coverlet.", StringComparison.OrdinalIgnoreCase)))
            .Select(file => Path.GetRelativePath(Root, file).Replace('\\', '/'))
            .ToArray();

        Assert.True(carriers.Length == 0,
            "These projects reference a coverlet package that nothing in the coverage pipeline triggers "
            + "(dotnet-coverage measures them): " + string.Join(", ", carriers));
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

    /// <summary>The release notes every package inherits, read from the one file that defines them.</summary>
    private static string ReleaseNotesTemplate()
    {
        var shared = XDocument.Load(Path.Combine(Root, "Directory.Build.targets"));
        return shared.Descendants("PackageReleaseNotes").SingleOrDefault()?.Value.Trim()
            ?? throw new InvalidOperationException("Directory.Build.targets defines no <PackageReleaseNotes>");
    }

    [Fact]
    public void EveryPackableProject_ShipsAnIcon()
    {
        // Without one, every NuGet search row and every row of the Visual Studio package manager shows a
        // placeholder — the first thing a developer sees about all fourteen packages. One image in the shared
        // build file covers them at once; this pins that it is declared and that no project blanks it out.
        var shared = XDocument.Load(Path.Combine(Root, "Directory.Build.props"));
        string icon = shared.Descendants("PackageIcon").Single().Value.Trim();

        Assert.Equal("icon.png", icon);
        Assert.True(File.Exists(Path.Combine(Root, icon)), $"{icon} is declared and not in the repository");

        var blanked = PackableProjects()
            .Where(p => p.Xml.Descendants("PackageIcon").Any(n => string.IsNullOrWhiteSpace(n.Value)))
            .Select(p => p.Path)
            .ToArray();

        Assert.True(blanked.Length == 0,
            $"These packages override the shared icon with nothing: {string.Join(", ", blanked)}");
    }

    [Fact]
    public void TheIcon_SurvivesGitignore()
    {
        // Measured the hard way: the repository ignores every PNG at its root, so the icon existed here, packed
        // here, passed every check here — and was never committed. A pack on a fresh checkout fails with NU5046,
        // "the icon file does not exist in the package", on the run that publishes.
        var rules = File.ReadAllLines(Path.Combine(Root, ".gitignore")).Select(l => l.Trim()).ToArray();

        bool ignoredByARootRule = rules.Contains("/*.png") || rules.Contains("*.png");
        bool rescued = rules.Contains("!/icon.png") || rules.Contains("!icon.png");

        Assert.True(!ignoredByARootRule || rescued,
            ".gitignore hides every PNG at the repository root and does not make an exception for icon.png");
    }

    [Fact]
    public void TheIcon_IsThePngNuGetAsksFor()
    {
        // nuget.org takes PNG or JPEG, caps the file at 1 MB and recommends 128x128. A file that misses any of
        // those is refused at push time, which is the worst moment to find out.
        byte[] png = File.ReadAllBytes(Path.Combine(Root, "icon.png"));

        Assert.Equal<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], png[..8]);

        // The IHDR chunk carries the dimensions, big-endian, right after the signature and the chunk header.
        int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];

        Assert.Equal(128, width);
        Assert.Equal(128, height);
        Assert.True(png.Length < 1024 * 1024, $"the icon is {png.Length} bytes; nuget.org caps it at 1 MB");
    }

    // ---- what a search engine reads ---------------------------------------------------------------------------

    /// <summary>The YAML header of a documentation page, or an empty string if it has none.</summary>
    private static string FrontMatter(string markdown)
    {
        if (!markdown.StartsWith("---", StringComparison.Ordinal)) { return string.Empty; }

        int end = markdown.IndexOf("\n---", 3, StringComparison.Ordinal);
        return end < 0 ? string.Empty : markdown[3..end];
    }

    [Fact]
    public void EveryCookbookPage_TellsASearchEngineWhatItIsAndWhatItAnswers()
    {
        // A cookbook page is the page a stranger can actually land on: they search for the thing they want to
        // draw, not for this library's name. Two lines of YAML decide what they see in the result. Without a
        // title docfx falls back to the heading, which is written for a reader who is already here — "Line
        // Charts" — instead of the words that were typed to find it.
        var missing = Directory.EnumerateFiles(Path.Combine(Root, "docs", "cookbook"), "*.md")
            .Select(file => (Name: Path.GetFileName(file), Head: FrontMatter(File.ReadAllText(file))))
            .Where(p => !p.Head.Contains("\ntitle:", StringComparison.Ordinal)
                     || !p.Head.Contains("\ndescription:", StringComparison.Ordinal))
            .Select(p => p.Name)
            .ToArray();

        Assert.True(missing.Length == 0,
            $"These cookbook pages carry no title or no description of their own: {string.Join(", ", missing)}");
    }

    [Fact]
    public void TheDocumentationHomePage_SaysWhatThisIsInTheFormATSearchEngineReads()
    {
        // Measured over the built site: 0 of 772 pages carried structured data of any kind, so a search engine
        // had nothing but prose to decide what this project even is — and the name reads as "matplotlib" plus
        // ".net", which is two other projects' names. One JSON-LD block on the page that matters states it.
        // It is raw HTML in the Markdown, which markdig passes straight through; no forked docfx template.
        string home = Read("docs", "index.md");
        var block = Regex.Match(home, @"<script type=""application/ld\+json"">\s*(?<json>\{.*?\})\s*</script>", RegexOptions.Singleline);

        Assert.True(block.Success, "docs/index.md carries no JSON-LD block");

        using var data = JsonDocument.Parse(block.Groups["json"].Value);
        var root = data.RootElement;

        Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
        Assert.Equal("SoftwareApplication", root.GetProperty("@type").GetString());
        Assert.Equal("MatPlotLibNet", root.GetProperty("name").GetString());
        Assert.Equal("https://xkqg.github.io/MatPlotLibNet/", root.GetProperty("url").GetString());
        Assert.Equal("https://github.com/xkqg/MatPlotLibNet", root.GetProperty("codeRepository").GetString());
        Assert.Equal("https://www.nuget.org/packages/MatPlotLibNet", root.GetProperty("downloadUrl").GetString());
    }

    [Fact]
    public void TheIndexNowKey_IsPublishedWithTheSiteAndSaysItsOwnName()
    {
        // IndexNow is how a site tells Bing and Yandex that a page changed without holding an account anywhere:
        // the proof of ownership is a file, served from the site itself, whose CONTENT is its own name. This
        // site lives under a project path and the host root answers 404, so the key is submitted with an
        // explicit keyLocation — which means the file has to be reachable at exactly that address. A key docfx
        // does not copy into the site answers 404 there, and every submission made with it is refused.
        var keys = Directory.EnumerateFiles(Path.Combine(Root, "docs"), "*.txt")
            .Where(file => Path.GetFileNameWithoutExtension(file) is { Length: 32 } name
                        && name.All(Uri.IsHexDigit))
            .ToArray();

        string key = Assert.Single(keys);
        Assert.Equal(Path.GetFileNameWithoutExtension(key), File.ReadAllText(key).Trim());

        using var config = JsonDocument.Parse(Read("docs", "docfx.json"));
        var resources = config.RootElement.GetProperty("build").GetProperty("resource")[0].GetProperty("files")
            .EnumerateArray().Select(f => f.GetString()).ToArray();

        Assert.True(resources.Contains("*.txt", StringComparer.Ordinal)
                 || resources.Contains(Path.GetFileName(key), StringComparer.Ordinal),
            "docs/docfx.json does not copy the IndexNow key into the built site");
    }

    [Fact]
    public void TheDocumentationSite_CarriesItsAnalyticsTag()
    {
        // The tag is one key in globalMetadata and the modern template renders it into every page's head, so
        // there is nothing to maintain — but there is something to lose: a key removed or misspelled produces a
        // site that builds, deploys and reports nothing, and an empty report reads exactly like no visitors.
        using var config = JsonDocument.Parse(Read("docs", "docfx.json"));
        string? tag = config.RootElement.GetProperty("build").GetProperty("globalMetadata")
            .GetProperty("_googleAnalyticsTagId").GetString();

        Assert.Matches("^G-[A-Z0-9]{10}$", tag ?? string.Empty);
    }

    [Fact]
    public void EveryCookbookPage_SaysWhichPackageItNeeds()
    {
        // A cookbook page is where a stranger lands from a search — "candlestick chart in C#" — and every one of
        // the 34 arrived without naming a package or an install line. They are the pages that have to answer
        // "and how do I get this", because the reader has no reason to walk back to the home page for it.
        var silent = Directory.EnumerateFiles(Path.Combine(Root, "docs", "cookbook"), "*.md")
            .Where(file => !File.ReadAllText(file).Contains("dotnet add package", StringComparison.Ordinal)
                        && !File.ReadAllText(file).Contains("dotnet tool install", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.True(silent.Length == 0,
            $"These cookbook pages never say what to install: {string.Join(", ", silent)}");
    }

    [Fact]
    public void TheDocumentationHomePage_LinksEveryPackageToWhereItIsInstalledFrom()
    {
        // Someone who arrives at the documentation from a search is there to USE the library, and the package
        // table listed all fourteen names as plain text — the reader had to retype one into nuget.org. Every
        // name is a link now, and a package added later has to be one too, or the table quietly stops being
        // the way out of the site.
        string home = Read("docs", "index.md");

        var missing = PackableProjects()
            .Select(p => p.Xml.Descendants("PackageId").First().Value.Trim())
            .Where(id => !home.Contains($"[`{id}`](https://www.nuget.org/packages/{id})", StringComparison.Ordinal))
            .ToArray();

        Assert.True(missing.Length == 0,
            $"The package table does not link these to nuget.org: {string.Join(", ", missing)}");
    }

    [Fact]
    public void ThePagesWorkflow_StripsTheByteOrderMarkFromTheSitemap()
    {
        // docfx writes the sitemap as UTF-8 WITH a byte-order mark, three bytes in front of the XML
        // declaration, and there is no docfx setting for it — so the workflow that publishes the site takes
        // them off again. Nothing else about the file was wrong when Search Console refused it: 766 urls,
        // every one inside the property, no duplicates, valid priorities and lastmods, served as
        // application/xml over a 200 to a Googlebot user agent.
        string workflow = Read(".github", "workflows", "pages.yml");
        int build = workflow.IndexOf("docfx build", StringComparison.Ordinal);
        int strip = workflow.IndexOf("_site/sitemap.xml", StringComparison.Ordinal);

        Assert.True(strip > build && build >= 0,
            "pages.yml no longer strips the byte-order mark from docs/_site/sitemap.xml after the docfx build");
    }

    [Fact]
    public void TheSearchConsoleVerification_IsPublishedWithTheSiteAndNamesItself()
    {
        // Google verifies ownership of a site by fetching one file it named, whose single line repeats that
        // file's own name. It is fetched again from time to time, not only once: delete the file, or stop
        // copying it into the built site, and the property silently falls back to unverified — with it goes
        // every crawl statistic and the ability to submit a sitemap.
        var files = Directory.EnumerateFiles(Path.Combine(Root, "docs"), "google*.html").ToArray();

        string verification = Assert.Single(files);
        Assert.Equal($"google-site-verification: {Path.GetFileName(verification)}", File.ReadAllText(verification).Trim());

        using var config = JsonDocument.Parse(Read("docs", "docfx.json"));
        var resources = config.RootElement.GetProperty("build").GetProperty("resource")[0].GetProperty("files")
            .EnumerateArray().Select(f => f.GetString()).ToArray();

        Assert.True(resources.Contains("google*.html", StringComparer.Ordinal)
                 || resources.Contains(Path.GetFileName(verification), StringComparer.Ordinal),
            "docs/docfx.json does not copy the Search Console verification file into the built site");
    }

    // ---- the numbers a package page prints --------------------------------------------------------------------

    /// <summary>Every counted claim a <c>&lt;Description&gt;</c> may carry, beside the code that measures it.</summary>
    private static (string Pattern, int Measured, string What)[] PublishedCounts() =>
    [
        (@"(\d+) colormaps", Styling.ColorMapRegistryTests.DeclaredColorMaps().Length * 2, "colormaps"),
        (@"(\d+) base (?:maps|colormaps)", Styling.ColorMapRegistryTests.DeclaredColorMaps().Length, "base maps"),
        (@"(\d+) (?:series types|chart types)", Models.SeriesCountContractTests.ConcreteSeries().Length, "concrete series"),
        (@"(\d+) themes", ThemeCount(), "themes"),
        // The indicator count is NOT here: it is package-specific. The core assembly ships 58 indicator types and
        // the DataFrame package exposes its own extension surface over them, so one number checked against one
        // assembly would be wrong for the other package. DataFrame pins its own, in its own test project.
    ];

    private static int ThemeCount() =>
        typeof(global::MatPlotLibNet.Styling.Theme)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Count(p => p.PropertyType == typeof(global::MatPlotLibNet.Styling.Theme));

    [Fact]
    public void EveryPackageDescription_CountsWhatTheLibraryActuallyShips()
    {
        // The colormap count is pinned across every *.md in the repository — and the number that reaches the most
        // readers is in no *.md at all. It is <Description>: the text nuget.org prints on the package page and the
        // package manager prints in its search row. The core package advertised 142 colormaps there while the
        // registry held 148, and nothing was red. The same hole as the tags — product text no test reads.
        foreach (var (path, xml) in PackableProjects())
        {
            string description = xml.Descendants("Description").FirstOrDefault()?.Value ?? string.Empty;

            foreach (var (pattern, measured, what) in PublishedCounts())
            {
                foreach (Match m in Regex.Matches(description, pattern))
                {
                    Assert.True(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) == measured,
                        $"{path} says \"{m.Value}\"; the library ships {measured} {what}.");
                }
            }
        }
    }

    // ---- what a reader types into the search box ------------------------------------------------------------

    /// <summary>The tags of one package, however the project spelled its separator.</summary>
    private static string[] TagsOf(XDocument csproj) =>
        [.. (csproj.Descendants("PackageTags").FirstOrDefault()?.Value ?? string.Empty)
            .Split([';', ' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    [Fact]
    public void EveryPackage_CarriesTheWordThisLibraryIsSearchedFor()
    {
        // Measured against the live nuget.org search index: on the query "matplotlib" — 31 hits, the one query
        // this library is the answer to — only MatPlotLibNet.DataFrame reached the first page, at rank 10. The
        // core package sat at 14, below two packages with a hundredth of its downloads. DataFrame was also the
        // only one of the fourteen whose tags spelled the word; the search index cannot rank on a word no
        // package carries.
        var without = PackableProjects()
            .Where(p => !TagsOf(p.Xml).Contains("matplotlib", StringComparer.OrdinalIgnoreCase))
            .Select(p => p.Path)
            .ToArray();

        Assert.True(without.Length == 0,
            $"These packages cannot be found by the name of what they are: {string.Join(", ", without)}");
    }

    [Fact]
    public void EveryPackagesTags_AreSeparatedTheSameWay()
    {
        // NuGet accepts a space-separated and a semicolon-separated list alike, so a project that mixes them
        // reads correctly and reviews wrongly: thirteen projects used semicolons and one used spaces, and the
        // odd one out is invisible until someone appends a tag with a semicolon to it and silently creates
        // "finance;matplotlib" as a single tag.
        var mixed = PackableProjects()
            .Where(p => (p.Xml.Descendants("PackageTags").FirstOrDefault()?.Value ?? string.Empty).Contains(' '))
            .Select(p => p.Path)
            .ToArray();

        Assert.True(mixed.Length == 0,
            $"These packages separate their tags with spaces where every other package uses ';': {string.Join(", ", mixed)}");
    }

    [Fact]
    public void EveryPackableProject_ShipsReleaseNotes()
    {
        // A package page with an empty Release Notes box tells a reader nothing about what they are upgrading
        // into. Fourteen hand-kept copies of the same sentence would drift the moment one of them is forgotten,
        // so the text lives in one shared build file and every project inherits it; this pins that it is there
        // and that no project quietly blanks it out.
        Assert.False(string.IsNullOrWhiteSpace(ReleaseNotesTemplate()));

        var overriding = PackableProjects()
            .Where(p => p.Xml.Descendants("PackageReleaseNotes").Any(n => string.IsNullOrWhiteSpace(n.Value)))
            .Select(p => p.Path)
            .ToArray();

        Assert.True(overriding.Length == 0,
            $"These packages override the shared release notes with nothing: {string.Join(", ", overriding)}");
    }

    [Fact]
    public void TheReleaseNotes_LinkToAChangelogSectionThatExists()
    {
        // The notes carry a deep link, and a deep link to a heading that is not there lands the reader at the top
        // of a long file with no idea which part was theirs. Both halves are built from the version, so this
        // check is what keeps the link honest after the next bump.
        string version = VersionOf(PackableProjects()[0].Xml);
        string notes = ReleaseNotesTemplate()
            .Replace("$(ChangelogAnchor)", version.Replace(".", string.Empty), StringComparison.Ordinal)
            .Replace("$(Version)", version, StringComparison.Ordinal);

        Assert.DoesNotContain("$(", notes, StringComparison.Ordinal);
        Assert.Contains(version, notes, StringComparison.Ordinal);

        string anchor = Regex.Match(notes, @"CHANGELOG\.md#([A-Za-z0-9-]+)").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(anchor), "the release notes carry no link into the changelog");

        // GitHub builds a heading anchor by lower-casing, dropping punctuation and joining words with hyphens,
        // so "## [1.17.1]" becomes "#1171".
        var headings = Regex.Matches(Read("CHANGELOG.md"), @"^## (.+)$", RegexOptions.Multiline)
            .Select(m => Regex.Replace(m.Groups[1].Value.ToLowerInvariant(), @"[^a-z0-9 -]", string.Empty).Trim().Replace(' ', '-'))
            .ToArray();

        Assert.Contains(anchor, headings);
    }

    // ---- the lists that decide what CI guards and what a release ships ---------------------------------------

    /// <summary>Suites of the CI filter a workflow builds and deliberately does not run, each with the reason.</summary>
    private static readonly IReadOnlyDictionary<WorkflowSuite, string> NotRunByTheWorkflow = new Dictionary<WorkflowSuite, string>
    {
        [new("publish.yml", "Tst/MatPlotLibNet.Wiki/MatPlotLibNet.Wiki.Tests.csproj")] =
            "publish checks out the release tag and the wiki has no tags: a wiki newer than the tag would compile against older code and block the release",
    };

    [Theory]
    [InlineData("ci.yml")]
    [InlineData("publish.yml")]
    public void EveryTestProjectInTheCiFilter_IsRunByTheWorkflow(string workflow)
    {
        var run = TestProjectsRunBy(workflow);
        var missing = CiFilterTestProjects()
            .Except(run, StringComparer.Ordinal)
            .Where(suite => !NotRunByTheWorkflow.ContainsKey(new(workflow, suite)))
            .ToArray();

        Assert.True(missing.Length == 0,
            $"{workflow} builds these suites and never runs them: {string.Join(", ", missing)}");
    }

    [Fact]
    public void ASuiteAWorkflowIsSaidNotToRun_IsNotRunByIt()
    {
        // An exception that no longer holds is a reason nobody can trust: drop it from the list the day the workflow
        // starts running the suite after all.
        var stale = NotRunByTheWorkflow.Keys
            .Where(key => TestProjectsRunBy(key.Workflow).Contains(key.Suite, StringComparer.Ordinal))
            .Select(key => $"{key.Workflow} runs {key.Suite}")
            .ToArray();

        Assert.True(stale.Length == 0, "Listed as not run, and run anyway: " + string.Join(", ", stale));
    }

    /// <summary>The jobs of a workflow: each job's name and its text, up to the next job.</summary>
    private static WorkflowJob[] JobsOf(string workflow)
    {
        var text = Read(".github", "workflows", workflow).ReplaceLineEndings("\n");
        var body = text[(text.IndexOf("\njobs:\n", StringComparison.Ordinal) + "\njobs:\n".Length)..];
        var starts = Regex.Matches(body, @"^  (?<name>[A-Za-z0-9_-]+):\s*$", RegexOptions.Multiline);

        return [.. starts.Select((start, at) => new WorkflowJob(
            workflow + ":" + start.Groups["name"].Value,
            body[start.Index..(at + 1 < starts.Count ? starts[at + 1].Index : body.Length)]))];
    }

    private const string WikiSuite = "Tst/MatPlotLibNet.Wiki/MatPlotLibNet.Wiki.Tests.csproj";

    [Fact]
    public void EveryJobThatRunsTheWikiSuite_ClonesTheWikiBesideTheRepositoryFirst()
    {
        // The wiki is a repository of its own, so a runner has it only when a step clones it -- and the suite looks
        // for it beside this repository. A job that runs the suite without that step fails on the runner only.
        var clone = new Regex(@"git clone [^\n]*\.wiki\.git""?\s+\.\./MatPlotLibNet\.wiki\s*$", RegexOptions.Multiline);
        var jobs = Directory.GetFiles(Path.Combine(Root, ".github", "workflows"), "*.yml")
            .SelectMany(path => JobsOf(Path.GetFileName(path)))
            .Where(job => job.Text.Contains("dotnet run --project " + WikiSuite, StringComparison.Ordinal))
            .ToArray();

        Assert.Contains(jobs, job => job.Name == "ci.yml:build-and-test");

        var unready = jobs
            .Where(job => clone.Match(job.Text) is not { Success: true } cloned
                          || cloned.Index > job.Text.IndexOf("dotnet run --project " + WikiSuite, StringComparison.Ordinal))
            .Select(job => job.Name)
            .ToArray();

        Assert.True(unready.Length == 0,
            "These jobs run the wiki suite without first cloning the wiki into ../MatPlotLibNet.wiki: " + string.Join(", ", unready));
    }

    [Fact]
    public void EveryWikiClone_UsesTheUpstreamAddress_NotTheRepositoryTheRunBelongsTo()
    {
        // In a fork github.repository is the fork, and a fork has no wiki of its own: a clone built from it fails at
        // the clone and turns a contributor's push run red for a reason that is not theirs.
        const string upstream = "https://github.com/xkqg/MatPlotLibNet.wiki.git";
        var clones = Directory.GetFiles(Path.Combine(Root, ".github", "workflows"), "*.yml")
            .SelectMany(path => JobsOf(Path.GetFileName(path)))
            .SelectMany(job => Regex.Matches(job.Text, @"^\s*run:\s*git clone [^\n]*\.wiki\.git[^\n]*$", RegexOptions.Multiline)
                .Select(match => job.Name + ": " + match.Value.Trim()))
            .ToArray();

        Assert.NotEmpty(clones);

        var wrong = clones.Where(clone => !clone.Contains(upstream, StringComparison.Ordinal)
                                          || clone.Contains("github.repository", StringComparison.Ordinal)).ToArray();

        Assert.True(wrong.Length == 0,
            "These wiki clones do not use the fixed upstream address " + upstream + ": " + string.Join(" | ", wrong));
    }

    [Fact]
    public void EveryPublishJobThatPushesPackages_ChecksTheTagAgainstTheVersionFirst()
    {
        // A tag on the commit before the version bump would push the previous release again: --skip-duplicate skips
        // every package and the run is green with nothing new on nuget.org. A typo tag (v.1.18.0 exists) reads the
        // same. The step strips an optional 'v' or 'v.' and compares with the core package's <Version>.
        const string check = "- name: The tag names the version the packages carry";
        var pushing = JobsOf("publish.yml").Where(job => job.Text.Contains("dotnet nuget push", StringComparison.Ordinal)).ToArray();

        Assert.NotEmpty(pushing);

        foreach (var job in pushing)
        {
            var at = job.Text.IndexOf(check, StringComparison.Ordinal);

            Assert.True(at >= 0, $"{job.Name} pushes packages and never checks the tag against the version they carry.");
            Assert.True(
                at < job.Text.IndexOf("dotnet pack", StringComparison.Ordinal),
                $"{job.Name} checks the tag only after it packs.");

            var step = job.Text[at..];
            step = step[..(step.IndexOf("\n      - ", 1, StringComparison.Ordinal) is var next and > 0 ? next : step.Length)];

            Assert.Contains("Src/MatPlotLibNet/MatPlotLibNet.csproj", step, StringComparison.Ordinal);
            Assert.Contains("${tag#v}", step, StringComparison.Ordinal);
            Assert.Contains("${tag#.}", step, StringComparison.Ordinal);
            Assert.Contains("exit 1", step, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("ci.yml")]
    [InlineData("publish.yml")]
    public void EveryTestProjectTheWorkflowRuns_IsBuiltByTheCiFilter(string workflow)
    {
        // `--no-build` means a project outside the filter has no output to run; the step fails on the release.
        var unbuilt = TestProjectsRunWithoutBuilding(workflow).Except(CiFilterTestProjects(), StringComparer.Ordinal).ToArray();

        Assert.True(unbuilt.Length == 0,
            $"{workflow} runs these with --no-build but the CI filter never builds them: {string.Join(", ", unbuilt)}");
    }

    [Fact]
    public void EveryPackableProject_IsPublishedBySomeWorkflow()
    {
        // publish-core packs the whole CI filter in one go; the platform packages are packed by name on Windows,
        // and publish-uno likewise (its own job, in the same workflow and trigger, because its SDK has no dotnet
        // workload and needs the Uno.Templates install the other two jobs don't).
        var inFilter = CiFilterProjects();
        string publish = Read(".github", "workflows", "publish.yml");

        var unpublished = LivePackages()
            .Select(p => p.Path)
            .Where(path => !inFilter.Contains(path, StringComparer.Ordinal)
                           && !publish.Contains(path, StringComparison.Ordinal))
            .ToArray();

        Assert.True(unpublished.Length == 0,
            $"These packages have a PackageId and nothing ever pushes them: {string.Join(", ", unpublished)}");
    }

    // ---- the platform heads are judged on every push, not by the tag ------------------------------------------

    private const string MauiProject = "Src/MatPlotLibNet.Maui/MatPlotLibNet.Maui.csproj";
    private const string WpfProject = "Src/MatPlotLibNet.Wpf/MatPlotLibNet.Wpf.csproj";
    private const string MauiSuite = "Tst/MatPlotLibNet.Maui/MatPlotLibNet.Maui.Tests.csproj";
    private const string WpfSuite = "Tst/MatPlotLibNet.Wpf/MatPlotLibNet.Wpf.Tests.csproj";
    private const string UnoSuite = "Tst/MatPlotLibNet.Uno/MatPlotLibNet.Uno.Tests.csproj";

    /// <summary>The one job of <c>ci.yml</c> that runs on Windows: the packages that need a workload or a Windows SDK
    /// are built there, because the Linux runner cannot build them.</summary>
    private static WorkflowJob CiWindowsJob() =>
        JobsOf("ci.yml").Single(job => job.Text.Contains("runs-on: windows-latest", StringComparison.Ordinal));

    [Fact]
    public void TheWindowsHeads_AreBuiltAndTestedByCiOnEveryPush()
    {
        // Measured: MAUI, WPF and Uno were built for the first time by the publish workflow, after the tag, so a break
        // in them (or in the API-compatibility check their pack runs against the previous release) showed on a
        // release, where a package already pushed cannot be taken back. A job that builds what it runs is why these
        // projects stay out of the CI filter.
        var job = CiWindowsJob();
        int workloads = job.Text.IndexOf("dotnet workload install maui android", StringComparison.Ordinal);

        Assert.True(workloads >= 0, $"{job.Name} never installs the workloads MAUI needs.");

        foreach (string project in new[] { MauiProject, WpfProject })
        {
            int built = job.Text.IndexOf("dotnet build " + project, StringComparison.Ordinal);

            Assert.True(built >= 0, $"{job.Name} never builds {project}.");
            Assert.True(built > workloads, $"{job.Name} builds {project} before it installs the workloads.");
        }

        foreach (string suite in new[] { MauiSuite, WpfSuite, UnoSuite })
        {
            Assert.True(
                job.Text.Contains("dotnet run --project " + suite, StringComparison.Ordinal),
                $"{job.Name} never runs {suite}.");
        }
    }

    [Fact]
    public void TheWindowsJobOfCi_InstallsTheWorkloadsThePublishJobInstalls()
    {
        // The job exists to judge what publish-platform will build; a different workload list would judge another thing.
        static string WorkloadLine(string text) =>
            Regex.Match(text, @"dotnet workload install [^\r\n]+").Value.Trim();

        string publish = WorkloadLine(Read(".github", "workflows", "publish.yml"));

        Assert.NotEmpty(publish);
        Assert.Equal(publish, WorkloadLine(CiWindowsJob().Text));
    }

    [Fact]
    public void TheWindowsJobOfCi_IsStoppedByItsOwnLimit_NotByTheSixHourDefault()
    {
        // Measured: the Uno suite waited on a dialog nobody could close and the step ran 59 minutes without one line of
        // output. A job with no timeout-minutes is only stopped by the runner's default of six hours.
        var job = CiWindowsJob();
        var limit = Regex.Match(job.Text, @"^    timeout-minutes:\s*(?<minutes>\d+)\s*$", RegexOptions.Multiline);

        Assert.True(limit.Success, $"{job.Name} has no timeout-minutes, so a step that hangs holds the run for six hours.");
        Assert.InRange(int.Parse(limit.Groups["minutes"].Value, CultureInfo.InvariantCulture), 1, 60);
    }

    [Fact]
    public void TheUnoSuite_DoesNotStartTheWindowsAppRuntimeBootstrap()
    {
        // The suite is an unpackaged Exe, and for that Microsoft.WindowsAppSDK compiles in an initializer that runs
        // before Main and, when no matching Windows App Runtime is installed, shows a dialog (OnNoMatch_ShowUI is its
        // default). A development machine has the runtime. The runner has none and nobody there can close the dialog,
        // so the step hangs before the test host prints its banner. The suite builds no WinUI object, so it asks for
        // no runtime.
        string suite = Read("Tst", "MatPlotLibNet.Uno", "MatPlotLibNet.Uno.Tests.csproj");

        Assert.Contains(
            "<WindowsAppSdkBootstrapInitialize>false</WindowsAppSdkBootstrapInitialize>", suite, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryProjectPublishBuildsByName_IsBuiltByCiToo()
    {
        // publish.yml builds the platform packages by name; the CI filter builds the rest. A project only the publish
        // workflow builds is validated for the first time on a release.
        var named = Regex.Matches(Read(".github", "workflows", "publish.yml"), @"dotnet (?:build|pack) (Src/\S+\.csproj)")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal);
        string ci = Read(".github", "workflows", "ci.yml");

        var unjudged = named
            .Where(path => !CiFilterProjects().Contains(path, StringComparer.Ordinal)
                           && !ci.Contains("dotnet build " + path, StringComparison.Ordinal))
            .ToArray();

        Assert.True(unjudged.Length == 0,
            $"publish.yml builds these and no job of ci.yml does: {string.Join(", ", unjudged)}");
    }

    [Fact]
    public void TheMauiSuite_IsInTheCiFilter_SoTheLinuxRunnerRunsItToo()
    {
        // The suite compiles the package's sources into a plain net10.0 assembly, so it needs no workload and runs on
        // the Linux job like the others; the Windows job runs it again against the real package build.
        Assert.Contains(MauiSuite, CiFilterTestProjects());
        Assert.Contains(MauiSuite, TestProjectsRunWithoutBuilding("ci.yml"));
    }

    [Fact]
    public void TheReleaseWorkflow_FiresForTheTagsThisRepoActuallyUses()
    {
        // Measured 2026-09-11: tags 1.14.3 and 1.14.4 carry no `v`, so a `v*`-only trigger skipped them — the
        // releases were created by hand and the announcement steps never ran.
        string release = Read(".github", "workflows", "release.yml");
        var patterns = Regex.Matches(release, @"^\s+- '([^']+)'", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).ToArray();

        Assert.Contains("v*", patterns);
        Assert.Contains(patterns, p => p.StartsWith('[') || char.IsDigit(p[0]));
    }

    /// <summary>Packages the API site cannot document, each with the reason. docfx runs on the Linux Pages
    /// runner, which has no MAUI workload — the metadata step would fail the whole site for one package.</summary>
    private static readonly IReadOnlyDictionary<string, string> NotOnTheApiSite = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Src/MatPlotLibNet.Maui/MatPlotLibNet.Maui.csproj"] = "needs the MAUI workload, which the Pages runner has not got",
    };

    [Fact]
    public void EveryCookbookPage_IsInTheTableOfContents()
    {
        // docfx builds the site from the TOC, so a page the TOC does not list is a 404 behind a link the index
        // already carries. Measured: six pages had reached the index and never the TOC, the newest of them
        // linked from the cookbook's own front page.
        var pages = Directory.EnumerateFiles(Path.Combine(Root, "docs", "cookbook"), "*.md")
            .Select(Path.GetFileName)
            .Where(name => name != "index.md")
            .OrderBy(name => name, StringComparer.Ordinal);
        string toc = Read("docs", "cookbook", "toc.yml");

        var missing = pages.Where(name => !toc.Contains($"href: {name}", StringComparison.Ordinal)).ToArray();

        Assert.True(missing.Length == 0,
            $"These cookbook pages are published but unreachable: {string.Join(", ", missing)}");
    }

    [Fact]
    public void TheMcpReadme_CarriesTheRegistryOwnershipMarker()
    {
        // The official MCP Registry proves ownership by reading the PUBLISHED package's README for this exact
        // line. 1.15.0 shipped without it and the listing was refused until 1.15.1 carried it; nuget.org packages
        // are immutable, so a README edit that loses the line costs a release, not a commit.
        var lines = File.ReadLines(Path.Combine(Root, "Src", "MatPlotLibNet.Mcp", "README.md")).Select(l => l.Trim());

        Assert.Contains("mcp-name: io.github.xkqg/matplotlibnet", lines);
    }

    [Fact]
    public void TheAccessibilityPage_NamesEverySeriesWithNoTabularForm()
    {
        // "Every chart has a data table" is the promise the page makes, and it is not quite true: two series
        // are annotations rather than data. A reader who builds an accessible page on that promise and finds
        // nothing has been misled by a document, not by the library. The exceptions are a hand-kept list in
        // one test file; this pins the page to it, so adding a third one fails here rather than silently
        // widening the gap between what the page says and what the code does.
        string page = Read("docs", "cookbook", "accessibility.md");

        var unnamed = Models.Series.SeriesDataTableTests.WithoutATabularForm
            .Where(name => !page.Contains(name, StringComparison.Ordinal))
            .ToArray();

        Assert.True(unnamed.Length == 0,
            $"The accessibility page promises a table for every chart but never names these exceptions: {string.Join(", ", unnamed)}");
    }

    /// <summary>The documents that describe what the library is today. The changelog is left out on purpose — it
    /// records what each release added, and those numbers are meant to stay at the value they had — and so are the
    /// path manifests, which are working records rather than published text.</summary>
    private static string[] CurrentStateDocuments() =>
        [.. Directory.EnumerateFiles(Root, "*.md", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Root, file).Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal)
                        && !path.Contains("/obj/", StringComparison.Ordinal)
                        && !path.Contains("node_modules/", StringComparison.Ordinal)
                        && !path.StartsWith(".path-manifests/", StringComparison.Ordinal)
                        && path != "CHANGELOG.md")
            .OrderBy(path => path, StringComparer.Ordinal)];

    [Fact]
    public void EveryDocumentThatCountsTheColormaps_CountsWhatTheLibraryActuallyShips()
    {
        // Seven documents printed "142 colormaps" and nothing held them to it: the only check asked for "at least
        // 114", so the number could have been wrong by twenty-eight and stayed green. The registry is the source
        // of the number, and these are the documents that repeat it.
        int registered = Styling.ColorMapRegistryTests.DeclaredColorMaps().Length * 2;
        int baseMaps = registered / 2;

        foreach (string path in CurrentStateDocuments())
        {
            string text = Read(path);
            foreach (Match m in Regex.Matches(text, @"(\d+) colormaps"))
            {
                Assert.True(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) == registered,
                    $"{path} says \"{m.Value}\"; the library registers {registered}.");
            }

            foreach (Match m in Regex.Matches(text, @"(\d+) base (?:maps|colormaps)"))
            {
                Assert.True(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) == baseMaps,
                    $"{path} says \"{m.Value}\"; the library declares {baseMaps}.");
            }

            // "viridis, plasma, turbo, coolwarm and N more" — four named, the rest counted.
            foreach (Match m in Regex.Matches(text, @"coolwarm,? and (\d+) more"))
            {
                Assert.True(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) == registered - 4,
                    $"{path} names four colormaps and says \"{m.Value}\"; there are {registered - 4} others.");
            }
        }
    }

    [Fact]
    public void TheVersionTheArchitectureDocumentNamesOnItsFirstLine_IsTheVersionThePackagesCarry()
    {
        // The first line of the architecture document says "(v1.18.2)" long after the packages moved on: nothing tied
        // the hand-written number to the one the csproj files carry.
        string firstLine = Read("Src", "MatPlotLibNet", "ARCHITECTURE.md").Split('\n')[0];
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
        int heading = readme.IndexOf("## Where this is going", StringComparison.Ordinal);
        Assert.True(heading >= 0, "README.md has no \"Where this is going\" section.");

        var newest = Regex.Match(readme[heading..], @"\*\*(\d+\.\d+\.\d+)\*\*");
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

    [Fact]
    public void TheApiDocumentation_CoversEveryPackableProject()
    {
        // docfx metadata is a hand-kept file list too: a package absent from it is absent from the API site.
        string docfx = Read("docs", "docfx.json");
        var missing = PackableProjects()
            .Select(p => p.Path)
            .Where(path => !docfx.Contains(path, StringComparison.Ordinal) && !NotOnTheApiSite.ContainsKey(path))
            .ToArray();

        Assert.True(missing.Length == 0,
            $"These packages would be missing from the published API reference: {string.Join(", ", missing)}");
    }

    // ---- what reaches a search engine at all -------------------------------------------------------------------

    /// <summary>The address the site is served at, as the sitemap is built with it. One source, so a tool that
    /// writes addresses into pages cannot disagree with the sitemap that lists them.</summary>
    private static string SiteBaseUrl()
    {
        using var config = JsonDocument.Parse(Read("docs", "docfx.json"));
        return config.RootElement.GetProperty("build").GetProperty("sitemap").GetProperty("baseUrl").GetString()!;
    }

    [Fact]
    public void ThePagesWorkflow_GivesEveryBuiltPageOneAddressAndAPreviewCard()
    {
        // Measured on the live site: 0 of 766 pages carried a canonical address or a link-preview tag, and
        // neither can be switched on from docfx's own configuration — _canonicalUrlPrefix emits nothing in
        // 2.78.5 and an unknown frontmatter key is dropped in silence. So the site is finished after the build
        // instead. The step is worth nothing unless it runs and is checked, so this pins all three: the tool's
        // own cases run first, it is applied, and the result is verified before anything is uploaded.
        string workflow = Read(".github", "workflows", "pages.yml");

        Assert.Contains("tools/seo/page-metadata.py --self-test", workflow, StringComparison.Ordinal);
        Assert.Contains("tools/seo/page-metadata.py --site docs/_site", workflow, StringComparison.Ordinal);
        Assert.Contains("--apply", workflow, StringComparison.Ordinal);
        Assert.Contains("--verify", workflow, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(Root, "tools", "seo", "page-metadata.py")),
            "the workflow runs tools/seo/page-metadata.py and it is not in the repository");

        Assert.Contains($"--base-url {SiteBaseUrl()}", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePagesWorkflow_TellsIndexNowWhichPagesChanged()
    {
        // A key file nobody ever submits a URL with tells nobody anything, and that is exactly what was here:
        // the key was published, verified by its own test, and never used. This pins the other half. It has to
        // run after the deployment, because a crawler asked to fetch a page that is not live yet reads the old
        // one, and it has to derive the key from the published file rather than repeat it, because a key in two
        // places drifts and every submission made with the stale one is refused.
        string workflow = Read(".github", "workflows", "pages.yml");

        Assert.Contains("tools/seo/indexnow.py --self-test", workflow, StringComparison.Ordinal);
        Assert.Contains("tools/seo/indexnow.py", workflow, StringComparison.Ordinal);
        Assert.Contains("--key-dir docs", workflow, StringComparison.Ordinal);
        Assert.Contains("--submit", workflow, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(Root, "tools", "seo", "indexnow.py")),
            "the workflow runs tools/seo/indexnow.py and it is not in the repository");

        int deployed = workflow.IndexOf("actions/deploy-pages", StringComparison.Ordinal);
        int submitted = workflow.IndexOf("tools/seo/indexnow.py --base-url", StringComparison.Ordinal);
        Assert.True(deployed >= 0 && submitted > deployed,
            "IndexNow is told about the pages before they are deployed, so a crawler would read the old ones");

        var key = Directory.EnumerateFiles(Path.Combine(Root, "docs"), "*.txt")
            .Select(Path.GetFileNameWithoutExtension)
            .Single(name => name is { Length: 32 } && name.All(Uri.IsHexDigit));

        Assert.DoesNotContain(key!, workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSocialCard_IsTheSizeALinkPreviewAsksFor()
    {
        // Every page points its preview at one image. A link posted in a chat, a forum or an issue is rendered
        // from it, and a preview that is too small is dropped and the link shows as bare text. 1200x630 is the
        // size every renderer accepts as the wide card; anything under 600x315 is refused outright.
        byte[] png = File.ReadAllBytes(Path.Combine(Root, "images", "social-card.png"));

        Assert.Equal<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], png[..8]);

        int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];

        Assert.Equal(1200, width);
        Assert.Equal(630, height);
        Assert.True(png.Length < 5 * 1024 * 1024, $"the card is {png.Length} bytes; a preview is capped at 5 MB");
    }

    /// <summary>The receiver each example in the translation table is written on, and the type it stands for.</summary>
    private static Type ReceiverType(string receiver) => receiver switch
    {
        "ax" => typeof(AxesBuilder),
        "figure" => typeof(FigureBuilder),
        _ => typeof(Plt).Assembly.GetExportedTypes().SingleOrDefault(t => t.Name == receiver)
             ?? throw new Xunit.Sdk.XunitException(
                 $"the translation table writes \"{receiver}.\", which is neither a known receiver nor a public type"),
    };

    /// <summary>Whether the library declares a public extension method named <paramref name="member"/> on
    /// <paramref name="receiver"/>. A verb such as <c>ax.Pareto</c> is called on the axes like any member of
    /// <see cref="AxesBuilder"/>, though it lives in a family class of its own (<c>CategoricalAxesExtensions</c>),
    /// so the table may name it as a reader writes it.</summary>
    private static bool IsExtensionOf(Type receiver, string member) =>
        typeof(Plt).Assembly.GetExportedTypes()
            .Where(t => t.IsAbstract && t.IsSealed)
            .SelectMany(t => t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .Any(m => m.Name == member
                      && m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false)
                      && m.GetParameters() is [var first, ..]
                      && first.ParameterType == receiver);

    [Fact]
    public void TheMatplotlibTranslationPage_NamesOnlyCallsThisLibraryHas()
    {
        // The page exists because the words people search for are matplotlib's, not this library's: they know
        // plt.plot and want the line that draws it in C#. That only works while every line in it compiles, and
        // a table of code that nothing builds rots on the first rename. This reads the table back and asks the
        // assembly whether each call is real.
        string page = Read("docs", "cookbook", "matplotlib-to-csharp.md");
        var rows = Regex.Matches(page, @"^\|(?<cells>.+)\|\s*$", RegexOptions.Multiline);

        int checkedCalls = 0;
        foreach (Match row in rows)
        {
            var cells = row.Groups["cells"].Value.Split('|');
            if (cells.Length < 3 || cells[1].Trim().StartsWith("---", StringComparison.Ordinal)) { continue; }

            var span = Regex.Match(cells[1], @"`(?<code>[^`]+)`");
            if (!span.Success) { continue; }

            var call = Regex.Match(span.Groups["code"].Value.Trim(), @"^(?<receiver>[A-Za-z_]\w*)\.(?<member>[A-Za-z_]\w*)");
            Assert.True(call.Success,
                $"this row's C# is not a call on a receiver: \"{span.Groups["code"].Value}\"");

            var type = ReceiverType(call.Groups["receiver"].Value);
            string member = call.Groups["member"].Value;

            Assert.True(type.GetMember(member).Length > 0 || IsExtensionOf(type, member),
                $"the table says {call.Groups["receiver"].Value}.{member}, and {type.Name} has no such member");
            checkedCalls++;
        }

        Assert.True(checkedCalls >= 40,
            $"the translation table checks {checkedCalls} calls; it is meant to cover the pyplot surface people search for");
    }

    // ---- the old Y axis shape is a phase-out: obsolete, naming what replaces it, and named by no document -----------

    private static MemberInfo CoreMember(Type declaring, string name) =>
        declaring.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Single();

    /// <summary>A published type of the core package, found by name: the old types are obsolete, and a <c>typeof</c> of
    /// one in this file would be the very warning the attribute exists to raise.</summary>
    private static Type CoreType(string fullName) => typeof(Plt).Assembly.GetType(fullName, throwOnError: true)!;

    /// <summary>The Y axis members 1.23.0 published that the container replaced, each with the members of the container
    /// that replace it. The exact list: a member that is not here is not part of the phase-out, and a member here that
    /// is not obsolete (or does not say what to use instead) is a defect. They are found by name, never by
    /// <c>nameof</c>, so this file does not call them either.</summary>
    private static OldYAxisMember[] OldYAxisMembers()
    {
        var builder = typeof(AxesBuilder);
        var axes = typeof(Axes);
        var verbs = typeof(AxesXYVerbs);
        MemberInfo addRight = CoreMember(builder, nameof(AxesBuilder.WithAddRightYAxis));
        MemberInfo addLeft = CoreMember(builder, nameof(AxesBuilder.WithAddLeftYAxis));
        MemberInfo yAxes = CoreMember(axes, nameof(Axes.YAxes));
        MemberInfo right = CoreMember(typeof(YAxisContainer), nameof(YAxisContainer.Right));
        MemberInfo left = CoreMember(typeof(YAxisContainer), nameof(YAxisContainer.Left));
        MemberInfo firstOrAdd = CoreMember(typeof(YAxisStack), nameof(YAxisStack.FirstOrAdd));
        MemberInfo add = CoreMember(typeof(YAxisStack), nameof(YAxisStack.Add));
        MemberInfo addSeries = CoreMember(typeof(YAxisSlot), nameof(YAxisSlot.AddSeries));

        return
        [
            new(CoreMember(builder, "WithSecondaryYAxis"), [addRight]),
            new(CoreMember(builder, "WithOutwardYAxis"), [addRight, addLeft]),
            new(CoreType("MatPlotLibNet.SecondaryAxisBuilder"), [typeof(YAxisSlotBuilder)]),
            new(CoreType("MatPlotLibNet.OutwardYAxisBuilder"), [typeof(YAxisSlotBuilder)]),
            new(CoreMember(axes, "SecondaryYAxis"), [yAxes, right]),
            new(CoreMember(axes, "SecondarySeries"), [yAxes, right]),
            new(CoreMember(axes, "TwinX"), [yAxes, firstOrAdd]),
            new(CoreMember(axes, "AddSecondarySeries"), [firstOrAdd, addSeries]),
            new(CoreMember(axes, "AddOutwardYAxis"), [yAxes, right, left, add]),
            new(CoreMember(axes, "RightYAxes"), [yAxes, right]),
            new(CoreMember(axes, "LeftYAxes"), [yAxes, left]),
            new(CoreType("MatPlotLibNet.Models.SecondaryAxisSlot"), [typeof(YAxisSlot)]),
            new(CoreMember(verbs, "PlotSecondary"), [addRight, firstOrAdd, addSeries]),
            new(CoreMember(verbs, "ScatterSecondary"), [addRight, firstOrAdd, addSeries]),
        ];
    }

    /// <summary>A name in a line of text that is one of the old members' names as a whole word, case sensitive:
    /// <c>XSecondarySeries</c>, <c>SecondaryAxisSlotDto</c> and the JSON field <c>leftYAxes</c> are other names.</summary>
    private static Regex ObsoleteYAxisName() =>
        new(@"(?<![A-Za-z0-9_])(?:" + string.Join('|', OldYAxisMembers().Select(m => Regex.Escape(m.Old.Name)).Distinct())
            + @")(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);

    [Fact]
    public void EveryOldYAxisMember_IsObsolete_NamingItsReplacement()
    {
        // The container replaced the verbs, builders, accessors and types 1.23.0 published, and they stay published so
        // old code compiles. Staying is only half the phase-out: without [Obsolete] a reader never learns the shorter
        // form exists, and an [Obsolete] that says only "obsolete" sends them to the changelog to find out which one.
        // A warning, not an error: an error would break the code the members are kept for.
        var problems = new List<string>();
        foreach (var (old, replacements) in OldYAxisMembers())
        {
            string who = old.DeclaringType is { } owner ? $"{owner.Name}.{old.Name}" : old.Name;
            var obsolete = old.GetCustomAttribute<ObsoleteAttribute>(inherit: false);
            if (obsolete is null) { problems.Add($"{who} carries no [Obsolete]"); continue; }

            if (obsolete.IsError) { problems.Add($"{who} is an error, not a warning"); }
            if (string.IsNullOrWhiteSpace(obsolete.Message)) { problems.Add($"{who} is obsolete and says nothing"); continue; }

            foreach (var replacement in replacements.Where(r => !Regex.IsMatch(obsolete.Message, $@"(?<![A-Za-z0-9_]){Regex.Escape(r.Name)}(?![A-Za-z0-9_])")))
            {
                problems.Add($"{who} does not name {replacement.Name}: \"{obsolete.Message}\"");
            }
        }

        Assert.True(problems.Count == 0, "These old Y axis members do not tell a reader what replaces them:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void TheMatplotlibTranslationPage_NamesNoObsoleteMember()
    {
        // The page is where a matplotlib user looks for the C# of what they know. Naming a member that is on its way out
        // there teaches the old shape to exactly the readers who have none to unlearn, and twinx is the row that did.
        string page = Read("docs", "cookbook", "matplotlib-to-csharp.md");
        var offenders = ObsoleteYAxisName().Matches(page).Select(m => m.Value).Distinct().ToArray();

        Assert.True(offenders.Length == 0,
            $"The matplotlib translation page names obsolete members: {string.Join(", ", offenders)}");

        var twinx = Regex.Matches(page, @"^\|(?<cells>.+)\|\s*$", RegexOptions.Multiline)
            .Select(row => row.Groups["cells"].Value.Split('|'))
            .Single(cells => cells.Length >= 2 && cells[0].Contains("ax.twinx()", StringComparison.Ordinal));
        Assert.Contains("WithAddRightYAxis", twinx[1], StringComparison.Ordinal);
    }

    /// <summary>The documents that teach the library: every authored markdown file except the records that keep what
    /// each release added or the design it was built to (<c>CHANGELOG.md</c>, <c>ROADMAP.md</c>, <c>ARCHITECTURE.md</c>),
    /// the design records under <c>docs/contrib</c>, and the generated API pages and benchmark reports.</summary>
    private static string[] AuthoredDocuments() =>
        [.. CurrentStateDocuments().Where(path =>
            !path.StartsWith("docs/api/", StringComparison.Ordinal)
            && !path.StartsWith("docs/contrib/", StringComparison.Ordinal)
            && !path.StartsWith(".claude/", StringComparison.Ordinal)
            && !path.Contains("BenchmarkDotNet.Artifacts/", StringComparison.Ordinal)
            && path != "ROADMAP.md"
            && path != "Src/MatPlotLibNet/ARCHITECTURE.md")];

    [Fact]
    public void NoAuthoredDocument_NamesAnObsoleteMember()
    {
        // A reader copies what a document shows. 94 samples had drifted off the API before they were swept, and a
        // document that keeps showing the old Y axis verbs would put the phase-out back one copy-paste at a time. The
        // records are left out on purpose: a changelog line says what 1.23.0 added under the name it had.
        var documents = AuthoredDocuments();
        var name = ObsoleteYAxisName();

        // A scan that finds nothing proves nothing: the pattern must see a name where one stands, and the list of
        // documents must hold the front door and a package page, or a path bug would read green.
        Assert.Matches(name, "ax.TwinX()");
        Assert.DoesNotMatch(name, "axes.XSecondarySeries and the secondaryYAxis field");
        Assert.Contains("README.md", documents);
        Assert.Contains("docs/cookbook/streaming.md", documents);
        Assert.Contains("Src/MatPlotLibNet.Mcp/README.md", documents);

        var offending = new List<string>();
        foreach (string path in documents)
        {
            var lines = Read(path).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (Match hit in name.Matches(lines[i]))
                {
                    offending.Add($"{path}:{i + 1} names {hit.Value}");
                }
            }
        }

        Assert.True(offending.Count == 0,
            "These documents name an obsolete Y axis member; use WithAddRightYAxis, WithAddLeftYAxis and Axes.YAxes:\n"
            + string.Join("\n", offending));
    }

    [Fact]
    public void ThePublishWorkflow_CannotReportSuccessWhileItPushedNothing()
    {
        // The release is the one run nobody watches closely, and the push step was written so that it reads
        // the same whether it pushed every package or none: the loop body simply does not run on an empty
        // directory, and a failure partway through leaves the step's verdict to whichever push happened to be
        // last. A pack step that quietly produces nothing would then look exactly like a release that went out.
        // The step now names the count, refuses an empty directory, and checks each push where it happens.
        string workflow = Read(".github", "workflows", "publish.yml");

        Assert.Contains("$pkgs.Count -eq 0", workflow, StringComparison.Ordinal);
        Assert.Contains("if ($LASTEXITCODE -ne 0)", workflow, StringComparison.Ordinal);

        // The Linux job pushes with one glob, which reports nothing to push as an error of its own -- but only
        // if something checks that the glob matched at all.
        Assert.Contains("count=$(ls nupkgs/*.nupkg", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void TheWpfReadme_SaysWhichWindowsPlatformVersionAProjectMustTarget()
    {
        // MatPlotLibNet.Wpf targets net8.0-windows10.0.19041.0 because SkiaSharp.Views.WPF ships no asset below
        // that platform version. A project on a bare netX.0-windows (platform version 7.0) cannot restore the
        // package (NU1202), and issue 21 had to ask what the floor was and who imposed it. The csproj is where
        // the floor is set; the README is where a consumer looks first, so it must quote the csproj's own value.
        string target = XDocument.Load(Path.Combine(Root, "Src", "MatPlotLibNet.Wpf", "MatPlotLibNet.Wpf.csproj"))
            .Descendants("TargetFramework").Single().Value.Trim();
        string platformVersion = Regex.Match(target, @"windows(?<v>[\d.]+)$").Groups["v"].Value;
        string readme = Read("Src", "MatPlotLibNet.Wpf", "README.md");

        Assert.NotEqual(string.Empty, platformVersion);
        Assert.Contains(platformVersion, readme, StringComparison.Ordinal);
        Assert.Contains("SkiaSharp.Views.WPF", readme, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Dpi")]
    [InlineData("Title")]
    [InlineData("Author")]
    public void TheSkiaReadme_NamesEachSaveSettingThePngAndPdfExportsRead(string setting)
    {
        // PngTransform reads Dpi, Title and Author of the SaveOptions it is handed, PdfTransform reads Title and Author.
        // The README ships inside the package, and it is where a consumer of the PNG and PDF export looks for what a
        // save can set; a setting the export reads and the README does not name is one nobody finds.
        Assert.NotNull(typeof(SaveOptions).GetProperty(setting));
        Assert.Contains($"{setting} = ", Read("Src", "MatPlotLibNet.Skia", "README.md"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheMcpReadme_SaysASpecsDpiSizesThePngInsideTheCanvasCeiling()
    {
        // The spec's dpi sizes the PNG, so the 4,000 by 4,000 pixel ceiling holds for the picture it draws. The README
        // ships inside the package; the tool descriptions say it to a model, this says it to the person who installs it.
        string readme = Read("Src", "MatPlotLibNet.Mcp", "README.md");

        Assert.Contains("`dpi`", readme, StringComparison.Ordinal);
        Assert.Contains("ceiling", readme, StringComparison.Ordinal);
        Assert.Contains("PNG", readme[readme.IndexOf("`dpi`", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }
}

/// <summary>An old Y axis member and the members of the container that replace it.</summary>
/// <param name="Old">The old type or member, as published.</param>
/// <param name="Replacements">The members (or types) its obsolete message has to name.</param>
internal readonly record struct OldYAxisMember(MemberInfo Old, MemberInfo[] Replacements);

/// <summary>A suite named together with the workflow it is said of.</summary>
/// <param name="Workflow">The workflow's file name.</param>
/// <param name="Suite">The suite's project path.</param>
internal readonly record struct WorkflowSuite(string Workflow, string Suite);

/// <summary>One job of a workflow.</summary>
/// <param name="Name">The workflow's file name and the job's name, as <c>ci.yml:build-and-test</c>.</param>
/// <param name="Text">The job's text, up to the next job.</param>
internal readonly record struct WorkflowJob(string Name, string Text);
