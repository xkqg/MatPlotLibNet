// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
