// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// A document that names a member under or beside a type says the type has it: in a table of methods under the type's name, in a list after
/// the type in a sentence, or spelled <c>Type.Member</c>. A name that exists somewhere in the library is no proof of that: the per-indicator
/// calls of a data frame were removed while the indicator classes of the same names stayed, and a page went on listing the calls under the
/// extension class. This reads each such claim against the members of the type it names, in the packages as built.
/// </summary>
public sealed class NamedMembersTests
{
    /// <summary>How many claims of each spelling the wiki holds under a type of the packages, at least: a reader that finds fewer has
    /// stopped seeing a spelling, or a page was read from the wrong place. Measured, not chosen.</summary>
    private const int FewestDotted = 150;

    private const int FewestTable = 150;

    private const int FewestList = 50;

    private const string MigrationPage = "Migrating-to-1.30.0.md";

    /// <summary>The folder of the plans and histories of past work, which name members as they were.</summary>
    private const string Records = "docs/contrib/";

    /// <summary>How many table claims and how many dotted calls of a table the repository's documents hold at least, measured like the floors above.</summary>
    private const int FewestRepositoryTable = 100;

    private const int FewestChain = 60;

    /// <summary>How many documents of the repository the tree lists at least: a tree walked from the wrong place lists none.</summary>
    private const int FewestDocuments = 50;

    [Fact]
    public void TheClaimsOfADocument_AreReadInEverySpellingTheWikiUses()
    {
        string text = string.Join('\n',
        [
            "### the tile",
            "",
            "A call is `Plt.Create()` or `Plt.Style.Use(\"x\")`, and `SankeyNode.SubLabel` / `SubLabelColor` stand in a cell.",
            "",
            "**Financial indicators** (`DataFrameIndicatorExtensions`) - each method:",
            "",
            "| Group | Methods |",
            "|---|---|",
            "| Price | `Sma`, `Ema(period)` |",
            "| text | `1.5` |",
            "",
            "| `AxisTransform` | Shows |",
            "|---|---|",
            "| `Linear(scale, offset = 0)` | `scale` |",
            "",
            "Its builder, `XAxisSlotBuilder`, has `SetXLabel`, `Link` and `SetLocation`; the axis holds no series.",
            "The callback's `YAxisSlotBuilder` has the same verbs",
            "for every axis: `SetYLabel`, `Plot` or `Scatter`.",
            "```csharp",
            "var a = `Plt.Nothing`;",
            "```",
            "| Call | Returns |",
            "|---|---|",
            "| `Orphan()` | `Fig` |",
        ]);

        string[] claims = [.. MemberClaim.Of("page.md", text).Select(claim => $"{claim.Line} {claim.Spelling} {string.Join('/', claim.Owners)} {string.Join('.', claim.Path)}")];

        Assert.Equal(
        [
            "3 Dotted Plt Create",
            "3 Dotted Plt Style.Use",
            "3 Dotted SankeyNode SubLabel",
            "3 Dotted SankeyNode SubLabelColor",
            "9 Table DataFrameIndicatorExtensions Sma",
            "9 Table DataFrameIndicatorExtensions Ema",
            "14 Table AxisTransform Linear",
            "16 List XAxisSlotBuilder SetXLabel",
            "16 List XAxisSlotBuilder Link",
            "16 List XAxisSlotBuilder SetLocation",
            "18 List YAxisSlotBuilder SetYLabel",
            "18 List YAxisSlotBuilder Plot",
            "18 List YAxisSlotBuilder Scatter",
        ], claims);
    }

    [Fact]
    public void TheClaimsOfADocument_LeaveOutWhatItSaysIsGone_ValuesInBrackets_AndNamesThatOnlyFollowAType()
    {
        string text = string.Join('\n',
        [
            "| Options | Used by | Holds |",
            "|---|---|---|",
            "| `SpectralOptions` | `Psd` | `SampleRate` (2), `Scaling` (`Density`) |",
            "",
            "| Removed | Replaced by |",
            "|---|---|",
            "| `Widget.Gone` | `Foo.Bar` |",
            "",
            "`GridWhich` controls tick level: `Major` (default), `Minor`, `Both`. `Plot` uses `Line`, `Fill`.",
            "The callback receives `RaincloudParts` (`Violin`, `Box`, `Points`).",
        ]);

        string[] claims = [.. MemberClaim.Of("page.md", text).Select(claim => $"{claim.Line} {claim.Spelling} {string.Join('/', claim.Owners)} {string.Join('.', claim.Path)}")];

        Assert.Equal(
        [
            "3 Table SpectralOptions SampleRate",
            "3 Table SpectralOptions Scaling",
            "7 Dotted Foo Bar",
            "9 List GridWhich Major",
            "9 List GridWhich Minor",
            "9 List GridWhich Both",
            "10 List RaincloudParts Violin",
            "10 List RaincloudParts Box",
            "10 List RaincloudParts Points",
        ], claims);
    }

