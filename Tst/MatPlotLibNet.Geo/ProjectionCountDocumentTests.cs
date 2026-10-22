// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The number of map projections is a published claim: the home page, the API index, the cookbook, the README and
/// the package description all print it, and nothing held them to the code, so one of them said 11 while the package
/// shipped 13. The projections are counted off the assembly (every concrete class that is an
/// <see cref="IGeoProjection"/>), and every current-state document that counts them is held to the number.</summary>
public class ProjectionCountDocumentTests
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

    /// <summary>What the Geo package ships: every public, concrete projection.</summary>
    private static int ShippedProjections() =>
        typeof(IGeoProjection).Assembly.GetTypes()
            .Count(type => type is { IsClass: true, IsAbstract: false, IsPublic: true } && typeof(IGeoProjection).IsAssignableFrom(type));

    private static readonly Regex Claim = new(@"\b(\d+) (?:map |geographic )?projections\b", RegexOptions.Compiled);

    /// <summary>The documents that describe what the library is today: every Markdown file but the changelog (a record of
    /// what each release added) and the path manifests (working records).</summary>
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
    public void TheProjectionsAreFound()
    {
        Assert.Equal(13, ShippedProjections());
    }

    [Fact]
    public void EveryDocumentThatCountsTheProjections_CountsWhatThePackageShips()
    {
        int shipped = ShippedProjections();
        var wrong = new List<string>();
        int stating = 0;

        foreach (string path in CurrentStateDocuments())
        {
            foreach (Match match in Claim.Matches(File.ReadAllText(Path.Combine(Root, path))))
            {
                stating++;
                if (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) != shipped)
                {
                    wrong.Add($"{path} says \"{match.Value}\"");
                }
            }
        }

        Assert.True(stating > 0, "No document states a projection count, so this test measured nothing.");
        Assert.True(wrong.Count == 0, $"The Geo package ships {shipped} projections; {string.Join("; ", wrong)}.");
    }

    [Fact]
    public void ThePackageDescription_CountsWhatThePackageShips()
    {
        string description = XDocument.Load(Path.Combine(Root, "Src", "MatPlotLibNet.Geo", "MatPlotLibNet.Geo.csproj"))
            .Descendants("Description").First().Value;

        var claim = Claim.Match(description);

        Assert.True(claim.Success, "The Geo package description states no projection count.");
        Assert.Equal(ShippedProjections(), int.Parse(claim.Groups[1].Value, CultureInfo.InvariantCulture));
    }
}
