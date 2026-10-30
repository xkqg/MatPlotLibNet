// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.Models;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>The wiki states how many series types the library ships on its front page, in the chart-type reference, in the
/// cheatsheet and in the package map. The core suite pins that number in the assembly and holds the repository's documents
/// to it; this holds the wiki's pages to it, so a series added in the repository cannot leave the wiki a count behind.</summary>
public sealed class PublishedCountsTests
{
    private static int SeriesShipped() =>
        typeof(ChartSeries).Assembly.GetTypes()
            .Count(t => t.IsClass && !t.IsAbstract && t.IsPublic && typeof(ChartSeries).IsAssignableFrom(t));

    [Fact]
    public void EveryWikiPage_StatesTheNumberOfSeriesTheAssemblyShips()
    {
        int shipped = SeriesShipped();

        var stale = Wiki.BesideTheRepository.Pages()
            .SelectMany(page => SeriesCountPhrases.In(Wiki.BesideTheRepository.Text(page.Name))
                .Where(p => p.Count != shipped)
                .Select(p => $"{page.Name}: \"{p.Phrase}\""))
            .ToArray();

        Assert.True(stale.Length == 0,
            $"the assembly ships {shipped} series types; these wiki pages say otherwise:\n" + string.Join("\n", stale));
    }

    [Fact]
    public void TheWikiFrontPage_StatesTheSeriesCountInDigits()
    {
        // A count in words cannot be read by the test above, and the front page is where most readers meet it.
        Assert.Contains(SeriesCountPhrases.In(Wiki.BesideTheRepository.Text("Home.md")), p => p.Count == SeriesShipped());
    }
}
