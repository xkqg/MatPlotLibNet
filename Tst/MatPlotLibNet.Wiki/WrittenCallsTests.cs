// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// A call a document writes outside a block the wiki compiles is compiled by nothing else: a row of the cheat sheet, the C# column of the
/// page from matplotlib to C#, a fenced block of a repository document, the code of an XML comment. A parameter renamed, an argument
/// dropped or a call moved from one builder to another leaves such a call reading true while no compiler accepts it. Each call is compiled
/// here against the packages as built, a name the document leaves undefined standing for any value; and the matplotlib call it is compared
/// with is read against the signatures matplotlib 3.11.2 has (<see cref="PythonSignatures"/>).
/// </summary>
public sealed class WrittenCallsTests
{
    /// <summary>How many calls each kind of document holds at least: a reader that finds fewer has stopped seeing a spelling. Measured, not
    /// chosen.</summary>
    private const int FewestWikiCalls = 480;

    private const int FewestRepositoryCalls = 1045;

    private const int FewestRepositoryBlocks = 370;

    private const int FewestXmlCalls = 210;

    private const int FewestPythonCalls = 319;

    private const string MigrationPage = "Migrating-to-1.30.0.md";

    /// <summary>The folder of the plans and histories of past work, which write calls as they were.</summary>
    private const string Records = "docs/contrib/";

    [Fact]
    public void TheCallsOfADocument_AreReadInEverySpelling_WithTheTypeTheyStandUnder()
    {
        string text = string.Join('\n',
        [
            "A call is `ax.Plot(x, y)`, `Plt.Create().WithTitle(\"t\")` or `.WithLegend()`; `figure.Save(…)` too.",
            "",
            "## `AxesBuilder`",
            "",
            "| Call | Returns |",
            "|---|---|",
            "| `WithAddRightYAxis(Action)` | `AxesBuilder` |",
            "",
            "| Removed | Replaced by |",
            "|---|---|",
            "| `Gone(x)` | `Kept(x)` |",
            "",
            "The verb `Psd(signal, options?)` and `new Rectangle(new(1, 2), 3, 4) { Angle = 5 }`; `plt.plot(x, y)` is Python, `S(m)` is math; there is no synchronous `Show()`.",
            "```csharp",
            "var b = 1;",
            "```",
        ]);

        string[] calls = [.. CallClaim.InlineOf("page.md", text).Select(claim => $"{claim.Line} {string.Join('/', claim.Owners)} {claim.Returns} {claim.Code}")];

        Assert.Equal(
        [
            "1   ax.Plot(x, y)",
            "1   Plt.Create().WithTitle(\"t\")",
            "1   .WithLegend()",
            "1   figure.Save(…)",
            "7 AxesBuilder AxesBuilder WithAddRightYAxis(Action)",
            "11   Kept(x)",
            "13   Psd(signal, options?)",
            "13   new Rectangle(new(1, 2), 3, 4) { Angle = 5 }",
        ], calls);
        Assert.Equal(["15 var b = 1;"], CallClaim.BlocksOf("page.md", text).Select(claim => $"{claim.Line} {claim.Code.Trim()}"));
        Assert.Equal(["13 plt.plot(x, y)"], PythonCall.InlineOf("page.md", text).Select(call => $"{call.Line} {call.Code}"));
    }

    [Fact]
    public void ACallTheXmlDocumentationWrites_IsReadWithTheTypeItsCommentStandsIn()
    {
        const string source = """
            namespace N;

            /// <summary>Drawn by <c>WithGrid(g =&gt; g with { Visible = true })</c>, as matplotlib's <c>ax.grid(True)</c> is.</summary>
            /// <example><code>
            /// Plt.Create().Plot(x, y);
            /// </code></example>
            public static class Holder
            {
                /// <summary>See <c>Inner(1)</c> and <c>X(k) = sum x(m)</c>.</summary>
                public sealed class Nested { }
            }
            """;

        var file = new SourceFile("Src/Lib/Holder.cs", source);

        Assert.Equal(
        [
            "3 Holder WithGrid(g => g with { Visible = true })",
            "9 Nested/Holder Inner(1)",
        ], CallClaim.InlineOf(file).Select(claim => $"{claim.Line} {string.Join('/', claim.Owners)} {claim.Code}"));
        Assert.Equal(["5 Plt.Create().Plot(x, y);"], CallClaim.BlocksOf(file).Select(claim => $"{claim.Line} {claim.Code.Trim()}"));
        Assert.Equal(["3 ax.grid(True)"], PythonCall.InlineOf(file).Select(call => $"{call.Line} {call.Code}"));
    }

