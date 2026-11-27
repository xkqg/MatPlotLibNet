// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A package that reads the internals of another package (<c>InternalsVisibleTo</c>) works only with the build of it
/// that it was compiled against: an internal member is no promise, and the next release may move it, which a consumer would see
/// as a <see cref="MissingMethodException"/> at run time. Such a package says so in its project file
/// (<c>DependsOnInternals</c>), and the build writes its dependency on every package it references as exactly this version,
/// <c>[x.y.z]</c>, instead of the floor NuGet writes by default (<c>Directory.Build.targets</c>). These tests hold the list of
/// packages that say so to the internals that are actually granted.</summary>
public class InternalsDependencyContractTests
{
    private static readonly string Root = RepositoryRoot.Folder;

    /// <summary>A package of the repository: its assembly name, its project file, and the packages it references.</summary>
    private sealed record Package(string Name, XDocument Project, IReadOnlyList<string> References)
    {
        public bool IsTool => Project.Descendants("PackAsTool").Any(n => n.Value.Trim() == "true");

        public bool DependsOnInternals => Project.Descendants("DependsOnInternals").Any(n => n.Value.Trim() == "true");
    }

    private static Dictionary<string, Package> Packages()
    {
        var packages = new Dictionary<string, Package>(StringComparer.Ordinal);
        foreach (var file in RepositoryTree.Files("Src", "*.csproj"))
        {
            var project = file.Text.Xml();
            if (!project.Descendants("PackageId").Any()) continue;

            var references = project.Descendants("ProjectReference")
                .Select(r => Path.GetFileNameWithoutExtension((r.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/')))
                .ToList();
            string name = Path.GetFileNameWithoutExtension(file.Path);
            packages[name] = new Package(name, project, references);
        }

        return packages;
    }

    /// <summary>Which package reads the internals of which: every <c>InternalsVisibleTo</c> a package grants to another package
    /// (a test suite or the benchmarks is no package and is left out), in either of the two places a grant is written: an attribute
    /// of <c>Properties/AssemblyAttributes.cs</c> or an item of the project file.</summary>
    private static List<InternalsGrant> Grants(Dictionary<string, Package> packages)
    {
        var grants = new List<InternalsGrant>();
        foreach (var (granter, package) in packages)
        {
            string attributes = Path.Combine(Root, "Src", granter, "Properties", "AssemblyAttributes.cs");
            IEnumerable<string> byAttribute = File.Exists(attributes)
                ? Regex.Matches(File.ReadAllText(attributes), @"\[assembly:\s*InternalsVisibleTo\(""([^""]+)""\)\]").Select(match => match.Groups[1].Value)
                : [];
            var byItem = package.Project.Descendants("InternalsVisibleTo").Select(item => item.Attribute("Include")?.Value ?? string.Empty);

            foreach (var reader in byAttribute.Concat(byItem))
                if (packages.ContainsKey(reader))
                    grants.Add(new InternalsGrant(granter, reader));
        }

        return grants;
    }

    /// <summary>The grants are read from both places a grant is written: the core grants Geo by an attribute, Geo grants the MCP
    /// server by an item of its project file. A reader of one place alone would miss every grant of the other.</summary>
    [Fact]
    public void TheGrants_AreReadFromTheAttributes_AndFromTheProjectFiles()
    {
        var grants = Grants(Packages());

        Assert.Contains(new InternalsGrant("MatPlotLibNet", "MatPlotLibNet.Geo"), grants);
        Assert.Contains(new InternalsGrant("MatPlotLibNet.Geo", "MatPlotLibNet.Mcp"), grants);
    }

    /// <summary>A project file grants by the item, <c>&lt;InternalsVisibleTo Include="..." /&gt;</c>, the one form the SDK reads: an
    /// element of that name with the reader as its text is a property, which grants nothing (the data frame suite could not see the
    /// package's internals while its project file said so).</summary>
    [Fact]
    public void AProjectFile_GrantsByTheItem_NeverByAProperty()
    {
        var properties = RepositoryTree.Files("Src", "*.csproj")
            .Where(file => file.Text.Xml().Descendants("InternalsVisibleTo").Any(element => element.Attribute("Include") is null))
            .Select(file => Path.GetFileName(file.Path))
            .ToList();

        Assert.True(properties.Count == 0, "These project files write InternalsVisibleTo as a property, which grants nothing: " + string.Join(", ", properties));
    }

    /// <summary><paramref name="Reader"/> reads the internals <paramref name="Granter"/> grants it.</summary>
    private readonly record struct InternalsGrant(string Granter, string Reader);

    /// <summary>Whether <paramref name="from"/> reaches <paramref name="to"/> through project references whose every package on
    /// the way depends on internals, so that the dependency on <paramref name="to"/> is exact all along.</summary>
    private static bool ReachesExactly(Dictionary<string, Package> packages, string from, string to)
    {
        var package = packages[from];
        if (!package.DependsOnInternals) return false;
        return package.References.Any(r => r == to || (packages.ContainsKey(r) && ReachesExactly(packages, r, to)));
    }

    [Fact]
    public void APackageThatReadsTheInternalsOfAnother_DependsOnItAtExactlyThisVersion()
    {
        var packages = Packages();

        // A .NET tool carries the packages it uses inside it, so it has no dependency a newer build could move under it.
        var loose = Grants(packages)
            .Where(g => !packages[g.Reader].IsTool)
            .Where(g => !ReachesExactly(packages, g.Reader, g.Granter))
            .Select(g => $"{g.Reader} (reads the internals of {g.Granter})")
            .ToList();

        Assert.True(loose.Count == 0,
            "These packages read internals of a package they would depend on only as a floor; set <DependsOnInternals>true</DependsOnInternals> in each, "
            + "and in every package between it and the one it reads: " + string.Join(", ", loose));
    }

    [Fact]
    public void APackageThatSaysItDependsOnInternals_ReadsTheInternalsOfAPackageItReferences()
    {
        var packages = Packages();
        var grants = Grants(packages);

        // Needed by a package that reads internals itself, or that lies between such a package and the one whose internals it reads.
        bool Needed(Package p) => grants.Any(g => g.Reader == p.Name
            || (ReachesExactly(packages, g.Reader, p.Name)
                && (p.References.Contains(g.Granter) || ReachesExactly(packages, p.Name, g.Granter))));

        var needless = packages.Values.Where(p => p.DependsOnInternals && !Needed(p)).Select(p => p.Name).ToList();

        Assert.True(needless.Count == 0,
            "These packages pin their dependencies exactly and read no internals that need it: " + string.Join(", ", needless));
    }

    [Fact]
    public void TheBuild_WritesTheDependencyOfSuchAPackage_AsExactlyThisVersion()
    {
        var targets = XDocument.Load(Path.Combine(Root, "Directory.Build.targets"));

        var pin = targets.Descendants("Target").SingleOrDefault(t =>
            (t.Attribute("AfterTargets")?.Value ?? string.Empty).Contains("_GetProjectReferenceVersions", StringComparison.Ordinal)
            && (t.Attribute("Condition")?.Value ?? string.Empty).Contains("DependsOnInternals", StringComparison.Ordinal));

        Assert.NotNull(pin);
        Assert.Contains("[%(ProjectVersion)]", pin.Descendants("ProjectVersion").Select(v => v.Value.Trim()));
    }
}