    [Fact]
    public void TheClaimsOfADocument_ReadATypeInAnyColumnOfARow_InTheCellOfARow_AndInAnEnclosingHeading()
    {
        string text = string.Join('\n',
        [
            "| Data | Class | Verbs |",
            "|---|---|---|",
            "| values keyed by names | `CategoricalAxesExtensions` | `Lollipop`, `Pareto` |",
            "",
            "| Data | Verbs |",
            "|---|---|",
            "| **Names** (`DistributionAxesExtensions`) | `Ridgeline(groups)`, `QQPlot(sample)` (a `SpectralOptions`) |",
            "",
            "## 3. `AxesBuilder` - per-subplot chaining",
            "",
            "### sharing",
            "",
            "Some prose, no type in it.",
            "",
            "| Call | Returns |",
            "|---|---|",
            "| `WithAddRightYAxis(Action)` | `AxesBuilder` |",
            "",
            "## 4. other things",
            "",
            "| Call | Returns |",
            "|---|---|",
            "| `Elsewhere()` | `Fig` |",
            "",
            "## Fluent API reference - BarSeries",
            "",
            "| Property | Type |",
            "|---|---|",
            "| `Label` | `string` |",
        ]);

        string[] claims = [.. MemberClaim.Of("page.md", text).Select(claim => $"{claim.Line} {claim.Spelling} {string.Join('/', claim.Owners)} {string.Join('.', claim.Path)}")];

        Assert.Equal(
        [
            "3 Table CategoricalAxesExtensions Lollipop",
            "3 Table CategoricalAxesExtensions Pareto",
            "7 Table DistributionAxesExtensions Ridgeline",
            "7 Table DistributionAxesExtensions QQPlot",
            "17 Table AxesBuilder WithAddRightYAxis",
            "29 Table Fluent/API/BarSeries Label",
        ], claims);
    }

    [Fact]
    public void ACallWrittenWithADotInATable_IsACallOfABuilder_AndAValueColumnOfATypedRowListsItsMembers()
    {
        string text = string.Join('\n',
        [
            "| Axes method | Description |",
            "|---|---|",
            "| `.WithTitle(text)` | a call of the builders |",
            "| `.NoSuchCallAnywhere()` | gone |",
            "| `Plt.Mosaic(pattern)` | not a dotted call |",
            "",
            "| Axis | Values | What it says |",
            "|---|---|---|",
            "| `OpsSeverity` | `Normal`, `Gone` | how bad |",
        ]);

        var claims = MemberClaim.Of("page.md", text);

        Assert.Equal(
        [
            "3 Chain  WithTitle",
            "4 Chain  NoSuchCallAnywhere",
            "5 Dotted Plt Mosaic",
            "9 Table OpsSeverity Normal",
            "9 Table OpsSeverity Gone",
        ], claims.Select(claim => $"{claim.Line} {claim.Spelling} {string.Join('/', claim.Owners)} {string.Join('.', claim.Path)}"));
        Assert.Equal(
        [
            null,
            "page.md, line 4: no builder has the call `.NoSuchCallAnywhere` (Chain)",
            null,
            null,
            "page.md, line 9: `OpsSeverity` has no member `Gone` (Table)",
        ], claims.Select(claim => claim.Missing(PackageSurface.Loaded)));
    }

    [Fact]
    public void AListedNameThatIsATypeOfThePackages_IsReadAsTheType()
    {
        var claim = new MemberClaim("page.md", 1, ["Axes"], ["FigureBuilder"], MemberClaimSpelling.List);
        var dotted = claim with { Spelling = MemberClaimSpelling.Dotted };

        Assert.Null(claim.Missing(PackageSurface.Loaded));
        Assert.NotNull(dotted.Missing(PackageSurface.Loaded));
        Assert.Null(new MemberClaim("page.md", 1, ["NoSuchTypeAnywhere"], ["Plot"], MemberClaimSpelling.Dotted).Missing(PackageSurface.Loaded));
    }

    [Fact]
    public void ATypeOfAPackageThisSuiteCannotBuild_IsReadFromItsSource()
    {
        var sources = UnbuiltPackages.Sources;

        Assert.True(sources.Declares("WpfAnimationTimer"));
        Assert.True(sources.Has("MauiGraphicsRenderContext", "ToRectF"), "a static method");
        Assert.True(sources.Has("MauiGraphicsRenderContext", "ApplyStroke"), "a private method");
        Assert.False(sources.Has("MauiGraphicsRenderContext", "NoSuchMember"));
        Assert.False(sources.Declares("NoSuchType"));
        Assert.True(PackageSurface.Loaded.IsType("MauiAnimationTimer"));
        Assert.Null(new MemberClaim("page.md", 1, ["MauiGraphicsRenderContext"], ["PathOf"], MemberClaimSpelling.Dotted).Missing(PackageSurface.Loaded));
        Assert.NotNull(new MemberClaim("page.md", 1, ["MauiGraphicsRenderContext"], ["NoSuchMember"], MemberClaimSpelling.Dotted).Missing(PackageSurface.Loaded));
        Assert.Null(new MemberClaim("page.md", 1, ["MauiAnimationTimer"], ["WpfAnimationTimer"], MemberClaimSpelling.List).Missing(PackageSurface.Loaded));
    }