    [Fact]
    public void ACallThatTheBuilderTakes_Fits_AndOneItDoesNotTake_IsNamedWithWhatIsWrong()
    {
        string text = string.Join('\n',
        [
            "`ax.Plot(x, y)`, `ax.Plot(x, y, s => { s.Color = Colors.SteelBlue; s.LineWidth = 2; })`, `ax.Hist(data, 20)`,",
            "`figure.AddSubPlot(1, 1, 1, ax => …)`, `Plt.Create().WithSize(800, 600).Build().ToSvg()`, `ax.Hist(data, bins: 20)`,",
            "`figure.Build().Save(\"c.png\", new SaveOptions { Dpi = 300 })`, `ax.SetXLim(0, 10, 20)`, `ax.Hist(data, binz: 20)`,",
            "`Plt.Create().NoSuchCall()`, `ax.Plot(x, y, s => s.NoSuchProperty = 1)`, `new MatPlotLibNet.Models.Patches.Circle(new(0, 0), 1) { Nope = 2 }`, `BarGroup(label)`.",
            "",
            "## `AxesBuilder`",
            "",
            "| Call | Returns |",
            "|---|---|",
            "| `WithAddRightYAxis(Action)` | `AxesBuilder` |",
            "| `WithAddLeftYAxis(Action)` | `FigureBuilder` |",
            "| `WithYAxisTitleGap(double pixels)` | `AxesBuilder` |",
            "| `WithYAxisTitleGap(string pixels)` | `AxesBuilder` |",
            "| `WithYAxisTitleGap(double gap)` | `AxesBuilder` |",
            "| `WithAddRightYAxis(Func<int>)` | `AxesBuilder` |",
        ]);

        var mismatches = CallClaim.InlineOf("page.md", text).Compiled().Mismatches;

        Assert.Equal(
        [
            "3 ax.SetXLim(0, 10, 20) CS1501",
            "3 ax.Hist(data, binz: 20) CS1739",
            "4 Plt.Create().NoSuchCall() Unknown",
            "4 ax.Plot(x, y, s => s.NoSuchProperty = 1) Unknown",
            "4 new MatPlotLibNet.Models.Patches.Circle(new(0, 0), 1) { Nope = 2 } Unknown",
            "4 BarGroup(label) CS7036",
            "11 WithAddLeftYAxis(Action) Returns",
            "13 WithYAxisTitleGap(string pixels) Type",
            "14 WithYAxisTitleGap(double gap) CS1739",
            "15 WithAddRightYAxis(Func<int>) Type",
        ], mismatches.Select(each => $"{each.Line} {each.Code} {each.Id}"));
        Assert.Equal("page.md, line 11: `WithAddLeftYAxis(Action)`: Returns: the call returns AxesBuilder, the document says FigureBuilder", mismatches[6].ToString());
    }

    [Fact]
    public void AnOptionalArgument_MustBeOptional_AndAPlaceholderStandsForAnyArguments()
    {
        string text = string.Join('\n',
        [
            "## `AxesBuilder`",
            "",
            "| Verb | What |",
            "|---|---|",
            "| `Psd(signal, options?)` | fits: options may be left out |",
            "| `SetXLim(min?, max)` | the first of two is never optional |",
            "| `WithTitle(…)` | any arguments |",
            "| `AddInset(bounds, …)` | the rest left out |",
        ]);

        var mismatches = CallClaim.InlineOf("page.md", text).Compiled().Mismatches;

        Assert.Equal(["6 SetXLim(min?, max)"], mismatches.Select(each => $"{each.Line} {each.Code}"));
    }

