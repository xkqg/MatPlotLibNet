// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The cookbook page of the server says which chart types a spec cannot name. That list is the catalog's, so
/// a type the catalog starts refusing, or stops refusing, is a sentence on the page that has become false and nothing
/// but this notices.</summary>
public class McpDocumentTests
{
    private static readonly string[] CountWords =
        ["No", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve"];

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

    private static string CookbookPage =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", "cookbook", "mcp.md")).Replace("\r\n", "\n", StringComparison.Ordinal);

    [Fact]
    public void TheCookbookPage_NamesEveryTypeTheCatalogRefuses_AndSaysHowManyThereAre()
    {
        var excluded = new ChartTypeCatalog().Excluded;
        string page = CookbookPage;

        Assert.Contains($"{CountWords[excluded.Count]} chart types are not available through the spec.", page);
        foreach (string type in excluded.Keys)
        {
            Assert.True(page.Contains($"`{type}`", StringComparison.Ordinal),
                $"The cookbook page docs/cookbook/mcp.md does not name '{type}', which the server refuses.");
        }
    }

    [Fact]
    public void TheCookbookPage_NamesNoRefusedTypeTheCatalogLists()
    {
        var catalog = new ChartTypeCatalog();
        string section = CookbookPage.Split("chart types are not available through the spec.", 2)[1].Split("\n\n", 2)[0];

        foreach (string type in catalog.Listed)
        {
            Assert.False(section.Contains($"`{type}`", StringComparison.Ordinal),
                $"The cookbook page says '{type}' is not available, but the server lists it.");
        }
    }
}
