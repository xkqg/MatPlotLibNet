// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>
/// A release is carried by six hand-kept lists — the CI solution filter, two workflow test blocks, two coverage
/// runners and the version in every csproj — and nothing checks that they agree. They already had drifted:
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

    // ---- the version, in every place it is written ----------------------------------------------------------

    [Fact]
    public void EveryPackableProject_CarriesTheSameVersion()
    {
        var byVersion = PackableProjects()
            .GroupBy(p => VersionOf(p.Xml), StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ToArray();

        Assert.True(byVersion.Length == 1,
            "A release bumps every package together, or the odd one out ships an old number under a new tag. Found: "
            + string.Join(" | ", byVersion.Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.Path))}")));
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

    [Theory]
    [InlineData("ci.yml")]
    [InlineData("publish.yml")]
    public void EveryTestProjectInTheCiFilter_IsRunByTheWorkflow(string workflow)
    {
        var run = TestProjectsRunBy(workflow);
        var missing = CiFilterTestProjects().Except(run, StringComparer.Ordinal).ToArray();

        Assert.True(missing.Length == 0,
            $"{workflow} builds these suites and never runs them: {string.Join(", ", missing)}");
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
        // and Uno has its own workflow because its SDK has no dotnet workload.
        var inFilter = CiFilterProjects();
        string publish = Read(".github", "workflows", "publish.yml");
        string uno = Read(".github", "workflows", "push-uno.yml");

        var unpublished = PackableProjects()
            .Select(p => p.Path)
            .Where(path => !inFilter.Contains(path, StringComparer.Ordinal)
                           && !publish.Contains(path, StringComparison.Ordinal)
                           && !uno.Contains(path, StringComparison.Ordinal))
            .ToArray();

        Assert.True(unpublished.Length == 0,
            $"These packages have a PackageId and nothing ever pushes them: {string.Join(", ", unpublished)}");
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
}