    [Fact]
    public void ACallOfThePackages_IsHeld_WhereTheCodeAroundItNamesWhatTheSuiteCannotLoad()
    {
        // A toolkit's type, a namespace of a head the suite cannot build, a class the block declares, a razor component, a block of the
        // repository's tests and a notebook cell each name something the packages do not have; the calls of the packages beside it are
        // calls all the same.
        string text = string.Join('\n',
        [
            "`new MplChartView { Figure = Plt.Create().WithLegend().Build() }` beside `new MplChartView { Figure = Plt.Create().WithTitle(\"t\").Build() }`.",
            "",
            "```csharp",
            "using MatPlotLibNet;",
            "using MatPlotLibNet.Maui;",
            "",
            "var chart = new MplChartView",
            "{",
            "    Figure = Plt.Create().Plot(time, values, l => l.Color = Color.Blue).Build()",
            "};",
            "```",
            "",
            "```csharp",
            "using System.Windows;",
            "",
            "public partial class ReplayWindow : Window",
            "{",
            "    private Figure Kept() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(xs, ys).SetXLim(xs[0], xs[^1])).Build();",
            "    private Figure Shown() => Plt.Create().WithLegend().Build();",
            "}",
            "```",
            "",
            "```razor",
            "@using MatPlotLibNet",
            "@inject FigureRegistry Registry",
            "<p>@((MarkupString)figure.ToSvg())</p>",
            "<MplChart Figure=\"@_figure\" />",
            "@code {",
            "    private Figure _figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(xs, xs).SetXLim(xs[0], xs[^1])).WithLegend().Build();",
            "",
            "    protected override void OnInitialized() => Registry.Register(\"id\", _figure);",
            "}",
            "```",
        ]);
        string page = string.Join('\n',
        [
            "<!-- needs: Tests -->",
            "```csharp",
            "using var h = InteractionScriptHarness.FromBuilder(b => b);",
            "Plt.Create().WithLegend();",
            "```",
            "",
            "<!-- a C# cell of a notebook -->",
            "```csharp",
            "#r \"nuget: MatPlotLibNet\"",
            "Plt.Create().WithTitle(\"t\").Build()",
            "```",
            "",
            "<!-- a C# cell of a notebook -->",
            "```csharp",
            "Plt.Create().SetXLim(0, 1).Build()",
            "```",
        ]);

        var mismatches = CallClaim.InlineOf("page.md", text).Concat(CallClaim.BlocksOf("page.md", text))
            .Concat(CallClaim.BlocksOf(WikiPage.Read("wiki.md", page))).Compiled().Mismatches;

        Assert.Equal(
        [
            "page.md 1 new MplChartView { Figure = Plt.Create().WithLegend().Build() } Unknown",
            "page.md 9 Figure = Plt.Create().Plot(time, values, l => l.Color = Color.Blue).Build() Unknown",
            "page.md 19 private Figure Shown() => Plt.Create().WithLegend().Build(); Unknown",
            "page.md 29 private Figure _figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(xs, xs).SetXLim(xs[0], xs[^1])).WithLegend().Build(); Unknown",
            "wiki.md 4 Plt.Create().WithLegend(); Unknown",
            "wiki.md 15 Plt.Create().SetXLim(0, 1).Build() Unknown",
        ], mismatches.Select(each => $"{each.Document} {each.Line} {each.Code} {each.Id}"));
    }

