// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The files and projects of the repository a release test reads, and the list of the packages a release carries: the reads the version, the
/// packaging and the document tests share, so none of them keeps a copy.</summary>
internal static class ReleaseFiles
{
    internal static readonly string Root = RepositoryRoot.Folder;

    internal static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));

    /// <summary>Every packable project under Src/ — the ones that become a NuGet package.</summary>
    internal static (string Path, XDocument Xml)[] PackableProjects() =>
        [.. RepositoryTree.Files("Src", "*.csproj")
            .Select(file => (Path: file.Path, Xml: file.Text.Xml()))
            .Where(p => p.Xml.Descendants("PackageId").Any())
            .OrderBy(p => p.Path, StringComparer.Ordinal)];

    internal static string VersionOf(XDocument csproj) =>
        csproj.Descendants("Version").FirstOrDefault()?.Value.Trim()
        ?? throw new InvalidOperationException("no <Version>");

    // ---- packages that are finished, and say so ---------------------------------------------------------------

    /// <summary>Packages that are deliberately standing still: they keep the version they were last released at,
    /// nothing builds them, nothing publishes them, and nothing measures them. A frozen package is not a forgotten
    /// one, and the difference has to be written down or six months from now nobody can tell them apart — which is
    /// why every test that would otherwise go red for a frozen package reads this list and says the reason.</summary>
    internal static readonly IReadOnlyDictionary<string, string> Frozen = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Src/MatPlotLibNet.Notebooks/MatPlotLibNet.Notebooks.csproj"] =
            "it renders into Polyglot Notebooks, whose runtime Microsoft has ended; MatPlotLibNet.Verso is the successor",
    };

    internal static (string Path, XDocument Xml)[] LivePackages() =>
        [.. PackableProjects().Where(p => !Frozen.ContainsKey(p.Path))];

    // ---- the projects a box without a workload can build ---------------------------------------------------------

    /// <summary>Projects of the solution that cannot be built without a workload, with the reason. <c>dotnet build</c> of the whole solution
    /// stops on such a project at NETSDK1147 before it compiles any other, so a filter on that one error reads a tree with errors as clean;
    /// the workload-free solution filter holds every other project of the solution.</summary>
    internal static readonly IReadOnlyDictionary<string, string> NeedsWorkload = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Src/MatPlotLibNet.Maui/MatPlotLibNet.Maui.csproj"] =
            "its target frameworks are android, ios and maccatalyst, which need the maui workload; the windows-heads job of ci.yml builds it",
    };

    internal const string WorkloadFreeFilter = "MatPlotLibNet.NoMaui.slnf";

    /// <summary>Every project a solution filter carries, as repo-relative paths with forward slashes.</summary>
    internal static string[] FilterProjects(string filter)
    {
        using var doc = JsonDocument.Parse(Read(filter));
        return [.. doc.RootElement.GetProperty("solution").GetProperty("projects")
            .EnumerateArray().Select(p => p.GetString()!.Replace('\\', '/'))];
    }

    /// <summary>Every project the solution holds, as repo-relative paths with forward slashes.</summary>
    internal static string[] SolutionProjects() =>
        [.. Read("MatPlotLibNet.slnx").Xml().Descendants("Project")
            .Select(p => p.Attribute("Path")!.Value.Replace('\\', '/'))];

    // ---- A package version written once: SourceLink, System.Numerics.Tensors ------------------------------------

    /// <summary>Every project file the repository builds: the packages, their suites, the samples and the benchmarks.</summary>
    internal static SourceFile[] RepositoryProjects() =>
        [.. new[] { "Src", "Tst", "Samples", "Benchmarks" }
            .SelectMany(folder => RepositoryTree.Files(folder, "*.csproj"))
            .OrderBy(file => file.Path, StringComparer.Ordinal)];
}
