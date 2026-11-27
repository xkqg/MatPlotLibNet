// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Tests.Rendering;
using static MatPlotLibNet.Tests.Packaging.ReleaseFiles;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>
/// A release is carried by hand-kept lists (the CI solution filter, the workflow test blocks, the coverage runners, the projects that pack, the
/// icon and the release notes a package carries) and nothing checked that they agree. A list that must be edited by hand is only as good as the
/// memory of whoever edits it; each test here turns one of those agreements into a failing test.
/// </summary>
public class ReleasePackagingContractTests
{
    /// <summary>Every project the CI solution filter carries, as repo-relative paths with forward slashes.</summary>
    private static string[] CiFilterProjects() => FilterProjects("MatPlotLibNet.CI.slnf");

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

    // ---- one build of everything this box can build -----------------------------------------------------------

    [Fact]
    public void TheWorkloadFreeFilter_HoldsEveryProjectOfTheSolution_ButThoseThatNeedAWorkload()
    {
        // `dotnet build MatPlotLibNet.slnx` without the maui workload stops at NETSDK1147 before it compiles anything, so a
        // filter on that error read a tree with 16 errors in Geo as clean for four rounds. One filter holds the rest.
        string[] solution = SolutionProjects();
        string[] held = FilterProjects(WorkloadFreeFilter);

        string[] missing = [.. solution.Except(NeedsWorkload.Keys).Except(held, StringComparer.Ordinal)];
        string[] foreign = [.. held.Except(solution, StringComparer.Ordinal)];
        string[] kept = [.. held.Intersect(NeedsWorkload.Keys, StringComparer.Ordinal)];
        string[] unknown = [.. NeedsWorkload.Keys.Except(solution, StringComparer.Ordinal)];

        Assert.True(missing.Length == 0, $"{WorkloadFreeFilter} lacks projects of the solution that need no workload: {string.Join(", ", missing)}");
        Assert.True(foreign.Length == 0, $"{WorkloadFreeFilter} holds projects the solution does not: {string.Join(", ", foreign)}");
        Assert.True(kept.Length == 0, $"{WorkloadFreeFilter} holds projects that need a workload: {string.Join(", ", kept)}");
        Assert.True(unknown.Length == 0, $"ReleaseFiles.NeedsWorkload names projects the solution does not hold: {string.Join(", ", unknown)}");

        using var doc = JsonDocument.Parse(Read(WorkloadFreeFilter));
        Assert.Equal("MatPlotLibNet.slnx", doc.RootElement.GetProperty("solution").GetProperty("path").GetString());
    }

    [Fact]
    public void EveryProjectThatNeedsAWorkload_TargetsAPlatformFramework()
    {
        // The exception list is not a place to park a project that merely fails to build: a project belongs on it only while its
        // target frameworks are platform ones.
        string[] plain = [.. NeedsWorkload.Keys.Where(path => !Regex.IsMatch(Read(path.Split('/')), @"<TargetFrameworks?>[^<]*-(android|ios|maccatalyst)"))];

        Assert.True(plain.Length == 0, $"These projects are listed as needing a workload and target no android, ios or maccatalyst framework: {string.Join(", ", plain)}");
    }

    [Fact]
    public void ThePushGate_BuildsTheWorkloadFreeFilter_AndNeverTheBareSolution()
    {
        IReadOnlyList<string> lines = RepositoryTree.FileAt("tools", "prepush.ps1").CodeLines;

        Assert.Contains(lines, line => line.Contains("Assert-CleanBuild", StringComparison.Ordinal) && line.Contains(WorkloadFreeFilter, StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains("MatPlotLibNet.slnx", StringComparison.Ordinal));
    }

    [Fact]
    public void TheContributorGuide_NamesTheWorkloadFreeFilter_WhereItListsTheCommands()
    {
        Assert.Contains(WorkloadFreeFilter, Read("CLAUDE.md"), StringComparison.Ordinal);
        Assert.Contains(WorkloadFreeFilter, Read("CONTRIBUTING.md"), StringComparison.Ordinal);
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
            .Where(file => file.Text.Xml().Descendants()
                .Where(n => n.Name.LocalName is "PackageReference" or "PackageVersion")
                .Select(n => (string?)n.Attribute("Include") ?? (string?)n.Attribute("Update") ?? string.Empty)
                .Any(id => id.StartsWith("coverlet.", StringComparison.OrdinalIgnoreCase)))
            .Select(file => file.Path)
            .ToArray();

        Assert.True(carriers.Length == 0,
            "These projects reference a coverlet package that nothing in the coverage pipeline triggers "
            + "(dotnet-coverage measures them): " + string.Join(", ", carriers));
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
        var rules = RepositoryTree.FileAt(".gitignore").Lines.Select(l => l.Trim()).ToArray();

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
        var headings = SourceFile.SectionsOf(Read("CHANGELOG.md"))
            .Select(section => Regex.Replace(section.Heading.ToLowerInvariant(), @"[^a-z0-9 -]", string.Empty).Trim().Replace(' ', '-'))
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
        var jobs = SourceFile.YamlChildrenOf(RepositoryTree.FileAt(".github", "workflows", workflow).Text, "jobs");

        return [.. jobs.Select(job => new WorkflowJob(workflow + ":" + job[..job.IndexOf(':')].Trim(), job))];
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

            var step = SourceFile.YamlChildrenOf(job.Text, "steps").Single(candidate => candidate.Contains(check, StringComparison.Ordinal));

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
}

/// <summary>A suite named together with the workflow it is said of.</summary>
/// <param name="Workflow">The workflow's file name.</param>
/// <param name="Suite">The suite's project path.</param>
internal readonly record struct WorkflowSuite(string Workflow, string Suite);

/// <summary>One job of a workflow.</summary>
/// <param name="Name">The workflow's file name and the job's name, as <c>ci.yml:build-and-test</c>.</param>
/// <param name="Text">The job's text, up to the next job.</param>
internal readonly record struct WorkflowJob(string Name, string Text);
