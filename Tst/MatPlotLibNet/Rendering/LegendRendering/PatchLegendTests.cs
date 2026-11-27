// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>A labelled patch is a row of the legend, as matplotlib lists one: a rectangle swatch in the patch's own style, among the
/// rows of the series in the order they were added, toggled like a series row. A patch without a label, or with one starting with an
/// underscore, has no row. The legend of the axes and the legend of the figure list it, and the box is measured with it.</summary>
public class PatchLegendTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    private static Rectangle Region(string? label) => new(new(0.2, 1.2), 1.0, 1.0) { Label = label };

    /// <summary>The texts of the first legend group drawn: the legend of the first axes, or the figure's when the axes draw none.</summary>
    private static string[] LegendTexts(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int begin = ctx.Calls.FindIndex(c => c is BeginGroupCall { CssClass: "legend" });
        Assert.True(begin >= 0, "no legend group was drawn");
        int end = ctx.Calls.FindIndex(begin, c => c is EndGroupCall);
        return [.. ctx.Calls.GetRange(begin, end - begin + 1).OfType<DrawTextCall>().Select(c => c.Text)];
    }

    /// <summary>The texts of the last legend group drawn: the figure's legend, drawn after every subplot.</summary>
    private static string[] FigureLegendTexts(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int begin = ctx.Calls.FindLastIndex(c => c is BeginGroupCall { CssClass: "legend" });
        Assert.True(begin >= 0, "no legend group was drawn");
        int end = ctx.Calls.FindIndex(begin, c => c is EndGroupCall);
        return [.. ctx.Calls.GetRange(begin, end - begin + 1).OfType<DrawTextCall>().Select(c => c.Text)];
    }

    private static List<(int Index, string Label)> ToggledEntries(string svg) =>
        [.. svg.LegendEntries().Select(entry => (entry.Index, entry.Label))];

    private static IEnumerable<string> SeriesIndicesOf(string svg) =>
        svg.Elements("g").Where(group => group["class"] == "series" && group["data-series-index"] is not null).Select(group => group["data-series-index"]!);

    // ── Which patches have a row ───────────────────────────────────────────

    [Fact]
    public void ALabelledPatch_IsARowOfTheLegend()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "line").AddPatch(Region("region"))).Build();

        Assert.Equal(["line", "region"], LegendTexts(figure));
    }

    /// <summary>matplotlib's rule (<c>_get_legend_handles_labels</c>): an empty label, or one starting with an underscore, is left out.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("_")]
    [InlineData("_hidden")]
    public void APatchWithoutALabel_OrWithOneStartingWithAnUnderscore_HasNoRow(string? label)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "line").AddPatch(Region(label))).Build();

        Assert.Equal(["line"], LegendTexts(figure));
    }

    [Fact]
    public void AnUnderscoreAnywhereButFirst_KeepsTheRow()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(Region("a_b"))
            .AddPatch(Region(" _lead"))).Build();

        Assert.Equal(["line", "a_b", " _lead"], LegendTexts(figure));
    }

    /// <summary>Whether a patch is listed is its label's say alone, as in matplotlib: a patch of no opacity keeps its row.</summary>
    [Fact]
    public void AClearPatch_IsStillListed()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(new Circle(new(1, 2), 0.5) { Label = "clear", Alpha = 0 })).Build();

        Assert.Equal(["clear"], LegendTexts(figure));
    }

    /// <summary>matplotlib lists the rows in the order the artists were added (<c>_get_legend_handles</c> walks <c>ax._children</c>):
    /// matplotlib 3.11.2, add_patch P then plot L prints <c>['P', 'L']</c>, and add_patch first, plot line, add_patch second, scatter
    /// points prints <c>['first', 'line', 'second', 'points']</c>. Under every theme: it is what the legend says, not a look.</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("default")]
    public void TheRows_ComeInTheOrderTheSeriesAndThePatchesWereAdded(string theme)
    {
        var figure = Plt.Create().WithTheme(theme.AsTheme()).AddSubPlot(1, 1, 1, ax => ax
            .AddPatch(Region("first"))
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(new Wedge(new(1, 2), 0.5, 0, 90) { Label = "second" })
            .Scatter(X, Y, s => s.Label = "points")).Build();

        Assert.Equal(["first", "line", "second", "points"], LegendTexts(figure));
    }

    [Fact]
    public void APatchAddedBeforeALine_IsListedFirst()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(Region("P")).Plot(X, Y, s => s.Label = "L")).Build();

        Assert.Equal(["P", "L"], LegendTexts(figure));
    }

    /// <summary>A series of a second Y axis and of the top X axis stands after the rows of the axes' own series and patches, as
    /// matplotlib's figure legend lists a twin axes' rows after the rows of the axes it twins.</summary>
    [Fact]
    public void APatchAddedAfterTheLastSeriesOfTheAxes_StandsBeforeTheSeriesOfASecondYAxis()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.YAxes.StackOn(AxisSide.Right).Add().AddSeries(new MatPlotLibNet.Models.Series.LineSeries(X, Y) { Label = "right" });
        axes.Plot(X, Y).Label = "own";
        axes.AddPatch(Region("patch"));

        var renderer = AxesRenderer.Create(axes, new Rect(0, 0, 400, 300), new SvgRenderContext(), Theme.Default);

        Assert.Equal(["own", "patch", "right"], renderer.LegendEntries(legendValues: false).Select(e => e.Label));
    }

    /// <summary>Read back from its JSON, a figure lists its rows in the order they were added.</summary>
    [Fact]
    public void TheOrderOfTheRows_IsKeptThroughJson()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .AddPatch(Region("first"))
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(new Wedge(new(1, 2), 0.5, 0, 90) { Label = "second" })
            .Scatter(X, Y, s => s.Label = "points")
            .AddPatch(Region("last"))).Build();

        var read = figure.RoundTripped();

        Assert.Equal(["first", "line", "second", "points", "last"], LegendTexts(read));
    }

    /// <summary>A patch added after every series of its axes writes no place: the JSON of such a figure is what an earlier release
    /// wrote, and a file of an earlier release (which lists the patches after the series) reads back that way.</summary>
    [Fact]
    public void APatchAfterEverySeries_WritesNoPlace_AndOneBeforeASeriesDoes()
    {
        var serializer = new MatPlotLibNet.Serialization.ChartSerializer();
        string after = serializer.ToJson(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y).AddPatch(Region("p"))).Build());
        string before = serializer.ToJson(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(Region("p")).Plot(X, Y)).Build());

        Assert.DoesNotContain("seriesBefore", after);
        Assert.Matches("\"seriesBefore\"\\s*:\\s*0", before);
    }

    [Fact]
    public void APatchRow_TakesNoLegendValue()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithLegendValues()
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(Region("region"))).Build();

        Assert.Equal(["line = 2.00", "region"], LegendTexts(figure));
    }

    [Fact]
    public void ALegendOfPatchesOnly_ListsThem()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .AddPatch(Region("region"))
            .AddPatch(new Arrow(new(0, 0), 1, 1) { Width = 0.05, Label = "arrow" })
            .AddPatch(new Arc(new(1, 1), 1, 1) { Label = "arc" })).Build();

        Assert.Equal(["region", "arrow", "arc"], LegendTexts(figure));
    }

    /// <summary>A polar axes draws its patches and lists them, as matplotlib's polar axes does (<c>add_patch</c> puts the patch among
    /// its children, drawn through <c>transData</c>).</summary>
    [Fact]
    public void APatchOnPolarAxes_IsDrawn_AndHasARow()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0], s => s.Label = "P")
            .AddPatch(Region("region"))).Build();

        Assert.Equal(["P", "region"], LegendTexts(figure));
    }

    /// <summary>The renderer that draws an axes says whether it draws patches, and its legend lists them only then: every renderer
    /// the library ships does, a Cartesian axes, an inset, a polar, a 3-D and a ternary axes.</summary>
    [Theory]
    [InlineData(CoordinateSystem.Cartesian, false, true)]
    [InlineData(CoordinateSystem.Cartesian, true, true)]
    [InlineData(CoordinateSystem.Polar, false, true)]
    [InlineData(CoordinateSystem.ThreeD, false, true)]
    [InlineData(CoordinateSystem.Ternary, false, true)]
    public void OnlyARendererThatDrawsPatches_ListsThem(CoordinateSystem system, bool inset, bool listed)
    {
        var parent = new Axes { CoordinateSystem = system };
        var axes = inset ? parent.AddInset(new InsetBounds(0.5, 0.5, 0.4, 0.4)) : parent;
        axes.Plot(X, Y).Label = "line";
        axes.AddPatch(Region("region"));

        var renderer = AxesRenderer.Create(axes, new Rect(0, 0, 400, 300), new SvgRenderContext(), Theme.Default);

        string[] expected = listed ? ["line", "region"] : ["line"];
        Assert.Equal(listed, renderer.DrawsPatches);
        Assert.Equal(expected, renderer.LegendEntries(legendValues: false).Select(e => e.Label));
    }

    // ── The swatch ─────────────────────────────────────────────────────────

    /// <summary>Everything the legend draws for <paramref name="entry"/> alone, with no frame: the calls before its text.</summary>
    private static List<CallRecord> SwatchOf(LegendEntry entry, out LegendLayout layout)
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(new Legend { Position = LegendPosition.UpperRight, FrameOn = false }, Theme.Default, ctx);
        layout = renderer.Place([entry], new Rect(80, 60, 400, 300), () => MarkObstacles.None)!;
        renderer.Draw([entry], layout, interactive: true);
        return [.. ctx.Calls.Skip(1).TakeWhile(c => c is not (DrawTextCall or DrawRichTextCall))];
    }

    /// <summary>matplotlib's <c>HandlerPatch</c>: the handle is a rectangle the full handle long and high, in the patch's own face,
    /// edge, width, dash and hatch, the opacity already inside each colour (no group opacity).</summary>
    [Fact]
    public void APatchRow_IsARectangleSwatch_TheFullHandle_InThePatchsOwnStyle()
    {
        var patch = new Rectangle(new(0, 0), 1, 1)
        {
            Label = "styled", FaceColor = Colors.Tab10Orange, EdgeColor = Colors.Black, EdgeWidth = 2,
            EdgeLineStyle = LineStyle.Dashed, Alpha = 0.5, Hatch = HatchPattern.Cross,
        };

        var calls = SwatchOf(new PatchLegendEntry(patch, "styled", 0), out var layout);

        var swatch = Assert.IsType<DrawRectangleCall>(Assert.Single(calls));
        Assert.Equal(new Rect(layout.ColumnX(0), layout.EntryY(0), layout.SwatchWidth, layout.SwatchHeight), swatch.Rect);
        var style = swatch.Shape;
        Assert.Equal(patch.DrawnStyleUnder(Theme.Default), style);
        Assert.Equal(Colors.Tab10Orange.ScaleAlpha(0.5), style.Fill);
        Assert.Equal(Colors.Black.ScaleAlpha(0.5), style.Stroke);
        Assert.Equal((2.0, LineStyle.Dashed, HatchPattern.Cross), (style.StrokeThickness, style.StrokeLineStyle, style.Hatch));
    }

    public static TheoryData<string> Kinds => ["Rectangle", "Polygon", "Arrow", "Circle", "Ellipse", "Wedge", "Arc"];

    private static Patch Of(string kind) => kind switch
    {
        "Rectangle" => new Rectangle(new(0, 0), 1, 1),
        "Polygon" => new Polygon([new(0, 0), new(1, 0), new(0, 1)]) { EdgeColor = Colors.Red },
        "Arrow" => new Arrow(new(0, 0), 1, 1),
        "Circle" => new Circle(new(0, 0), 1) { FaceColor = Colors.Green },
        "Ellipse" => new Ellipse(new(0, 0), 2, 1) { Hatch = HatchPattern.Dots },
        "Wedge" => new Wedge(new(0, 0), 1, 0, 90) { Alpha = 0.25 },
        "Arc" => new Arc(new(0, 0), 2, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Every kind of patch has the one swatch, as every patch has in matplotlib: an arc's is an outline with no face.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void EveryKindOfPatch_HasTheOneRectangleSwatch_InItsDrawnStyle(string kind)
    {
        var patch = Of(kind);
        patch.Label = kind;

        var swatch = Assert.IsType<DrawRectangleCall>(Assert.Single(SwatchOf(new PatchLegendEntry(patch, kind, 0), out _)));

        Assert.Equal(patch.DrawnStyleUnder(Theme.Default), swatch.Shape);
    }

    /// <summary>matplotlib refuses to fill an arc, and its legend handle is a rectangle with no face and the arc's black edge.</summary>
    [Fact]
    public void AnArcsSwatch_IsAnOutlineWithNoFace()
    {
        var style = Assert.IsType<DrawRectangleCall>(Assert.Single(SwatchOf(new PatchLegendEntry(new Arc(new(0, 0), 2, 1) { Label = "arc" }, "arc", 0), out _))).Shape;

        Assert.Null(style.Fill);
        Assert.Equal(Colors.Black, style.Stroke);
    }

    [Fact]
    public void APatchRow_IsLabelledByThePatch_AndTogglesTheIndexItIsGiven()
    {
        var entry = new PatchLegendEntry(Region("region"), "region", 3);

        Assert.Equal("region", entry.Label);
        Assert.Equal(3, entry.ToggleIndex);
    }

    /// <summary>Two patch rows look alike, which a figure legend lists once, when they print the same label in the same drawn style;
    /// another label, another style or a series row looks different.</summary>
    [Fact]
    public void APatchRow_LooksLikeAnotherOfTheSameLabelAndStyle_Only()
    {
        var row = new PatchLegendEntry(Region("region"), "region", 0);

        Assert.True(row.LooksLike(new PatchLegendEntry(Region("region"), "region", 4), Theme.Default));
        Assert.False(row.LooksLike(new PatchLegendEntry(Region("other"), "other", 0), Theme.Default));
        Assert.False(row.LooksLike(new PatchLegendEntry(new Rectangle(new(0, 0), 1, 1) { Label = "region", FaceColor = Colors.Red }, "region", 0), Theme.Default));
        Assert.False(row.LooksLike(new SeriesLegendEntry(new MatPlotLibNet.Models.Series.LineSeries(X, Y), 0, 0, "region", MatPlotLibNet.Models.Series.LegendSwatch.Patch), Theme.Default));
    }

    // ── Toggled like a series ──────────────────────────────────────────────

    /// <summary>A patch is toggled by the index after every series of its axes: the series keep their identity slots
    /// (<see cref="Axes.AllSeries"/>) and the patch at place <c>i</c> of <see cref="Axes.Patches"/> takes the series count plus
    /// <c>i</c>, labelled or not, so the index of a patch does not move when another patch gains or loses its label.</summary>
    [Fact]
    public void APatchRow_CarriesTheIndexAfterEverySeriesOfItsAxes()
    {
        var axes = new Axes();
        axes.AddPatch(Region(null));
        axes.Plot(X, Y).Label = "line";
        axes.YAxes.StackOn(AxisSide.Right).Add().AddSeries(new MatPlotLibNet.Models.Series.LineSeries(X, Y) { Label = "right" });
        axes.AddPatch(Region("region"));

        var entries = AxesRenderer.Create(axes, new Rect(0, 0, 400, 300), new SvgRenderContext(), Theme.Default).LegendEntries(false);

        Assert.Equal([("line", 0), ("region", 3), ("right", 1)], entries.Select(e => (e.Label, e.ToggleIndex)));
    }

    /// <summary>In an interactive picture the patch is drawn in a group carrying its index, as a series is, and its row toggles that
    /// index, so the browser's legend toggle hides and shows the patch.</summary>
    [Fact]
    public void APatchRow_HasAToggleGroup_AndThePatchIsDrawnInAGroupOfItsIndex_InAnInteractiveSvg()
    {
        var figure = Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(Region("region"))).Build();

        string svg = figure.ToSvg();

        Assert.Equal([(0, "line"), (1, "region")], ToggledEntries(svg));
        // The patch's group holds the clip that cuts its outline (a group drawn in a clip only ever holds marks).
        var patch = svg.Each("g").Single(group => (string?)group.Attribute("class") == "patch" && (string?)group.Attribute("data-series-index") == "1"
            && (string?)group.Attribute("aria-label") == "region");
        var first = patch.Elements().First();
        Assert.True(first.Is("path") || first.Is("g") && first.Attribute("clip-path") is not null && first.Elements().First().Is("path"));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // A picture that is not interactive draws a patch without a group, as before.
        new DrawnToken("APatch_IsDrawnWithoutAGroup_InAPictureThatIsNotInteractive", () =>
        {
            var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "line").AddPatch(Region("region"))).Build();
            return figure.ToSvg();
        }) { Absent = ["class=\"patch\""] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The index a series' row and its drawn group carry is its own identity slot, with or without patches beside it.</summary>
    [Fact]
    public void TheSeriesIndices_AreTheSame_WithAndWithoutPatches()
    {
        FigureBuilder Chart(bool patches) => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax =>
        {
            if (patches) ax.AddPatch(Region("before"));
            ax.Plot(X, Y).Plot(X, Y, s => s.Label = "B").Scatter(X, Y, s => s.Label = "C");
            if (patches) ax.AddPatch(Region("after"));
        });

        string with = Chart(true).Build().ToSvg();
        string without = Chart(false).Build().ToSvg();

        Assert.Equal([(3, "before"), (1, "B"), (2, "C"), (4, "after")], ToggledEntries(with));
        Assert.Equal([(1, "B"), (2, "C")], ToggledEntries(without));
        Assert.Equal(SeriesIndicesOf(without), SeriesIndicesOf(with));
    }

    /// <summary>A click on a patch's row raises the toggle of the patch's index; the event hides the patch, the next one shows it
    /// again, and a hidden patch is not drawn while its row stays in the legend.</summary>
    [Fact]
    public void AClickOnAPatchRow_TogglesThePatch()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .WithLegend(LegendPosition.UpperRight)
            .Plot(X, Y, s => s.Label = "line")
            .AddPatch(Region("region"))).Build();
        var result = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());
        var layout = ChartLayout.Create(figure, result);
        var events = new List<FigureInteractionEvent>();
        var toggle = new LegendToggleModifier("chart", layout, events.Add);

        Assert.Equal(2, result.LegendItems[0].Count);
        var patchRow = result.LegendItems[0][1];
        var onPatchRow = new PointerInputArgs(patchRow.Bounds.X + 2, patchRow.Bounds.Y + patchRow.Bounds.Height / 2, PointerButton.Left, ModifierKeys.None);

        Assert.True(toggle.HandlesPointerPressed(onPatchRow));
        toggle.OnPointerPressed(onPatchRow);
        var click = Assert.IsType<LegendToggleEvent>(Assert.Single(events));
        Assert.Equal(1, click.SeriesIndex);

        var patch = figure.SubPlots[0].Patches[0];
        int drawnBefore = figure.ToSvg().Elements("path").Count;
        click.ApplyTo(figure);
        Assert.False(patch.Visible);
        string hidden = figure.ToSvg();
        Assert.Equal(drawnBefore - 1, hidden.Elements("path").Count);
        Assert.Contains(">region<", hidden);

        click.ApplyTo(figure);
        Assert.True(patch.Visible);
    }

    /// <summary>An index past the series and the patches of the axes toggles nothing, as one past the series always did.</summary>
    [Fact]
    public void AToggleOfAnIndexPastThePatches_ChangesNothing()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y).AddPatch(Region("region"))).Build();

        new LegendToggleEvent("chart", 0, 2).ApplyTo(figure);

        Assert.True(figure.SubPlots[0].Patches[0].Visible);
    }

    // ── The legend of the figure ───────────────────────────────────────────

    [Fact]
    public void TheFigureLegend_ListsTheRowsOfEachSubplot_InTheOrderTheyWereAdded()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).AddPatch(Region("p1")).Plot(X, Y, s => s.Label = "a1"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "b1"))
            .FigLegend()
            .Build();

        Assert.Equal(["p1", "a1", "b1"], FigureLegendTexts(figure));
    }

    /// <summary>Under the library's own themes a row drawn exactly like one listed already is left out of the figure's legend.</summary>
    [Fact]
    public void TheFigureLegend_ListsAPatchDrawnTheSameInTwoSubplots_Once_UnderTheLibrarysThemes()
    {
        var figure = Plt.Create().WithTheme(Theme.Default)
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).AddPatch(Region("zone")))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).AddPatch(Region("zone")))
            .FigLegend()
            .Build();

        Assert.Equal(["zone"], FigureLegendTexts(figure));
    }

    /// <summary>matplotlib's <c>fig.legend()</c> lists every row of every axes and leaves none out (matplotlib 3.11.2: two axes that
    /// each plot a line labelled L print <c>['L', 'L']</c>, two axes that each add a rectangle labelled zone print
    /// <c>['zone', 'zone']</c>), which the matplotlib themes follow.</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheFigureLegend_ListsEveryRowOfEverySubplot_UnderTheMatplotlibThemes(string style)
    {
        var theme = style.AsTheme();
        var patches = Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).AddPatch(Region("zone")))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).AddPatch(Region("zone")))
            .FigLegend()
            .Build();
        var lines = Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "L"; s.Color = Colors.Tab10Blue; }))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "L"; s.Color = Colors.Tab10Blue; }))
            .FigLegend()
            .Build();

        Assert.Equal(["zone", "zone"], FigureLegendTexts(patches));
        Assert.Equal(["L", "L"], FigureLegendTexts(lines));
    }

    [Fact]
    public void TheFigureLegend_ListsTheSamePatchLabelInAnotherStyle_Again()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).AddPatch(Region("zone")))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).AddPatch(new Rectangle(new(0, 0), 1, 1) { Label = "zone", Hatch = HatchPattern.Cross }))
            .FigLegend()
            .Build();

        Assert.Equal(["zone", "zone"], FigureLegendTexts(figure));
    }

    /// <summary>A series and a patch are drawn differently in a legend, whatever their colour, so one label on both is two rows.</summary>
    [Fact]
    public void TheFigureLegend_ListsASeriesAndAPatchOfOneLabel_Both()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Bar(["a", "b"], [1.0, 2.0], s => { s.Label = "x"; s.Color = Colors.Tab10Blue; }))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).AddPatch(Region("x")))
            .FigLegend()
            .Build();

        Assert.Equal(["x", "x"], FigureLegendTexts(figure));
    }
}
