// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using static MatPlotLibNet.Mcp.Tests.SpecFixtures;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The cookbook page of the server says which chart types a spec cannot name. That list is the catalog's, so
/// a type the catalog starts refusing, or stops refusing, is a sentence on the page that has become false and nothing
/// but this notices.</summary>
public class McpDocumentTests
{
    private static readonly string[] CountWords =
        ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve"];

    private static string CookbookPage =>
        SourceFile.ReadFrom(Path.Combine(RepositoryRoot.Folder, "docs", "cookbook", "mcp.md")).LineFeedText;

    private static string CountSentence(ChartTypeCatalog catalog) => catalog.Excluded.Count == 0
        ? "Every chart type is available through the spec."
        : $"{CountWords[catalog.Excluded.Count - 1]} chart types are not available through the spec.";

    [Fact]
    public void TheCookbookPage_NamesEveryTypeTheCatalogRefuses_AndSaysHowManyThereAre()
    {
        var catalog = new ChartTypeCatalog();
        string page = CookbookPage;

        Assert.Contains(CountSentence(catalog), page);
        foreach (string type in catalog.Excluded.Keys)
        {
            Assert.True(page.Contains($"`{type}`", StringComparison.Ordinal),
                $"The cookbook page docs/cookbook/mcp.md does not name '{type}', which the server refuses.");
        }
    }

    /// <summary>The specs the cookbook page shows (a JSON block whose object holds <c>subPlots</c> or <c>texts</c>) are read by the
    /// server as written, a map among them: an example a model copies is one the server takes.</summary>
    [Fact]
    public void EverySpecOnTheCookbookPage_IsReadByTheServer_AMapAmongThem()
    {
        var specs = SourceFile.FencedBlocksOf(CookbookPage, "json")
            .Where(block => System.Text.Json.Nodes.JsonNode.Parse(block) is System.Text.Json.Nodes.JsonObject spec
                            && (spec.ContainsKey("subPlots") || spec.ContainsKey("texts")))
            .ToList();

        Assert.Contains(specs, spec => spec.Contains("\"geo_polygon\"", StringComparison.Ordinal));
        foreach (string spec in specs)
        {
            Assert.NotNull(Reader.Read(new ChartSpec(spec)));
        }
    }

    [Fact]
    public void TheCookbookPage_NamesNoRefusedTypeTheCatalogLists()
    {
        var catalog = new ChartTypeCatalog();
        string section = SourceFile.SectionsOf(CookbookPage).Single(candidate => candidate.Text.Contains(CountSentence(catalog), StringComparison.Ordinal)).Text;

        foreach (string type in catalog.Listed)
        {
            Assert.False(section.Contains($"`{type}`", StringComparison.Ordinal),
                $"The cookbook page says '{type}' is not available, but the server lists it.");
        }
    }
}
