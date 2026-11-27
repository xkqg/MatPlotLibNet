// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The number of map projections is a published claim: the home page, the API index, the cookbook, the README and
/// the package description all print it, and nothing held them to the code, so one of them said 11 while the package
/// shipped 13. The projections are counted off the assembly (every concrete class that is an
/// <see cref="IGeoProjection"/>), and every current-state document that counts them is held to the number.</summary>
public class ProjectionCountDocumentTests
{
    private static readonly string Root = RepositoryRoot.Folder;

    /// <summary>What the Geo package ships: every public, concrete projection.</summary>
    private static int ShippedProjections() =>
        typeof(IGeoProjection).Assembly.ConcretePublicSubtypesOf(typeof(IGeoProjection)).Length;

    private static readonly Regex Claim = new(@"\b(\d+) (?:map |geographic )?projections\b", RegexOptions.Compiled);

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

        foreach (var (path, text) in RepositoryTree.CurrentStateDocuments())
        {
            foreach (Match match in Claim.Matches(text))
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
