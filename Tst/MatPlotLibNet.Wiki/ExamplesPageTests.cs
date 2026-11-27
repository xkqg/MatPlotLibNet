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
public sealed partial class ExamplesPageTests
{
    private const string PageName = "Examples.md";

    /// <summary>The most lines a block of the page holds. Measured, not chosen: the longest block draws five Y axes or builds the data of a
    /// figure by hand. A block that is longer is split into two examples, or its data is made shorter.</summary>
    private const int MostLinesOfABlock = 39;

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
        var changelog = File.ReadAllText(Path.Join(RepositoryRoot.Folder, "CHANGELOG.md"));
        Version[] versions = [.. Sections().Select(section => section.Version)];

        Assert.Equal(versions.OrderDescending(), versions);
        Assert.All(versions, version => Assert.Contains($"## [{version}]", changelog, StringComparison.Ordinal));
    }

    [Fact]
    public void TheTableAtTheTop_LeadsToEverySection()
    {
        var page = Page();
        var top = SourceFile.IntroductionOf(page);
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

    [Fact]
    public void AnExample_ShowsOnePicture_OnEveryPageOfTheWiki()
    {
        var wiki = Wiki.BesideTheRepository;

        string[] twoPictures =
        [
            .. wiki.Pages().SelectMany(page => WikiExampleReading.Of(wiki.Text(page.Name)).Examples
                .Where(example => example.Pictures.Count > 1)
                .Select(example => $"{page.Name}, the block at line {example.Line}: {string.Join(", ", example.Pictures)}")),
        ];

        Assert.True(twoPictures.Length == 0, "An example shows one picture, so a reader sees what the code beside it draws. Split each of these into one block and one picture: " + string.Join("; ", twoPictures));
    }

    [Fact]
    public void AnExampleThatShowsAPicture_DrawsOneFigure_OnEveryPageOfTheWiki()
    {
        var wiki = Wiki.BesideTheRepository;

        string[] twoFigures =
        [
            .. wiki.Pages().SelectMany(page => WikiExampleReading.Of(wiki.Text(page.Name)).Examples
                .Where(example => example.Pictures.Count > 0 && (example.Figures.Count > 1 || example.Entries > 1))
                .Select(example => $"{page.Name}, the block at line {example.Line}: {example.Entries} figures started, {string.Join(", ", example.Figures)} saved")),
        ];

        Assert.True(twoFigures.Length == 0, "A block that shows one picture draws one figure, saved or not; these draw more: " + string.Join("; ", twoFigures));
    }

    [Fact]
    public void AnExampleThatDrawsAFigure_ShowsItsPicture_OnTheExamplesPage()
    {
        string[] bare =
        [
            .. WikiExampleReading.Of(Page()).Examples
                .Where(example => example.Draws && example.Pictures.Count == 0)
                .Select(example => $"the block at line {example.Line} starts {example.Entries} figure(s) and saves {string.Join(", ", example.Figures)}"),
        ];

        Assert.True(bare.Length == 0, "An example whose code starts a figure (a call of Plt, saved, turned to SVG, shown or only built) shows the picture of it, drawn by the sample program (WikiPictures.cs where no sample draws it); only a handler, a block that goes on with the figure above and a call shown to refuse draw none: " + string.Join("; ", bare));
    }

    [Fact]
    public void AnExampleThatDrawsNothing_SaysInItsFirstLineAfterItWhatItShows_OnTheExamplesPage()
    {
        string[] silent =
        [
            .. WikiExampleReading.Of(Page()).Examples
                .Where(example => !example.Draws && example.Pictures.Count == 0 && !WhatItShows().IsMatch(FirstSentence().Match(example.After).Value))
                .Select(example => $"the block at line {example.Line} is followed by '{example.After}'"),
        ];

        Assert.True(silent.Length == 0, "An example that draws nothing carries no picture, so the line after it says what it shows (what it prints, what you see): " + string.Join("; ", silent));
    }

    [Fact]
    public void TheEntriesOfAFigure_AreEveryCallOfPltThatStartsOne_AndNoStyle()
    {
        var entries = WikiExampleReading.PltEntries;

        Assert.Contains("Create", entries);
        Assert.Contains("Mosaic", entries);
        Assert.All(entries, entry => Assert.Matches(WikiExampleReading.FigureEntry, $"var figure = Plt.{entry}(1);"));
        Assert.DoesNotMatch(WikiExampleReading.FigureEntry, "Plt.Style.Use(StyleSheet.Ggplot);");
        Assert.DoesNotMatch(WikiExampleReading.FigureEntry, "Console.WriteLine(figure.ToSvg());");
        Assert.Matches(WikiExampleReading.FigureEntry, "var figure = new Figure();");
    }

    [Fact]
    public void AnExampleDrawsAFigure_WhenItStartsOne_ForAnyOutput_AndNotWhenItCatchesARefusalOrGoesOnWithTheFigureAbove()
    {
        string page = string.Join('\n',
        [
            "```csharp",
            "string svg = Plt.Create().Plot([1.0], [2.0]).ToSvg();",
            "```",
            "```csharp",
            "var figure = Plt.Create().Plot([1.0], [2.0]).Build();",
            "```",
            "```csharp",
            "try { Plt.Create().Plot([1.0], [2.0]).Save(\"chart.png\"); }",
            "catch (InvalidOperationException refusal) { Console.WriteLine(refusal.Message); }",
            "```",
            "```csharp",
            "Console.WriteLine(figure.ToSvg() == figure.ToSvg());",
            "```",
            "```csharp",
            "var a = Plt.Create().Build();",
            "var b = Plt.Mosaic(\"AB\").Build();",
            "```",
        ]);

        var examples = WikiExampleReading.Of(page).Examples;

        Assert.Equal([true, true, false, false, true], examples.Select(example => example.Draws));
        Assert.Equal([1, 1, 1, 0, 2], examples.Select(example => example.Entries));
        Assert.Equal([false, false, true, false, false], examples.Select(example => example.Refuses));
    }

    [Fact]
    public void EveryPictureOfTheExamplesPage_StandsUnderTheExampleThatDrawsIt()
    {
        var loose = WikiExampleReading.Of(Page()).Loose;

        Assert.True(loose.Count == 0, "A picture of the Examples page stands under no block of its section: " + string.Join("; ", loose));
    }

    [Fact]
    public void EveryBlockOfTheExamplesPage_IsShortEnoughToReadAtAGlance()
    {
        string[] tooLong =
        [
            .. SourceFile.FenceBlocksOf(Page()).Where(block => block.Open.Info == "csharp" && block.Body.Count > MostLinesOfABlock)
                .Select(block => $"the block at line {block.OpenAt + 2} holds {block.Body.Count} lines"),
        ];

        Assert.True(tooLong.Length == 0, $"An example is short enough to read at a glance: at most {MostLinesOfABlock} lines, the length of the longest that needs its data and its setup written out. " + string.Join("; ", tooLong));
    }

    [Fact]
    public void TheReadingOfExamples_GivesEachBlockThePicturesStraightUnderIt_AndTheFiguresItSaves()
    {
        string page = string.Join('\n',
        [
            "![before](images/a.png)",
            "## One",
            "```csharp",
            "figure.Save(\"x.svg\");",
            "figure.Save(\"x.svg\");",
            "figure.Save(\"y.svg\");",
            "figure.Save(\"z.png\");",
            "```",
            "![x](images/x.png)",
            "```text",
            "output",
            "```",
            "![y](images/y.png)",
            "prose, and then a picture of another paragraph",
            "![late](images/late.png)",
            "```csharp",
            "// no save",
            "```",
            "## Two",
            "![after a heading](images/b.png)",
            "```csharp",
            "var kept = 1; // a '![x](images/c.png)' in code",
            "```",
            "![z](images/z.png)",
        ]);

        var reading = WikiExampleReading.Of(page);

        Assert.Equal([4, 17, 22], reading.Examples.Select(example => example.Line));
        Assert.Equal(["x.png", "y.png"], reading.Examples[0].Pictures);
        Assert.Equal(["x.svg", "y.svg"], reading.Examples[0].Figures);
        Assert.Empty(reading.Examples[1].Pictures);
        Assert.Empty(reading.Examples[1].Figures);
        Assert.Equal(["z.png"], reading.Examples[2].Pictures);
        Assert.Equal(["line 1: images/a.png", "line 15: images/late.png", "line 20: images/b.png"], reading.Loose);
        Assert.Equal(["prose, and then a picture of another paragraph", string.Empty, string.Empty], reading.Examples.Select(example => example.After));
    }

    [Fact]
    public void TheReadingOfExamples_TakesAnAnimationForAFigure_AndAnHtmlImageForAPicture()
    {
        string page = string.Join('\n',
        [
            "```csharp",
            "steps.SaveStepThroughSvg(\"wave.svg\");",
            "figure.Save(\"chart.png\");",
            "```",
            "<img src=\"images/wave.svg\" alt=\"steps\" width=\"560\">",
            "It steps through its frames.",
        ]);

        var example = Assert.Single(WikiExampleReading.Of(page).Examples);

        Assert.Equal(["wave.svg"], example.Figures);
        Assert.Equal(["wave.svg"], example.Pictures);
        Assert.Equal("It steps through its frames.", example.After);
    }

    // A sentence that tells a reader what an example shows: what it prints, what comes up, what the file holds.
    [GeneratedRegex(@"\b(?:see|prints?|shows?|writes?|reads?|holds?|opens?|starts?|says?|gives?|returns?)\b")]
    private static partial Regex WhatItShows();

    // The first sentence of a line: up to the first full stop, question mark or exclamation mark that is followed by a space or the end.
    [GeneratedRegex(@"^.*?(?:[.!?](?=\s|$)|$)")]
    private static partial Regex FirstSentence();

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
            .. SourceFile.SectionsOf(Page())
                .Select(section => new Section(section.Heading.Trim(), section.Text))
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