    [Fact]
    public void ATableWithNoTypeAboveIt_ClaimsNothing()
    {
        Assert.Empty(MemberClaim.Of("page.md", "| Call | Returns |\n|---|---|\n| `Orphan()` | `Fig` |\n"));
    }

    [Fact]
    public void TheSurface_ReachesTheMembersATypeDeclaresInheritsOrGainsByAnExtension_AndNoOtherName()
    {
        var surface = PackageSurface.Loaded;
        var indicators = surface.TypesNamed("DataFrameIndicatorExtensions");

        Assert.NotEmpty(indicators);
        Assert.True(surface.Reaches(indicators, "Hlc"), "a call the extension class holds");
        Assert.True(surface.Reaches(surface.TypesNamed("DataFrameColumnExtensions"), "Values"), "the call that reads one column");
        Assert.False(surface.Reaches(indicators, "Values"), "the column call is held by the class of columns, not by the class of bar shapes");
        Assert.False(surface.Reaches(indicators, "Sma"), "the indicator class is named Sma; the extension class holds no such call");
        Assert.True(surface.Reaches(surface.TypesNamed("AxesBuilder"), "Plot"), "an extension of the builder");
        Assert.True(surface.Reaches(surface.TypesNamed("Plt"), "Create"), "a static method");
        Assert.Contains(surface.TypesNamed("FigureBuilder"), type => type.Name == "FigureBuilder");
        Assert.Contains(surface.Gives(surface.TypesNamed("Plt"), "Create"), type => type.Name == "FigureBuilder");
    }

    [Fact]
    public void EveryMemberAWikiPageNamesUnderAType_IsAMemberOfThatType()
    {
        var wiki = Wiki.BesideTheRepository;

        // A record of what a release published names members as they were; the migration page says what was removed and is read
        // apart from its Removed columns.
        var claims = wiki.Pages()
            .Where(page => !DeletedName.PublishedRecords.Contains(page.Name) || page.Name == MigrationPage)
            .SelectMany(page => MemberClaim.Of(page.Name, wiki.Text(page.Name)))
            .ToList();

        var underATypeOfThePackages = claims.Where(claim => claim.Owners.Any(PackageSurface.Loaded.IsType)).ToList();

        Assert.True(underATypeOfThePackages.Count(claim => claim.Spelling == MemberClaimSpelling.Dotted) >= FewestDotted, "the wiki reads fewer dotted claims than it holds");
        Assert.True(underATypeOfThePackages.Count(claim => claim.Spelling == MemberClaimSpelling.Table) >= FewestTable, "the wiki reads fewer table claims than it holds");
        Assert.True(underATypeOfThePackages.Count(claim => claim.Spelling == MemberClaimSpelling.List) >= FewestList, "the wiki reads fewer list claims than it holds");

        var missing = claims.Select(claim => claim.Missing(PackageSurface.Loaded)).OfType<string>().ToList();

        Assert.True(missing.Count == 0, $"{missing.Count} members a page names under a type that the type does not have:\n" + string.Join('\n', missing));
    }

    [Fact]
    public void EveryMemberADocumentOfTheRepositoryNamesUnderAType_IsAMemberOfThatType()
    {
        // The change log and the roadmap record what each release published; the plans and histories under docs/contrib say what a
        // piece of work was when it was done. All of them name members as they were.
        var documents = RepositoryTree.CurrentStateDocuments()
            .Where(document => !document.Path.StartsWith(Records, StringComparison.Ordinal) && !DeletedName.PublishedRecords.Contains(document.Path))
            .ToList();

        var claims = documents.SelectMany(document => MemberClaim.Of(document.Path, document.Text)).ToList();
        var missing = claims.Select(claim => claim.Missing(PackageSurface.Loaded)).OfType<string>().ToList();

        Assert.True(claims.Count(claim => claim.Spelling == MemberClaimSpelling.Table && claim.Owners.Any(PackageSurface.Loaded.IsType)) >= FewestRepositoryTable, "the documents read fewer table claims than they hold");
        Assert.True(claims.Count(claim => claim.Spelling == MemberClaimSpelling.Chain) >= FewestChain, "the documents read fewer dotted calls of a table than they hold");

        Assert.True(documents.Count >= FewestDocuments, $"the tree holds {documents.Count} documents of the repository, fewer than the {FewestDocuments} measured");
        Assert.True(missing.Count == 0, $"{missing.Count} members a document names under a type that the type does not have:\n" + string.Join('\n', missing));
    }
}