    [Fact]
    public void AMemberNoTypeHas_ReadOnAValueOfThePackages_IsAMismatch_WhereverTheCallStands()
    {
        // The receiver decides: a member called on a type of the packages that the type does not have is wrong in a sentence, a block, a
        // block the wiki sets aside and an XML comment alike, whether or not some other library has a member of that name.
        string text = string.Join('\n',
        [
            "`Plt.Creat()`, `Color.FromRgb(245, 245, 245)` and `Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithTitel(\"t\"))`.",
            "",
            "```csharp",
            "var figure = Plt.Creat();",
            "```",
            "",
            "```csharp",
            "var colour = Color.FromRgb(245, 245, 245);",
            "```",
            "",
            "```csharp",
            "Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithTitel(\"t\"));",
            "```",
            "",
            "## `AxesBuilder`",
            "",
            "| Call | What |",
            "|---|---|",
            "| `WithTitel(text)` | a title |",
            "",
            "A name of the reader's own as an argument excuses nothing: `Plt.Create().WithLegend(MyIndicator.Position)`.",
            "",
            "```csharp",
            "Plt.Create().WithLegend(MyIndicator.Position);",
            "```",
        ]);
        string page = string.Join('\n',
        [
            "<!-- a C# cell of a notebook -->",
            "```csharp",
            "Plt.Create().WithTitel(\"t\").Build()",
            "```",
        ]);
        const string source = """
            namespace N;

            /// <summary>Made by <c>Plt.Creat()</c>, coloured <c>Color.FromRgb(245, 245, 245)</c>, titled
            /// <c>Plt.Create().AddSubPlot(1, 1, 1, ax =&gt; ax.WithTitel("t"))</c>.</summary>
            /// <example><code>
            /// var background = Color.FromRgb(245, 245, 245);
            /// </code></example>
            public static class Holder { }
            """;
        var file = new SourceFile("Src/Lib/Receivers.cs", source);
        // A call written bare in the comment of a type of the packages is read on that type.
        var builder = new SourceFile("Src/Lib/AxesBuilder.cs", "namespace MatPlotLibNet;\n\n/// <summary>Titled by <c>WithTitel(\"t\")</c>.</summary>\npublic sealed partial class AxesBuilder { }\n");

        var mismatches = CallClaim.InlineOf("page.md", text).Concat(CallClaim.BlocksOf("page.md", text))
            .Concat(CallClaim.BlocksOf(WikiPage.Read("wiki.md", page))).Concat(CallClaim.InlineOf(file)).Concat(CallClaim.BlocksOf(file))
            .Concat(CallClaim.InlineOf(builder)).Compiled().Mismatches;

        Assert.Equal(
        [
            "page.md 1 Plt.Creat() Unknown",
            "page.md 1 Color.FromRgb(245, 245, 245) Unknown",
            "page.md 1 Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithTitel(\"t\")) Unknown",
            "page.md 19 WithTitel(text) Unknown",
            "page.md 21 Plt.Create().WithLegend(MyIndicator.Position) Unknown",
            "page.md 4 var figure = Plt.Creat(); Unknown",
            "page.md 8 var colour = Color.FromRgb(245, 245, 245); Unknown",
            "page.md 12 Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithTitel(\"t\")); Unknown",
            "page.md 24 Plt.Create().WithLegend(MyIndicator.Position); Unknown",
            "wiki.md 3 Plt.Create().WithTitel(\"t\").Build() Unknown",
            "Src/Lib/Receivers.cs 3 Plt.Creat() Unknown",
            "Src/Lib/Receivers.cs 3 Color.FromRgb(245, 245, 245) Unknown",
            "Src/Lib/Receivers.cs 4 Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithTitel(\"t\")) Unknown",
            "Src/Lib/Receivers.cs 6 var background = Color.FromRgb(245, 245, 245); Unknown",
            "Src/Lib/AxesBuilder.cs 3 WithTitel(\"t\") Unknown",
        ], mismatches.Select(each => $"{each.Document} {each.Line} {each.Code} {each.Id}"));
        Assert.Equal("Src/Lib/Receivers.cs, line 3: `Color.FromRgb(245, 245, 245)`: Unknown: Color has no member FromRgb", mismatches[11].ToString());
    }

