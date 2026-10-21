// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The roadmap is the wiki's page: one table of what is done and what is still to come, in the order the work is done,
/// and a section for every row. The repository carries the same table, so a reader of the code sees it without leaving
/// it. Two copies of one table drift apart, so the repository's is held to the wiki's row for row, and the page is held
/// to the order it claims: every row says Done or Later, and no row still to come stands above a row that is done.
/// </summary>
public sealed partial class RoadmapTests
{
    private const string PageName = "Roadmap.md";
    private const string Header = "| | What it is | In one line |";
    private const string WikiAddress = "https://github.com/xkqg/MatPlotLibNet/wiki/";
    private const string Done = "| **Done** |";
    private const string Later = "| **Later** |";

    [GeneratedRegex(@"\[\[(?<page>[^\]|#]+)(?:[|#][^\]]*)?\]\]")]
    private static partial Regex WikiLink();

    [Fact]
    public void TheWiki_HasARoadmapPage_ThatTheHomePageAndTheSidebarLeadTo()
    {
        Assert.True(File.Exists(Path.Join(Wiki.BesideTheRepository.Folder, PageName)), $"The wiki has no {PageName}.");
        Assert.Contains("[[Roadmap]]", Wiki.BesideTheRepository.Text("Home.md"), StringComparison.Ordinal);
        Assert.Contains("[[Roadmap]]", Wiki.BesideTheRepository.Text("_Sidebar.md"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheReadme_LeadsToTheRepositorysRoadmap_AndToTheWikisPage()
    {
        var readme = File.ReadAllText(Path.Join(Repository.Root, "README.md"));

        Assert.Contains("(ROADMAP.md)", readme, StringComparison.Ordinal);
        Assert.Contains(WikiAddress + "Roadmap", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRepositorysRoadmap_HoldsTheWikisTable_RowForRow_AndSendsTheReaderThereForTheRest()
    {
        var path = Path.Join(Repository.Root, "ROADMAP.md");

        Assert.True(File.Exists(path), "The repository has no ROADMAP.md: the copy of the wiki's roadmap table a reader of the code sees.");

        var here = File.ReadAllText(path).ReplaceLineEndings("\n");
        string[] expected = [.. Rows(Page()).Select(row => WikiLink().Replace(row, link =>
        {
            var page = link.Groups["page"].Value;
            return $"[{page}]({WikiAddress}{page.Replace(' ', '-')})";
        }))];

        Assert.Equal(expected, Rows(here));
        Assert.Contains(WikiAddress + "Roadmap", here, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryRowOfTheTable_SaysDoneOrLater()
    {
        var neither = Rows(Page())
            .Where(row => !row.StartsWith(Done, StringComparison.Ordinal) && !row.StartsWith(Later, StringComparison.Ordinal))
            .ToArray();

        Assert.True(neither.Length == 0, "Rows of the roadmap that say neither Done nor Later: " + string.Join(" / ", neither));
    }

    [Fact]
    public void NoRowStillToCome_StandsAboveARowThatIsDone()
    {
        // The roadmap says nothing stands above a row it needs, so what is done stands above what is still to come: a
        // Later row above a Done row would say a release was built on something that does not exist yet.
        var rows = Rows(Page());
        var lastDone = Array.FindLastIndex(rows, row => row.StartsWith(Done, StringComparison.Ordinal));
        var firstLater = Array.FindIndex(rows, row => row.StartsWith(Later, StringComparison.Ordinal));

        Assert.True(lastDone >= 0, "The roadmap has no row that is done.");
        Assert.True(firstLater < 0 || firstLater > lastDone,
            $"Row {firstLater + 1} of the roadmap is still to come and stands above row {lastDone + 1}, which is done.");
    }

    [Fact]
    public void EveryWikiLinkInTheTable_LeadsToAPageTheWikiHas()
    {
        var links = Rows(Page()).SelectMany(row => WikiLink().Matches(row).Select(link => link.Groups["page"].Value)).ToArray();
        var missing = links
            .Where(page => !File.Exists(Path.Join(Wiki.BesideTheRepository.Folder, page.Replace(' ', '-') + ".md")))
            .ToArray();

        Assert.NotEmpty(links);
        Assert.True(missing.Length == 0, "The roadmap's table links to wiki pages that do not exist: " + string.Join(", ", missing));
    }

    [Fact]
    public void EveryRow_HasASectionOfItsOwn_UnderWhatIsDoneOrWhatIsStillToCome()
    {
        var page = Page();
        var done = Part(page, "## What is done");
        var toCome = Part(page, "## What is still to come");
        var missing = Rows(page)
            .Where(row => !(row.StartsWith(Done, StringComparison.Ordinal) ? done : toCome)
                .Contains("\n### " + Name(row) + "\n", StringComparison.Ordinal))
            .Select(Name)
            .ToArray();

        Assert.True(missing.Length == 0, "Rows of the roadmap with no section of their own in the right part: " + string.Join(", ", missing));
    }

    private static string Page()
    {
        Assert.True(File.Exists(Path.Join(Wiki.BesideTheRepository.Folder, PageName)), $"The wiki has no {PageName}.");

        return Wiki.BesideTheRepository.Text(PageName);
    }

    // The table's rows: every line of the one table under its header, the header and the separator left out.
    private static string[] Rows(string page)
    {
        string[] rows =
        [
            .. page.Split('\n')
                .SkipWhile(line => line.Trim() != Header)
                .Skip(2)
                .TakeWhile(line => line.StartsWith('|'))
                .Select(line => line.Trim()),
        ];

        Assert.True(rows.Length > 0, $"The roadmap has no table under the header '{Header}'.");

        return rows;
    }

    // What a row is: its second cell, a wiki link written as the page it names.
    private static string Name(string row) => WikiLink().Replace(row.Split('|')[2].Trim(), link => link.Groups["page"].Value);

    // One part of the page: from its heading to the next heading of the same level, a line feed before the first line.
    private static string Part(string page, string heading)
    {
        var start = page.IndexOf("\n" + heading + "\n", StringComparison.Ordinal);

        if (start < 0)
        {
            return string.Empty;
        }

        var end = page.IndexOf("\n## ", start + heading.Length + 1, StringComparison.Ordinal);

        return end < 0 ? page[start..] + "\n" : page[start..end] + "\n";
    }
}
