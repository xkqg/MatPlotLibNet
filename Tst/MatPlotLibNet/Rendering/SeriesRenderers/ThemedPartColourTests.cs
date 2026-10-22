// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A stacked area, a donut, a funnel and a pie colour their own parts (a layer, a wedge, a stage): the part at
/// position <c>i</c> wears what the figure's theme hands out at slot <c>i</c>, as a series does. Before, the first three read
/// the default theme's cycle although they were handed the figure's theme, so a figure on <c>Theme.Ggplot</c> drew them in
/// matplotlib's blues and oranges; the pie read the theme's colour list but not its property cycler, and the axes wrote the
/// colours it picked into the series on its first render. The owner is <c>SeriesSlotExtensions</c> (<c>SeriesSlotTests</c>),
/// the one every series colour is asked from; a colour the series names itself still wins.</summary>
public class ThemedPartColourTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static readonly PropCycler RedGreenBlue =
        new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();

    private static readonly Theme CyclerTheme =
        Theme.CreateFrom(Theme.Default).WithPropCycler(RedGreenBlue).Build();

    /// <summary>The four charts, one part per count: a layer of a stacked area (alpha 1, so the fill is the bare colour),
    /// a wedge of a donut or a pie, a stage of a funnel. <paramref name="explicitColors"/> is what the series names itself.</summary>
    private static Color[] PartFills(string chart, Theme theme, int parts, Color[]? explicitColors = null)
    {
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue,
            new RenderArea(Bounds, ctx)) { Theme = theme };
        switch (chart)
        {
            case "stackedarea":
                new StackedAreaSeriesRenderer(context).Render(new StackedAreaSeries([0.0, 1, 2],
                    Enumerable.Range(0, parts).Select(_ => new[] { 1.0, 1, 1 }).ToArray()) { Alpha = 1 });
                return Fills(ctx, "DrawPolygon");
            case "donut":
                new DonutSeriesRenderer(context).Render(new DonutSeries(Enumerable.Repeat(1.0, parts).ToArray())
                    { Colors = explicitColors });
                return Fills(ctx, "DrawPath");
            case "pie":
                new PieSeriesRenderer(context).Render(new PieSeries(Enumerable.Repeat(1.0, parts).ToArray())
                    { Colors = explicitColors });
                return Fills(ctx, "DrawPath");
            case "funnel":
                new FunnelSeriesRenderer(context).Render(new FunnelSeries(
                    Enumerable.Range(0, parts).Select(i => $"stage {i}").ToArray(),
                    Enumerable.Range(0, parts).Select(i => 10.0 - i * 0.1).ToArray()) { Colors = explicitColors });
                return Fills(ctx, "DrawRectangle");
            default:
                throw new ArgumentOutOfRangeException(nameof(chart), chart, "Not one of the four.");
        }
    }

    private static Color[] Fills(RecordingRenderContext ctx, string kind) =>
        ctx.OfKind(kind).Select(c => (Color)c.Args!.GetType().GetProperty("fill")!.GetValue(c.Args)!).ToArray();

    // ── the premise: the figure's theme and the default theme hand out different colours ───────────────────

    [Fact]
    public void TheGgplotCycle_IsNotTheDefaultCycle()
    {
        Assert.NotEqual(Theme.Default.CycleColors[0], Theme.Ggplot.CycleColors[0]);
        Assert.NotEqual(Theme.Default.CycleColors[1], Theme.Ggplot.CycleColors[1]);
        Assert.NotEqual(Theme.Default.CycleColors[2], Theme.Ggplot.CycleColors[2]);
    }

    // ── a part wears the figure theme's colour at its own position ───────────────────────────────────────────

    [Theory]
    [InlineData("stackedarea")]
    [InlineData("donut")]
    [InlineData("pie")]
    [InlineData("funnel")]
    public void APart_WearsTheFigureThemesCycle(string chart)
    {
        var fills = PartFills(chart, Theme.Ggplot, 3);

        Assert.Equal(Theme.Ggplot.CycleColors.Take(3), fills);
    }

    [Theory]
    [InlineData("stackedarea")]
    [InlineData("donut")]
    [InlineData("pie")]
    [InlineData("funnel")]
    public void UnderTheDefaultTheme_APartKeepsTheDefaultCycle(string chart)
    {
        var fills = PartFills(chart, Theme.Default, 3);

        Assert.Equal(Theme.Default.CycleColors.Take(3), fills);
    }

    [Theory]
    [InlineData("stackedarea")]
    [InlineData("donut")]
    [InlineData("pie")]
    [InlineData("funnel")]
    public void APartPastTheEndOfTheCycle_StartsItOver(string chart)
    {
        int n = Theme.Ggplot.CycleColors.Length;

        var fills = PartFills(chart, Theme.Ggplot, n + 2);

        Assert.Equal(Theme.Ggplot.CycleColors[0], fills[n]);
        Assert.Equal(Theme.Ggplot.CycleColors[1], fills[n + 1]);
    }

    [Theory]
    [InlineData("stackedarea")]
    [InlineData("donut")]
    [InlineData("pie")]
    [InlineData("funnel")]
    public void ACyclerOnTheTheme_ColoursTheParts_AsItColoursSeries(string chart)
    {
        var fills = PartFills(chart, CyclerTheme, 3);

        Assert.Equal([Colors.Red, Colors.Green, Colors.Blue], fills);
    }

    // ── a colour the series names itself still wins ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("donut")]
    [InlineData("pie")]
    [InlineData("funnel")]
    public void AColourTheSeriesNamesWins_AndTheRestTakeTheirOwnPosition(string chart)
    {
        var fills = PartFills(chart, Theme.Ggplot, 3, [Colors.Red]);

        Assert.Equal([Colors.Red, Theme.Ggplot.CycleColors[1], Theme.Ggplot.CycleColors[2]], fills);
    }

    // ── the wiring: a figure hands its theme to the renderer ─────────────────────────────────────────────────

    private static string Svg(string chart, Theme theme) =>
        Plt.Create().WithTheme(theme).WithSize(400, 300).AddSubPlot(1, 1, 1, chart switch
        {
            "stackedarea" => ax => ax.StackPlot([0.0, 1, 2], [[1.0, 2, 1], [1.0, 1, 1], [2.0, 1, 2]]),
            "donut" => ax => ax.Donut([3.0, 2, 1]),
            "pie" => ax => ax.Pie([3.0, 2, 1]),
            "funnel" => ax => ax.Funnel(["a", "b", "c"], [3.0, 2, 1]),
            _ => throw new ArgumentOutOfRangeException(nameof(chart), chart, "Not one of the four."),
        }).ToSvg();

    [Theory]
    [InlineData("stackedarea")]
    [InlineData("donut")]
    [InlineData("pie")]
    [InlineData("funnel")]
    public void AFigureOnGgplot_DrawsTheChartInGgplotsColours(string chart)
    {
        string svg = Svg(chart, Theme.Ggplot);

        for (int i = 0; i < 3; i++)
        {
            Assert.Contains(Theme.Ggplot.CycleColors[i].ToHex(), svg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Theme.Default.CycleColors[i].ToHex(), svg, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("stackedarea")]
    [InlineData("donut")]
    [InlineData("pie")]
    [InlineData("funnel")]
    public void AFigureOnTheDefaultTheme_DrawsTheChartInTheDefaultColours(string chart)
    {
        string svg = Svg(chart, Theme.MatplotlibV2);

        for (int i = 0; i < 3; i++)
            Assert.Contains(Theme.Default.CycleColors[i].ToHex(), svg, StringComparison.OrdinalIgnoreCase);
    }
}