    [Fact]
    public void ACallReadOnNoTypeOfThePackages_IsForeign_WithWhy()
    {
        const string text = "`MyIndicator.Compute()`, `hubConnection.StartAsync()` and `Plt.Create().WithTitle(\"t\")`.";

        var reading = CallClaim.InlineOf("page.md", text).Compiled();

        Assert.Empty(reading.Mismatches);
        Assert.Equal(
        [
            "page.md, line 1: `MyIndicator.Compute()`: Foreign: MyIndicator is a name an example makes up",
            "page.md, line 1: `hubConnection.StartAsync()`: Foreign: it compiles only on a value of a type the document leaves unsaid and no type of the packages is",
        ], reading.Foreign.Select(each => each.ToString()));

        // A document about the tests calls what the tests add to a type of the packages; a member they do not add is still wrong there.
        var tests = CallClaim.InlineOf("CONTRIBUTING.md", "Read back with `figure.RoundTripped()` or `figure.RoundTrippd()`.").Compiled();

        Assert.Equal(["CONTRIBUTING.md, line 1: `figure.RoundTripped()`: Foreign: RoundTripped is a member the repository's tests add to Figure"], tests.Foreign.Select(each => each.ToString()));
        Assert.Equal(["CONTRIBUTING.md, line 1: `figure.RoundTrippd()`: Unknown: FigureBuilder has no member RoundTrippd"], tests.Mismatches.Select(each => each.ToString()));
    }

    [Fact]
    public void APythonCallMatplotlibHas_Fits_AndOneItDoesNotHave_IsNamedWithWhatIsWrong()
    {
        string text = string.Join('\n',
        [
            "`ax.plot(x, y)`, `ax.plot(x, y, color='steelblue', linewidth=2)`, `fig.savefig('c.png', dpi=300)`, `plt.subplots(2, 3)`,",
            "`ax.hist(data, bins=20)`, `scipy.stats.probplot(x, plot=ax)`, `plt.subplots_adjust(hspace=…)`, `ax.spines['top'].set_visible(False)`,",
            "`ax.xaxis.set_major_formatter(f)`, `Rectangle((x, y), w, h, angle=a)`, `sns.kdeplot(data)`, `plt.gca`, matplotlib's `loc='best'`,",
            "`plt.hold(True)`, `ax.hist(data, binz=20)`, `fig.canvas.tostring_rgb()`, `ax.plot(x, y, colr='r')`, `scipy.linkage`, `ax.set_xlim(1, 2, 3, 4, 5)`,",
            "`nosuchkeyword='x'` is no keyword of matplotlib.",
        ]);

        var mismatches = PythonCall.InlineOf("page.md", text).Select(call => call.Missing(PythonSignatures.Matplotlib)).OfType<string>().ToList();

        Assert.Equal(
        [
            "page.md, line 4: `plt.hold(True)`: matplotlib.pyplot has no hold",
            "page.md, line 4: `ax.hist(data, binz=20)`: hist takes no keyword binz",
            "page.md, line 4: `fig.canvas.tostring_rgb()`: FigureCanvasBase has no tostring_rgb",
            "page.md, line 4: `ax.plot(x, y, colr='r')`: plot takes no keyword colr",
            "page.md, line 4: `scipy.linkage`: scipy has no linkage",
            "page.md, line 4: `ax.set_xlim(1, 2, 3, 4, 5)`: set_xlim takes at most 2 arguments, the document gives 5",
            "page.md, line 5: `nosuchkeyword='x'`: no call of matplotlib takes the keyword nosuchkeyword",
        ], mismatches);
    }

