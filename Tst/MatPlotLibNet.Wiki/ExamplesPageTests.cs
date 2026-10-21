// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The changelog says what each release brought; the wiki's Examples page shows how to call it: a section to each release,
/// the newest first, each in code written against the library as it is now. Its code is compiled with every other block of
/// the wiki; this holds the page's shape: the release the packages carry comes first and shows something in code, every
/// section names a release the changelog has, the table at the top leads to every section, and the start page leads to it.
/// </summary>
public sealed class ExamplesPageTests
{
    private const string PageName = "Examples.md";

    [Fact]
    public void TheWiki_HasAnExamplesPage()
    {
        Assert.True(
            File.Exists(Path.Join(Wiki.BesideTheRepository.Folder, PageName)),
            $"The wiki at {Wiki.BesideTheRepository.Folder} has no {PageName}: the page that shows each release's additions in code.");
    }

    [Fact]
    public void TheFirstSection_IsTheReleaseThePackagesCarry_AndShowsItInCode()
    {
        var first = Sections()[0];

        Assert.Equal(Repository.PackageVersion, first.Version);
        Assert.NotEmpty(WikiPage.Read(PageName, first.Text).Programs);
    }

    [Fact]
    public void EverySection_IsAReleaseTheChangelogHas_NewestFirst()
    {
        var changelog = File.ReadAllText(Path.Join(Repository.Root, "CHANGELOG.md"));
        Version[] versions = [.. Sections().Select(section => section.Version)];

        Assert.Equal(versions.OrderDescending(), versions);
        Assert.All(versions, version => Assert.Contains($"## [{version}]", changelog, StringComparison.Ordinal));
    }

    [Fact]
    public void TheTableAtTheTop_LeadsToEverySection()
    {
        var page = Page();
        var top = page[..page.IndexOf("\n## ", StringComparison.Ordinal)];
        var missing = Sections()
            .Select(section => $"[{section.Version}](#{section.Heading.Replace(".", string.Empty, StringComparison.Ordinal)})")
            .Where(link => !top.Contains(link, StringComparison.Ordinal))
            .ToArray();

        Assert.True(missing.Length == 0, "The table at the top of the Examples page has no row linking: " + string.Join(", ", missing));
    }

    [Fact]
    public void TheStartPage_LeadsToTheExamplesPage()
    {
        var home = Wiki.BesideTheRepository.Text("Home.md");

        Assert.Matches(new Regex(@"\[\[(?:[^\]|]*\|)?Examples(?:#[^\]]*)?\]\]|\]\(Examples(?:#[^)]*)?\)"), home);
    }

    private static string Page()
    {
        Assert.True(File.Exists(Path.Join(Wiki.BesideTheRepository.Folder, PageName)), $"The wiki has no {PageName}.");

        return Wiki.BesideTheRepository.Text(PageName);
    }

    // The page's releases in the order they stand: every section whose heading is a version, up to the next heading.
    private static Section[] Sections()
    {
        Section[] sections =
        [
            .. Page().Split("\n## ").Skip(1)
                .Select(chunk => new Section(chunk[..chunk.IndexOf('\n', StringComparison.Ordinal)].Trim(), "## " + chunk))
                .Where(section => section.Heading.Split('.').Length == 3 && Version.TryParse(section.Heading, out _)),
        ];

        Assert.True(sections.Length > 0, $"The {PageName} page has no section headed by a release, such as '## 1.19.0'.");

        return sections;
    }

    /// <summary>One release's section of the page.</summary>
    /// <param name="Heading">What its heading says.</param>
    /// <param name="Text">The section, from its heading to the next.</param>
    private readonly record struct Section(string Heading, string Text)
    {
        /// <summary>The release the section is of.</summary>
        public Version Version => Version.Parse(Heading);
    }
}
