// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The data of a Cartesian axes (its grid, spans, patches, reference lines, drawing tools and series) is drawn in one
/// z-order. Under the matplotlib themes it is matplotlib's: <c>axes.axisbelow</c> puts the grid at 1.5 under the default style
/// (<c>'line'</c>) and at 2.5 under the classic one (<c>False</c>); a patch, a span, a bar, a fill or a scatter stands at 1, a line,
/// an <c>axhline</c> or a contour line at 2, an image at 0 (matplotlib 3.11.2, printed with python:
/// <c>ax.xaxis.get_zorder()</c> 1.5 and 2.5, <c>Rectangle</c> / <c>PathCollection</c> / <c>FillBetweenPolyCollection</c> 1,
/// <c>Line2D</c> / <c>LineCollection</c> 2, <c>AxesImage</c> 0). A <c>ZOrder</c> the caller gave wins, and a patch takes one too.
/// Under the library's own themes the grid stays under everything and the series keep their <see cref="ISeries.ZOrder"/>.</summary>
public class AxesZOrderTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly Color GridInk = Color.FromHex("#0A0B0C");
    private static readonly Color BarInk = Color.FromHex("#B0A001");
    private static readonly Color LineInk = Color.FromHex("#1100EE");
    private static readonly Color PatchInk = Color.FromHex("#123456");
    private static readonly Color SpanInk = Color.FromHex("#FE0102");
    private static readonly Color RuleInk = Color.FromHex("#00FE03");

    /// <summary>Draws the one subplot <paramref name="build"/> makes, with a grid in <see cref="GridInk"/>, under
    /// <paramref name="theme"/>.</summary>
    private static RecordingRenderContext Draw(Theme theme, Func<AxesBuilder, AxesBuilder> build)
    {
        var figure = Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => build(ax.WithGrid(g => g with { Visible = true, Color = GridInk }))).Build();
        var ctx = new RecordingRenderContext();
        new CartesianAxesRenderer(figure.SubPlots[0], Plot, ctx, theme).Render();
        return ctx;
    }

    private static int First<TCall>(RecordingRenderContext ctx, Func<TCall, bool> match) where TCall : CallRecord =>
        ctx.Calls.FindIndex(c => c is TCall call && match(call));

    private static int Last<TCall>(RecordingRenderContext ctx, Func<TCall, bool> match) where TCall : CallRecord =>
        ctx.Calls.FindLastIndex(c => c is TCall call && match(call));

    private static int FirstGrid(RecordingRenderContext ctx) => First<DrawLineCall>(ctx, a => Equals(a.Color, GridInk));
    private static int LastGrid(RecordingRenderContext ctx) => Last<DrawLineCall>(ctx, a => Equals(a.Color, GridInk));
    private static int Bar(RecordingRenderContext ctx) => First<DrawRectangleCall>(ctx, a => Equals(a.Fill, BarInk));
    private static int Line(RecordingRenderContext ctx) => First<DrawLinesCall>(ctx, a => Equals(a.Color, LineInk));
    private static int PatchAt(RecordingRenderContext ctx) => First<DrawPathCall>(ctx, a => Equals(a.Fill, PatchInk));
    private static int Span(RecordingRenderContext ctx) => First<DrawRectangleCall>(ctx, a => Equals(a.Fill, SpanInk));
    private static int Rule(RecordingRenderContext ctx) => First<DrawLineCall>(ctx, a => Equals(a.Color, RuleInk));

    private static AxesBuilder BarThenLine(AxesBuilder ax) => ax
        .Bar(["a", "b", "c"], [1.0, 3.0, 2.0], b => b.Color = BarInk)
        .Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => l.Color = LineInk);

    // ---- the grid's place ------------------------------------------------------------------------------------------------

    /// <summary>Under the default style the grid stands at 1.5: over a bar (1), under a line (2).</summary>
    [Fact]
    public void UnderV2_ABarIsDrawnBeforeTheGrid_AndTheGridBeforeALine()
    {
        var ctx = Draw(Theme.MatplotlibV2, BarThenLine);

        int bar = Bar(ctx), first = FirstGrid(ctx), last = LastGrid(ctx), line = Line(ctx);
        Assert.True(bar >= 0 && first > bar && line > last, $"bar {bar}, grid {first}..{last}, line {line}");
    }

    /// <summary>Under the classic style the grid stands at 2.5: over the line too.</summary>
    [Fact]
    public void UnderClassic_TheGridIsDrawnAfterTheLine()
    {
        var ctx = Draw(Theme.MatplotlibClassic, BarThenLine);

        int bar = Bar(ctx), line = Line(ctx), first = FirstGrid(ctx);
        Assert.True(bar >= 0 && line > bar && first > line, $"bar {bar}, line {line}, grid {first}");
    }

    /// <summary>The library's own themes keep the grid under everything, as before.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.Library), MemberType = typeof(ThemeCases))]
    public void UnderALibraryTheme_TheGridIsDrawnFirst(string name)
    {
        var ctx = Draw(name.AsTheme(), ax => BarThenLine(ax).AxHSpan(0.5, 1.0, s => { s.Color = SpanInk; s.Alpha = 1; }));

        int last = LastGrid(ctx);
        Assert.True(last >= 0 && Span(ctx) > last && Bar(ctx) > last && Line(ctx) > last, $"grid ..{last}, span {Span(ctx)}, bar {Bar(ctx)}");
    }

    /// <summary>Under the default style a span and a patch (1) stand under the grid and a reference line (<c>axhline</c>, 2) over
    /// it, and the reference line stands over a bar.</summary>
    [Fact]
    public void UnderV2_ASpanAndAPatchStandUnderTheGrid_AndAReferenceLineOverItAndOverABar()
    {
        var ctx = Draw(Theme.MatplotlibV2, ax => ax
            .Bar(["a", "b"], [1.0, 2.0], b => b.Color = BarInk)
            .AxHSpan(0.2, 0.4, s => { s.Color = SpanInk; s.Alpha = 1; })
            .AxHLine(1.5, r => r.Color = RuleInk)
            .AddPatch(new Rectangle(new(0, 0), 0.5, 0.5) { FaceColor = PatchInk }));

        int span = Span(ctx), patch = PatchAt(ctx), first = FirstGrid(ctx), last = LastGrid(ctx), rule = Rule(ctx), bar = Bar(ctx);
        Assert.True(span >= 0 && patch > span && first > patch && first > bar && rule > last,
            $"span {span}, patch {patch}, bar {bar}, grid {first}..{last}, rule {rule}");
    }

    /// <summary>Where nothing stands under the grid, the grid is drawn where it always was, before the tick labels, so a picture of
    /// lines on a grid writes the same elements in the same order.</summary>
    [Fact]
    public void UnderV2_AGridThatNothingStandsUnder_IsDrawnBeforeTheTickLabels()
    {
        var ctx = Draw(Theme.MatplotlibV2, ax => ax.Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => l.Color = LineInk));

        int last = LastGrid(ctx), label = ctx.Calls.FindIndex(c => c is DrawTextCall);
        Assert.True(last >= 0 && label > last, $"grid ..{last}, first tick label {label}");
    }

    // ---- the series among themselves ---------------------------------------------------------------------------------------

    /// <summary>Under the matplotlib themes a bar added after a line is still drawn under it (1 against 2), as matplotlib's
    /// <c>Axes.draw</c> sorts its artists by zorder; under the library's own themes the order they were added in holds.</summary>
    [Fact]
    public void ABarAddedAfterALine_IsDrawnUnderItUnderV2_AndOverItUnderTheLibraryTheme()
    {
        Func<AxesBuilder, AxesBuilder> lineThenBar = ax => ax
            .Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => l.Color = LineInk)
            .Bar(["a", "b", "c"], [1.0, 3.0, 2.0], b => b.Color = BarInk);

        var v2 = Draw(Theme.MatplotlibV2, lineThenBar);
        var library = Draw(Theme.Default, lineThenBar);

        Assert.True(Bar(v2) < Line(v2), $"v2: bar {Bar(v2)}, line {Line(v2)}");
        Assert.True(Bar(library) > Line(library), $"library: bar {Bar(library)}, line {Line(library)}");
    }

    /// <summary>A z-order the caller gave wins over the one the kind of series has, even when it is the value a series rests at:
    /// a line given 0 is drawn under a bar (1) and under the grid under the default style.</summary>
    [Fact]
    public void UnderV2_ALineGivenZOrder0_IsDrawnUnderABarAndUnderTheGrid()
    {
        var ctx = Draw(Theme.MatplotlibV2, ax => ax
            .Bar(["a", "b", "c"], [1.0, 3.0, 2.0], b => b.Color = BarInk)
            .Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => { l.Color = LineInk; l.ZOrder = 0; }));

        Assert.True(Line(ctx) >= 0 && Line(ctx) < Bar(ctx) && Line(ctx) < FirstGrid(ctx), $"line {Line(ctx)}, bar {Bar(ctx)}, grid {FirstGrid(ctx)}");
    }

    /// <summary>A line given a higher z-order than the scatter beside it, both on a second axis the caller adds to a line, is drawn after it.</summary>
    private static void AssertTheLineIsDrawnAfterTheDots(string theme, Func<AxesBuilder, AxesBuilder> addTheSecondAxis)
    {
        string svg = Plt.Create().WithTheme(theme.AsTheme()).AddSubPlot(1, 1, 1, ax => addTheSecondAxis(ax.Plot([0.0, 1.0], [0.0, 1.0]))).ToSvg();

        int line = svg.IndexOf(LineInk.ToHex(), StringComparison.OrdinalIgnoreCase);
        int dots = svg.IndexOf(BarInk.ToHex(), StringComparison.OrdinalIgnoreCase);
        Assert.True(dots >= 0 && line > dots, $"dots {dots}, line {line}");
    }

    /// <summary>The series of a second Y axis stand in the same z-order among themselves (a twin axes in matplotlib sorts its own
    /// artists by zorder): a line given 5 and added first is drawn after a scatter added after it.</summary>
    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("default")]
    public void OnASecondYAxis_ASeriesGivenAHigherZOrder_IsDrawnAfterTheOthers(string theme) =>
        AssertTheLineIsDrawnAfterTheDots(theme, ax => ax
            .WithAddRightYAxis(s => s
                .Plot([0.0, 1.0], [5.0, 6.0], l => { l.Color = LineInk; l.ZOrder = 5; })
                .Scatter([0.0, 1.0], [5.5, 5.5], m => m.Color = BarInk)));

    /// <summary>The series of the top X axis stand in the same z-order among themselves.</summary>
    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("default")]
    public void OnTheTopXAxis_ASeriesGivenAHigherZOrder_IsDrawnAfterTheOthers(string theme) =>
        AssertTheLineIsDrawnAfterTheDots(theme, ax => ax
            .WithSecondaryXAxis(s => s
                .PlotXSecondary([10.0, 20.0], [0.2, 0.8], l => { l.Color = LineInk; l.ZOrder = 5; })
                .ScatterXSecondary([10.0, 20.0], [0.5, 0.5], m => m.Color = BarInk)));

    // ---- a patch's own z-order ----------------------------------------------------------------------------------------------

    /// <summary>A patch given a z-order of 3 is drawn over a line, under every theme.</summary>
    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    [InlineData("default")]
    public void APatchWithZOrder3_IsDrawnOverALine(string theme)
    {
        var ctx = Draw(theme.AsTheme(), ax => ax
            .Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => l.Color = LineInk)
            .AddPatch(new Rectangle(new(0, 1), 1, 1) { FaceColor = PatchInk, ZOrder = 3 }));

        Assert.True(Line(ctx) >= 0 && PatchAt(ctx) > Line(ctx), $"line {Line(ctx)}, patch {PatchAt(ctx)}");
    }

    /// <summary>A patch given no z-order stands in the patches' place: under the series under the library's own themes, as
    /// before.</summary>
    [Fact]
    public void APatchGivenNoZOrder_IsDrawnUnderTheSeries_UnderTheLibraryTheme()
    {
        var patch = new Rectangle(new(0, 1), 1, 1) { FaceColor = PatchInk };
        var ctx = Draw(Theme.Default, ax => ax
            .Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => l.Color = LineInk)
            .AddPatch(patch));

        Assert.True(PatchAt(ctx) >= 0 && PatchAt(ctx) < Line(ctx), $"patch {PatchAt(ctx)}, line {Line(ctx)}");
    }

    /// <summary>The patches drawn one after another share one clip; a patch lifted over a series is clipped on its own, after it.</summary>
    [Fact]
    public void UnderASetLimit_APatchLiftedOverTheSeries_IsClippedOnItsOwnAfterThem()
    {
        var ctx = Draw(Theme.Default, ax => ax
            .SetXLim(0, 2)
            .Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => l.Color = LineInk)
            .AddPatch(new Rectangle(new(0, 1), 1, 1) { FaceColor = PatchInk })
            .AddPatch(new Rectangle(new(1, 1), 5, 5) { FaceColor = Colors.Red })
            .AddPatch(new Rectangle(new(0, 2), 5, 1) { FaceColor = Colors.Blue, ZOrder = 3 }));

        var kinds = ctx.Calls.Select(c => c.Kind).Where(k => k is "PushClip" or "PopClip" or "DrawPath").ToList();
        int line = Line(ctx), lifted = First<DrawPathCall>(ctx, a => Equals(a.Fill, Colors.Blue));
        Assert.Equal(["PushClip", "DrawPath", "DrawPath", "PopClip"], kinds.Take(4));
        Assert.True(lifted > line && ctx.Calls[lifted - 1] is PushClipCall && ctx.Calls[lifted + 1] is PopClipCall,
            $"line {line}, lifted patch {lifted}");
    }

    // ---- the theme value and the JSON -------------------------------------------------------------------------------------

    /// <summary>The axis' z-order is matplotlib's under its two styles and none under the library's own themes, and it survives the
    /// JSON and a theme made from it.</summary>
    [Fact]
    public void TheAxisZOrderOfATheme_IsMatplotlibsAxisbelow_AndSurvivesJsonAndAThemeMadeFromIt()
    {
        Assert.Equal(1.5, Theme.MatplotlibV2.Looks.Frame.AxisZOrder);
        Assert.Equal(2.5, Theme.MatplotlibClassic.Looks.Frame.AxisZOrder);
        Assert.Null(Theme.Default.Looks.Frame.AxisZOrder);
        Assert.Null(Theme.Dark.Looks.Frame.AxisZOrder);
        Assert.Equal(1.5, Theme.MatplotlibV2.Cloned().Looks.Frame.AxisZOrder);
        Assert.Equal(2.5, Theme.MatplotlibClassic.ThroughJson().Looks.Frame.AxisZOrder);
        Assert.Null(Theme.Dark.ThroughJson().Looks.Frame.AxisZOrder);
    }

    /// <summary>A z-order given at the value a series rests at is written, so it reads back as given and draws the same.</summary>
    [Fact]
    public void AZOrderGivenAtTheRestingValue_IsWritten_AndReadsBackAsGiven()
    {
        var serializer = new ChartSerializer();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Bar(["a", "b", "c"], [1.0, 3.0, 2.0], b => b.Color = BarInk)
            .Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => { l.Color = LineInk; l.ZOrder = 0; })).Build();

        string json = serializer.ToJson(figure);
        var line = (ChartSeries)serializer.FromJson(json).SubPlots[0].Series[1];

        Assert.Contains("\"zOrder\":0", json.Replace(" ", ""), StringComparison.Ordinal);
        Assert.Equal(0, line.GivenZOrder);
        Assert.Equal(figure.ToSvg(), serializer.FromJson(json).ToSvg());
    }

    /// <summary>A series given no z-order writes none, an area (which rests below the zero of a line) included.</summary>
    [Fact]
    public void ASeriesGivenNoZOrder_WritesNone()
    {
        var area = new AreaSeries([0.0, 1.0], [1.0, 2.0]);
        var line = new LineSeries([0.0, 1.0], [1.0, 2.0]);

        Assert.Null(area.GivenZOrder);
        Assert.Null(line.GivenZOrder);
        Assert.Null(area.ToDtoWithSharedFields().ZOrder);
        Assert.Null(line.ToDtoWithSharedFields().ZOrder);
    }
}