    [Fact]
    public void APythonCall_IsHeld_InEverySpellingADocumentWritesItIn()
    {
        string text = string.Join('\n',
        [
            "A keyword with blanks, `ndivz = 'auto'`, a list of keywords, `loc='best', bbox_to_anchr=(1, 0.5)`, and a keyword beside a",
            "class of another library: `FancyBboxPatch`'s `rounding_sizz=0.2`. A span that wraps, `fig.suptitle(title, fontsize=14,",
            "fontweigt='bold')`, a keyword outside a span, markeredgecolr='auto', and a call outside one, matplotlib's set_aspect('equal', adjustablz='box').",
            "",
            "An attribute of markup, `role=\"img\"`, a statement of matplotlib's source, `pad = self._pad + 7`, values, `x=41895, y=83826`, and a",
            "keyword matplotlib takes, `nbins = 'auto'`, `subs=(1.0,)`, hold nothing wrong; `scatter.edgecolorz = 'face'`, `cellLok='right'` and `nbinz = 9` do.",
            "```",
            "Tree: Auto (ndivz='auto'), a table at ax.table(loc='bottom', nosuch=1), Foo.Bar(x) of the library, the setting xtick.minor.ndivz='auto'",
            "```",
            "```python",
            "fig, ax = plt.subplots()",
            "ax.plot(x, y, colr='r')",
            "```",
        ]);

        var mismatches = PythonCall.InlineOf("page.md", text).Select(call => call.Missing(PythonSignatures.Matplotlib)).OfType<string>().ToList();

        Assert.Equal(
        [
            "page.md, line 1: `ndivz = 'auto'`: no call of matplotlib takes the keyword ndivz",
            "page.md, line 1: `loc='best', bbox_to_anchr=(1, 0.5)`: no call of matplotlib takes the keyword bbox_to_anchr",
            "page.md, line 2: `rounding_sizz=0.2`: no call of matplotlib takes the keyword rounding_sizz",
            "page.md, line 2: `fig.suptitle(title, fontsize=14, fontweigt='bold')`: suptitle takes no keyword fontweigt",
            "page.md, line 2: `set_aspect('equal', adjustablz='box')`: set_aspect takes no keyword adjustablz",
            "page.md, line 2: `markeredgecolr='auto'`: no call of matplotlib takes the keyword markeredgecolr",
            "page.md, line 6: `scatter.edgecolorz = 'face'`: matplotlib has no setting scatter.edgecolorz",
            "page.md, line 6: `cellLok='right'`: no call of matplotlib takes the keyword cellLok",
            "page.md, line 6: `nbinz = 9`: no call of matplotlib takes the keyword nbinz",
            "page.md, line 8: `ax.table(loc='bottom', nosuch=1)`: table takes no keyword nosuch",
            "page.md, line 8: `ndivz='auto'`: no call of matplotlib takes the keyword ndivz",
            "page.md, line 8: `xtick.minor.ndivz='auto'`: matplotlib has no setting xtick.minor.ndivz",
            "page.md, line 12: `ax.plot(x, y, colr='r')`: plot takes no keyword colr",
        ], mismatches);

        const string source = """
            namespace N;

            /// <summary>The corners are <c>FancyBboxPatch</c>'s <c>rounding_sizz=0.2</c>, the split <c>ndivz = 'auto'</c>; outside a span
            /// stands nonpositiv='clip', and <c>role="img"</c> is markup.</summary>
            public static class Holder { }
            """;

        Assert.Equal(
        [
            "Src/Lib/Corners.cs, line 3: `rounding_sizz=0.2`: no call of matplotlib takes the keyword rounding_sizz",
            "Src/Lib/Corners.cs, line 3: `ndivz = 'auto'`: no call of matplotlib takes the keyword ndivz",
            "Src/Lib/Corners.cs, line 4: `nonpositiv='clip'`: no call of matplotlib takes the keyword nonpositiv",
        ], PythonCall.InlineOf(new SourceFile("Src/Lib/Corners.cs", source)).Select(call => call.Missing(PythonSignatures.Matplotlib)).OfType<string>());
    }

    [Fact]
    public void ACallInASpanThatWrapsALine_IsReadWhole()
    {
        const string text = "The verb `FigImage(data, xOffset,\nyOffset)` places an image, and `Plt.Create()` starts.\n\nAn open `tick alone\n\nstays `open`.";

        Assert.Equal(["1 FigImage(data, xOffset, yOffset)", "1 Plt.Create()"], CallClaim.InlineOf("page.md", text).Select(claim => $"{claim.Line} {claim.Code}"));
    }

