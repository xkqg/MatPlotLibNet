// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// A reader finds the wiki through its menu and its links: the menu on the right leads to every page and to the three places of the
/// docs site that are not pages (the playground, the cookbook and the API reference), it names the release the packages carry, every
/// link from one page to another names a page, a picture or a heading that is there, and the page that says what 1.30.0 removed names
/// every member the change log lists as removed.
/// </summary>
public sealed partial class WikiNavigationTests
{
    private const string SidebarName = "_Sidebar.md";

    private const string MigrationName = "Migrating-to-1.30.0.md";

    private const string DocsSite = "https://xkqg.github.io/MatPlotLibNet/";

    [GeneratedRegex(@"\[\[(?:[^\]|]*\|)?(?<target>[^\]|]+)\]\]|\]\((?<target>[^)\s]+)\)")]
    private static partial Regex PageLink();

    [Fact]
    public void TheSidebar_LinksEveryPageOfTheWiki()
    {
        var linked = LinksOf(Wiki.BesideTheRepository.Text(SidebarName)).Select(link => link.Page).ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] missing = [.. PageNames().Where(page => page != "_Sidebar" && !linked.Contains(page))];

        Assert.True(missing.Length == 0, "The menu on the right leads to no page named: " + string.Join(", ", missing));
    }

    [Theory]
    [InlineData("playground/")]
    [InlineData("cookbook/")]
    [InlineData("api/")]
    public void TheSidebar_LeadsToThePlaygroundTheCookbookAndTheApiReferenceOfTheDocsSite(string place)
    {
        Assert.Contains($"]({DocsSite}{place})", Wiki.BesideTheRepository.Text(SidebarName), StringComparison.Ordinal);
    }

    [Fact]
    public void TheSidebar_NamesTheReleaseThePackagesCarry()
    {
        Assert.StartsWith($"**MatPlotLibNet {Repository.PackageVersion}**", Wiki.BesideTheRepository.Text(SidebarName), StringComparison.Ordinal);
    }

    [Fact]
    public void EveryLinkFromOnePageToAnother_NamesAPageAPictureOrAHeadingThatIsThere()
    {
        var wiki = Wiki.BesideTheRepository;
        var headings = PageNames().ToDictionary(page => page, page => HeadingAnchors(wiki.Text(page + ".md")), StringComparer.OrdinalIgnoreCase);

        string[] broken =
        [
            .. wiki.Pages().SelectMany(page => LinksOf(wiki.Text(page.Name)).Select(link => (Page: page.Name[..^3], link)))
                .Select(found => Fault(found.Page, found.link, headings))
                .OfType<string>(),
        ];

        Assert.True(broken.Length == 0, $"{broken.Length} links name nothing that is there:\n" + string.Join('\n', broken));
    }

    [Fact]
    public void TheMigrationPage_NamesEveryMemberTheChangeLogSaysIsRemoved_ASingleLineHoldingAllOfABulletsMembers()
    {
        // The page is about the release it names, whichever release the packages carry now.
        var release = MigrationName["Migrating-to-".Length..^".md".Length];
        var released = SourceFile.SectionOf(File.ReadAllText(Path.Join(RepositoryRoot.Folder, "CHANGELOG.md")), $"[{release}]");
        var removed = SourceFile.BulletsOf(SourceFile.SubsectionOf(released, "Removed"));

        Assert.NotEmpty(removed);
        Assert.Empty(Unnamed(removed, Wiki.BesideTheRepository.Text(MigrationName)));
    }

    [Fact]
    public void ARemovedMember_IsLeftOutWhenNoLineOfThePageNamesItWithTheOthersOfItsBullet_WhateverElseThePageSays()
    {
        string[] bullets =
        [
            "- `Gadget.OldOne()` -> `Gadget.New.First()`.",
            "- `Gadget.OldList` and `Gadget.OldOther` (both read-only) -> `Gadget.NewList` and `Gadget.NewOther`.",
            "- The `figure` parameter of the `OldModifier` constructor -> nothing: none of them read it.",
        ];
        const string figure = "| The `figure` parameter of the `OldModifier` constructor | nothing |";
        const string whole = "| `Gadget.OldOne()` | `Gadget.New.First()` |\n| `Gadget.OldList` and `Gadget.OldOther` | the lists |\n" + figure;

        Assert.Empty(Unnamed(bullets, whole));
        Assert.Equal([bullets[2]], Unnamed(bullets, whole.Replace(figure, "var figure = Plt.Create();", StringComparison.Ordinal)));
        Assert.Equal([bullets[1]], Unnamed(bullets, whole.Replace(" and `Gadget.OldOther`", string.Empty, StringComparison.Ordinal)));
        Assert.Equal([bullets[1]], Unnamed(bullets, whole.Replace(" and `Gadget.OldOther`", "\n| `Gadget.OldOther`", StringComparison.Ordinal)));
        Assert.Equal(["- nothing in code"], Unnamed(["- nothing in code"], whole));
    }

    // The bullets whose removed members (the names in backticks before the arrow, cut at their parenthesis) no single line of the page holds
    // together: a name that stands somewhere else on the page, in code, does not count for the row of the member.
    private static string[] Unnamed(IEnumerable<string> bullets, string page)
    {
        var lines = SourceFile.LinesOf(page);

        return
        [
            .. bullets.Where(bullet =>
            {
                string[] names = [.. SourceFile.CodeSpansOf(bullet.Split("->")[0]).Select(span => span.Code.Split('(')[0])];
                return names.Length == 0 || !lines.Any(line => names.All(name => line.Contains(name, StringComparison.Ordinal)));
            }),
        ];
    }

    // The pages' names without ".md", in the spelling of their files.
    private static string[] PageNames() => [.. Wiki.BesideTheRepository.Pages().Select(page => page.Name[..^3])];

    // What a link says about the page it leads to; the links of a page that are not to the wiki or to its pictures (web addresses, mail) are left out.
    private static IEnumerable<WikiLink> LinksOf(string text) =>
        SourceFile.MarkdownLinesOf(text).Where(line => !line.InFence)
            .SelectMany(line => PageLink().Matches(SourceFile.WithoutCodeSpans(line.Text)).Select(match => match.Groups["target"].Value.Trim())
            .Where(target => !Regex.IsMatch(target, "^(?:https?:|mailto:)"))
            .Select(target => new WikiLink(line.Number, target)));

    // The anchors GitHub gives the headings of a page: the heading in lower case without its backticks, only its letters, digits,
    // hyphens and spaces kept, a space a hyphen, and a second heading of the same words numbered.
    private static HashSet<string> HeadingAnchors(string text)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var anchors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var heading in SourceFile.HeadingsOf(text))
        {
            var slug = Regex.Replace(heading.ToLowerInvariant().Replace("`", string.Empty, StringComparison.Ordinal), @"[^\w\- ]", string.Empty).Replace(' ', '-');
            var count = seen.GetValueOrDefault(slug);
            seen[slug] = count + 1;
            anchors.Add(count == 0 ? slug : $"{slug}-{count}");
        }

        return anchors;
    }

    private static string? Fault(string page, WikiLink link, Dictionary<string, HashSet<string>> headings)
    {
        var parts = link.Target.Split('#', 2);
        var anchor = parts.Length == 2 ? parts[1] : string.Empty;
        var target = parts[0].Length == 0 ? page : link.Page;
        var where = $"{page}.md:{link.Line} -> {link.Target}";

        if (target.StartsWith("images/", StringComparison.Ordinal))
        {
            return File.Exists(Path.Join(Wiki.BesideTheRepository.Folder, target)) ? null : $"{where} (no such picture)";
        }

        if (!headings.TryGetValue(target, out var anchors))
        {
            return $"{where} (no such page)";
        }

        return anchor.Length == 0 || anchors.Contains(anchor) ? null : $"{where} (the page has no such heading)";
    }

    /// <summary>A link of a page: the line it stands on, and the page (and heading) it names.</summary>
    /// <param name="Line">The line, from one.</param>
    /// <param name="Target">What stands in the link: a page name with spaces or hyphens, a heading after a hash, or a picture's path.</param>
    private readonly record struct WikiLink(int Line, string Target)
    {
        /// <summary>The page the link leads to, in the spelling of its file: spaces are hyphens, the heading is left out.</summary>
        public string Page => Target.Split('#')[0].Replace(' ', '-');
    }
}