    [Fact]
    public void EveryCallAWikiPageWrites_IsACallThePackagesHave()
    {
        var wiki = Wiki.BesideTheRepository;

        // A record of what a release published writes calls as they were; the migration page is read apart from its Removed columns.
        var pages = wiki.Pages().Where(page => !DeletedName.PublishedRecords.Contains(page.Name) || page.Name == MigrationPage).ToList();
        // The csharp blocks the wiki compiles as programs are compiled there; the blocks it sets aside and the razor fences are read here.
        var calls = pages.SelectMany(page => CallClaim.InlineOf(page.Name, wiki.Text(page.Name))
            .Concat(CallClaim.BlocksOf(page)).Concat(CallClaim.RazorBlocksOf(page.Name, wiki.Text(page.Name)))).ToList();
        var python = pages.SelectMany(page => PythonCall.InlineOf(page.Name, wiki.Text(page.Name))).ToList();

        AssertFits(calls, python, FewestWikiCalls, "the wiki's pages");
    }

    [Fact]
    public void EveryCallADocumentOfTheRepositoryWrites_IsACallThePackagesHave()
    {
        var documents = RepositoryTree.CurrentStateDocuments()
            .Where(document => !document.Path.StartsWith(Records, StringComparison.Ordinal) && !DeletedName.PublishedRecords.Contains(document.Path))
            .ToList();
        var calls = documents.SelectMany(document => CallClaim.InlineOf(document.Path, document.Text)).ToList();
        var blocks = documents.SelectMany(document => CallClaim.BlocksOf(document.Path, document.Text)).ToList();
        var python = documents.SelectMany(document => PythonCall.InlineOf(document.Path, document.Text)).ToList();

        Assert.True(blocks.Count >= FewestRepositoryBlocks, $"the documents hold {blocks.Count} csharp blocks, fewer than the {FewestRepositoryBlocks} measured");
        AssertFits([.. calls, .. blocks], python, FewestRepositoryCalls, "the repository's documents");
    }

    [Fact]
    public void EveryCallTheXmlDocumentationWrites_IsACallThePackagesHave()
    {
        var files = RepositoryTree.SourceFiles();
        var calls = files.SelectMany(file => CallClaim.InlineOf(file).Concat(CallClaim.BlocksOf(file))).ToList();
        var python = files.SelectMany(PythonCall.InlineOf).ToList();

        AssertFits(calls, python, FewestXmlCalls, "the XML documentation");
    }

    private static void AssertFits(IReadOnlyList<CallClaim> calls, IReadOnlyList<PythonCall> python, int fewest, string what)
    {
        Assert.True(calls.Count >= fewest, $"{what} hold {calls.Count} calls, fewer than the {fewest} measured");

        var reading = calls.Compiled();
        var mismatches = reading.Mismatches.Select(each => each.ToString()).Concat(python.Select(call => call.Missing(PythonSignatures.Matplotlib)).OfType<string>()).ToList();

        // A call read as foreign says nothing of the packages: each is shown with why, so a reader sees what the guard set aside.
        TestContext.Current.TestOutputHelper?.WriteLine($"{reading.Foreign.Count} calls of {what} read as foreign:");

        foreach (var foreign in reading.Foreign)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(foreign.ToString());
        }

        Assert.True(mismatches.Count == 0, $"{mismatches.Count} calls of {what} that the packages or matplotlib do not have:\n" + string.Join('\n', mismatches));
    }

    [Fact]
    public void TheComparisonsWithMatplotlib_HoldAsManyPythonCallsAsMeasured()
    {
        var documents = RepositoryTree.CurrentStateDocuments().Where(document => !document.Path.StartsWith(Records, StringComparison.Ordinal));
        var count = documents.Sum(document => PythonCall.InlineOf(document.Path, document.Text).Count);

        Assert.True(count >= FewestPythonCalls, $"the documents hold {count} matplotlib calls, fewer than the {FewestPythonCalls} measured");
    }
}
