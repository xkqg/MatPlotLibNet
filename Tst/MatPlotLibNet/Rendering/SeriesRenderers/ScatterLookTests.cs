// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>What the matplotlib themes draw a scatter marker with where its author names nothing, read with <c>inspect</c> from
/// matplotlib 3.11.2. <c>Axes.scatter</c>: <c>s = (20 if mpl.rcParams['_internal.classic_mode'] else rcParams['lines.markersize']
/// ** 2.0)</c>, and <c>_parse_scatter_color_args</c>: <c>edgecolors = rcParams['scatter.edgecolors']</c> ('face') unless classic,
/// <c>c = "b" if rcParams['_internal.classic_mode'] else get_next_color_func()</c> (so a classic scatter takes a colour of the
/// cycle for nothing). A filled marker takes its collection's default width, <c>patch.linewidth</c> (1 pt in both styles); in
/// classic mode the edge colour is <c>patch.edgecolor</c> (black), forced by <c>patch.force_edgecolor</c>. Measured:
/// <c>get_sizes()</c> [20.] / [36.], <c>get_edgecolor()</c> black / the face colour, <c>get_linewidth()</c> [1.], <c>get_facecolor()</c>
/// blue (0, 0, 1) / tab:blue; the 3-D scatter's <c>s</c> is 20 in both styles (<c>Axes3D.scatter(…, s=20, …)</c>) and it calls
/// <c>Axes.scatter</c>. A marker with this stroke is its size and a point wide across. The library's own themes keep their
/// markers plain.</summary>
public class ScatterLookTests
{
    private const double Pt = 100.0 / 72.0;

    private static XDocument Svg(Theme theme, Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(400, 300).WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg().Xml();

    private static List<XElement> Circles(XDocument svg, Color fill) =>
        [.. svg.Each("circle").Where(e => (string?)e.Attribute("fill") == fill.ToHex())];

    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    // ── the default style: edge in the face colour, 1 pt ─────────────────────────────────────────────────────────

    [Fact]
    public void V2_AScatterMarker_IsStrokedOnePointInItsFaceColour()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.AddSeries(new ScatterSeries(X, Y)));

        var circles = Circles(svg, Theme.MatplotlibV2.CycleColors[0]);
        Assert.Equal(3, circles.Count);
        Assert.All(circles, c =>
        {
            Assert.Equal(Theme.MatplotlibV2.CycleColors[0].ToHex(), (string?)c.Attribute("stroke"));
            Assert.Equal(Pt, c.Number("stroke-width"), 1);
            Assert.Equal(6 * Pt / 2, c.Number("r"), 1);   // 36 points squared: the path is 6 points across, the stroke adds a point
        });
    }

    [Fact]
    public void V2_AMarkerWithAnAlpha_IsStrokedInTheSameTranslucentColour()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.AddSeries(new ScatterSeries(X, Y) { Alpha = 0.5 }));

        var circle = Circles(svg, Theme.MatplotlibV2.CycleColors[0]).First();
        Assert.Equal(0.5, circle.Number("fill-opacity"), 1);
        Assert.Equal(0.5, circle.Number("stroke-opacity"), 1);
    }

    [Fact]
    public void V2_ASquareMarker_IsStrokedToo()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.AddSeries(new ScatterSeries(X, Y) { Marker = MarkerStyle.Square }));

        var squares = svg.Each("rect").Where(e => (string?)e.Attribute("fill") == Theme.MatplotlibV2.CycleColors[0].ToHex()).ToList();
        Assert.Equal(3, squares.Count);
        Assert.All(squares, s => Assert.Equal(Theme.MatplotlibV2.CycleColors[0].ToHex(), (string?)s.Attribute("stroke")));
    }

    [Fact]
    public void TheLibrarysOwnThemes_DrawAScatterMarkerWithNoStroke()
    {
        var svg = Svg(Theme.Default, ax => ax.AddSeries(new ScatterSeries(X, Y)));

        var circles = Circles(svg, Theme.Default.CycleColors[0]);
        Assert.Equal(3, circles.Count);
        Assert.All(circles, c => Assert.True(c.Attribute("stroke") is null || (string?)c.Attribute("stroke") == "none"));
    }

    [Fact]
    public void AnEdgeTheSeriesNames_WinsOverTheThemes()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.AddSeries(new ScatterSeries(X, Y)
            { EdgeColors = [Colors.Red, Colors.Red, Colors.Red], LineWidths = [3.0, 3.0, 3.0] }));

        var circles = Circles(svg, Theme.MatplotlibV2.CycleColors[0]);
        Assert.All(circles, c =>
        {
            Assert.Equal(Colors.Red.ToHex(), (string?)c.Attribute("stroke"));
            Assert.Equal(3.0, c.Number("stroke-width"), 1);
        });
    }

    // ── the classic style: blue, 20 points squared, black edge ───────────────────────────────────────────────────

    [Fact]
    public void Classic_AScatterMarker_IsBlue_TwentyPointsSquared_AndEdgedBlack()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y)));

        var circles = Circles(svg, Colors.Blue);
        Assert.Equal(3, circles.Count);
        Assert.All(circles, c =>
        {
            Assert.Equal(Colors.Black.ToHex(), (string?)c.Attribute("stroke"));
            Assert.Equal(Pt, c.Number("stroke-width"), 1);
            Assert.Equal(Math.Sqrt(20) * Pt / 2, c.Number("r"), 1);
        });
    }

    [Fact]
    public void Classic_ASizeTheSeriesNames_IsDrawnAsNamed()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y) { MarkerSize = 50 }));

        Assert.All(Circles(svg, Colors.Blue), c => Assert.Equal(Math.Sqrt(50) * Pt / 2, c.Number("r"), 1));
    }

    [Fact]
    public void Classic_PerPointSizes_AreDrawnAsNamed_EvenWhenTheyEqualTheRestingSize()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y) { Sizes = [36, 36, 36] }));

        Assert.All(Circles(svg, Colors.Blue), c => Assert.Equal(6 * Pt / 2, c.Number("r"), 1));
    }

    [Fact]
    public void Classic_AColourTheSeriesNames_IsKept_AndTheEdgeIsBlack()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y) { Color = Colors.Red }));

        var circles = Circles(svg, Colors.Red);
        Assert.Equal(3, circles.Count);
        Assert.All(circles, c => Assert.Equal(Colors.Black.ToHex(), (string?)c.Attribute("stroke")));
    }

    [Fact]
    public void Classic_AScatterTakesNoColourOfTheCycle_SoALineAfterItIsTheFirst()
    {
        // Axes.scatter takes "b" and never asks the cycle: the next line is the first colour of the cycle (blue), not the second.
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y)).AddSeries(new LineSeries(X, Y)));

        var line = svg.Each("polyline").First(e => (string?)e.Attribute("stroke") is not null
            && (string?)e.Attribute("fill") is "none");
        Assert.Equal(Theme.MatplotlibClassic.CycleColors[0].ToHex(), (string?)line.Attribute("stroke"));
    }

    [Fact]
    public void V2_AScatterTakesThePatchCycle_SoALineAfterItIsTheFirstColour()
    {
        // matplotlib 3.11.2: ax.scatter(...) then ax.plot(...) draws the line '#1f77b4' (scatter asks _get_patches_for_fill).
        var svg = Svg(Theme.MatplotlibV2, ax => ax.AddSeries(new ScatterSeries(X, Y)).AddSeries(new LineSeries(X, Y)));

        var line = svg.Each("polyline").First(e => (string?)e.Attribute("fill") is "none"
            && (string?)e.Attribute("stroke") is not null);
        Assert.Equal(Theme.MatplotlibV2.CycleColors[0].ToHex(), (string?)line.Attribute("stroke"));
    }

    [Fact]
    public void Classic_TheLegendHandle_ShowsTheMarkerThePlotDraws()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y) { Label = "s" }).WithLegend());

        var circles = Circles(svg, Colors.Blue);
        Assert.Equal(6, circles.Count);   // three points and the handle, which shows three markers under classic (legend.scatterpoints 3)
        Assert.All(circles, c =>
        {
            Assert.Equal(Colors.Black.ToHex(), (string?)c.Attribute("stroke"));
            Assert.Equal(Math.Sqrt(20) * Pt / 2, c.Number("r"), 1);
        });
    }

    // ── the other scatters: polar, ternary, 3-D ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Classic_APolarScatterMarker_IsBlue_TwentyPointsSquared_AndEdgedBlack()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.PolarScatter([0.5, 1.0], [0.0, 1.0]));

        var circles = Circles(svg, Colors.Blue);
        Assert.Equal(2, circles.Count);
        Assert.All(circles, c =>
        {
            Assert.Equal(Colors.Black.ToHex(), (string?)c.Attribute("stroke"));
            Assert.Equal(Math.Sqrt(20) * Pt / 2, c.Number("r"), 1);
        });
    }

    [Fact]
    public void V2_APolarScatterMarker_IsSixPointsAcross_StrokedInItsFaceColour()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.PolarScatter([0.5, 1.0], [0.0, 1.0]));

        var circles = Circles(svg, Theme.MatplotlibV2.CycleColors[0]);
        Assert.Equal(2, circles.Count);
        Assert.All(circles, c =>
        {
            Assert.Equal(Theme.MatplotlibV2.CycleColors[0].ToHex(), (string?)c.Attribute("stroke"));
            Assert.Equal(6 * Pt / 2, c.Number("r"), 1);
        });
    }

    [Fact]
    public void Classic_ATernaryScatterMarker_IsBlue_TwentyPointsSquared_AndEdgedBlack()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.TernaryScatter([0.2, 0.5], [0.5, 0.3], [0.3, 0.2]));

        var circles = Circles(svg, Colors.Blue);
        Assert.Equal(2, circles.Count);
        Assert.All(circles, c =>
        {
            Assert.Equal(Colors.Black.ToHex(), (string?)c.Attribute("stroke"));
            Assert.Equal(Math.Sqrt(20) * Pt / 2, c.Number("r"), 1);
        });
    }

    [Fact]
    public void AThreeDScatter_IsTwentyPointsSquaredInEveryMatplotlibStyle_StrokedLikeAScatter()
    {
        // Axes3D.scatter(xs, ys, zs, zdir='z', s=20, ...): the size is its own default, in the default style too, and every marker is
        // that size (Scatter3DDepthTests holds the depth the fade shows).
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Scatter3D(X, Y, [1.0, 3.0, 2.0]));

        var circles = Circles(svg, Theme.MatplotlibV2.CycleColors[0]);
        Assert.Equal(3, circles.Count);
        Assert.All(circles, c => Assert.Equal(Math.Sqrt(20) * Pt / 2, c.Number("r"), 1));
        Assert.All(circles, c => Assert.Equal(Theme.MatplotlibV2.CycleColors[0].ToHex(), (string?)c.Attribute("stroke")));
    }

    [Fact]
    public void Classic_AThreeDScatter_IsBlueAndEdgedBlack()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.Scatter3D(X, Y, [1.0, 3.0, 2.0]));

        var circles = Circles(svg, Colors.Blue);
        Assert.Equal(3, circles.Count);
        Assert.All(circles, c => Assert.Equal(Colors.Black.ToHex(), (string?)c.Attribute("stroke")));
    }

    // ── the theme carries it ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheThemeCarriesTheScatterLook_AndAClone_AndTheJson_KeepIt()
    {
        var classic = Theme.MatplotlibClassic.Looks.Scatter!.Value;
        Assert.Equal(20, classic.Area);
        Assert.Equal(20, classic.Area3D);
        Assert.Equal(Colors.Blue, classic.Face);
        Assert.Equal(1.0 * Pt, classic.EdgeWidth!.Value, 9);
        Assert.Equal(Colors.Black, classic.EdgeColor);

        var v2 = Theme.MatplotlibV2.Looks.Scatter!.Value;
        Assert.Null(v2.Area);
        Assert.Equal(20, v2.Area3D);
        Assert.Null(v2.Face);
        Assert.Equal(1.0 * Pt, v2.EdgeWidth!.Value, 9);
        Assert.Null(v2.EdgeColor);

        Assert.Null(Theme.Default.Looks.Scatter);
        Assert.Equal(classic, Theme.MatplotlibClassic.Cloned().Looks.Scatter);
        Assert.Equal(classic, Theme.MatplotlibClassic.ThroughDto().Looks.Scatter);
        Assert.Equal(v2, Theme.MatplotlibV2.ThroughDto().Looks.Scatter);
        Assert.Null(Theme.Default.ThroughDto().Looks.Scatter);
    }

    // ── the slot is the one owner: the plot and the legend ask it ────────────────────────────────────────────────

    [Fact]
    public void TheSlot_GivesTheThemesSize_ToAScatterGivenNone_AndOnlyWhereTheThemeNamesOne()
    {
        var classic = Theme.MatplotlibClassic.SeriesSlot(0);
        var v2 = Theme.MatplotlibV2.SeriesSlot(0);
        var plain = Theme.Default.SeriesSlot(0);

        Assert.Equal(Math.Sqrt(20), classic.ScatterSizeOf(null), 9);
        Assert.Equal(7, classic.ScatterSizeOf(7));
        Assert.Equal(SeriesSlot.RestingScatterSize, classic.ScatterSizeOf(SeriesSlot.RestingScatterSize));
        Assert.Equal(SeriesSlot.RestingScatterSize, v2.ScatterSizeOf(null));
        Assert.Equal(Math.Sqrt(20), v2.Scatter3DSizeOf(null), 9);
        Assert.Equal(7, v2.Scatter3DSizeOf(7));
        Assert.Equal(SeriesSlot.RestingScatterSize, v2.Scatter3DSizeOf(SeriesSlot.RestingScatterSize));
        Assert.Equal(SeriesSlot.RestingScatterSize, plain.ScatterSizeOf(null));
        Assert.Equal(SeriesSlot.RestingScatterSize, plain.Scatter3DSizeOf(null));
    }

    [Fact]
    public void TheSlot_TheRestingSizeIsTheOneEveryScatterHolds()
    {
        Assert.Equal(SeriesSlot.RestingScatterSize, new ScatterSeries(X, Y).MarkerSize.AreaAcrossInPoints);
        Assert.Equal(SeriesSlot.RestingScatterSize, new PolarScatterSeries(X, Y).MarkerSize);
        Assert.Equal(SeriesSlot.RestingScatterSize, new TernaryScatterSeries(X, Y, X).MarkerSize);
        Assert.Equal(SeriesSlot.RestingScatterSize, new Scatter3DSeries(X, Y, X).MarkerSize);
    }

    [Fact]
    public void TheSlot_GivesTheEdgeInTheFaceColourOrTheThemes_OrNone()
    {
        var face = Colors.Red;

        Assert.Equal(new MarkerEdge(face, Pt), Theme.MatplotlibV2.SeriesSlot(0).ScatterEdgeOf(face));
        Assert.Equal(new MarkerEdge(Colors.Black, Pt), Theme.MatplotlibClassic.SeriesSlot(0).ScatterEdgeOf(face));
        Assert.Null(Theme.Default.SeriesSlot(0).ScatterEdgeOf(face));
        Assert.Equal(new ShapeStyle(face, null, 0), Theme.Default.SeriesSlot(0).ScatterStyleOf(face));
        // A scatter is a PathCollection: its marker's edge is joined as the matplotlib themes join a collection, round.
        Assert.Equal(new ShapeStyle(face, Colors.Black, Pt) { StrokeJoin = JoinStyle.Round }, Theme.MatplotlibClassic.SeriesSlot(0).ScatterStyleOf(face));
    }

    [Fact]
    public void ABubbleIsFilledAtTheSeriesOpacity_AndEdgedByTheThemesScatterEdgeFadedToo_OrByItsOwnColourOnePointWide()
    {
        var color = Colors.Red;
        var fill = color.WithOpacity(0.5);

        // The library's own themes name no scatter edge: the bubble is edged in its own colour, unfaded, 1 pixel wide.
        Assert.Equal(new ShapeStyle(fill, color, 1), Theme.Default.SeriesSlot(0).BubbleStyleOf(color, 0.5));
        // A theme that names one strokes it, its colour and the face's faded to the series' opacity, at the theme's width.
        var v2 = Theme.MatplotlibV2.SeriesSlot(0);
        Assert.Equal(new ShapeStyle(fill, v2.ScatterEdgeOf(fill)!.Value.Color.WithOpacity(0.5), Pt), v2.BubbleStyleOf(color, 0.5));
        var classic = Theme.MatplotlibClassic.SeriesSlot(0);
        Assert.Equal(new ShapeStyle(fill, Colors.Black.WithOpacity(0.5), Pt), classic.BubbleStyleOf(color, 0.5));
    }

    [Fact]
    public void UnderClassic_AScatterTakesNoColourOfTheCycle_NamedOrNot_AndOneInAGroupStandsInTheGroupsSlot()
    {
        // matplotlib 3.11.2, classic mode: Axes.scatter takes 'b' (or the colour it is given) and never asks a cycle.
        var axes = new MatPlotLibNet.Models.Axes();
        axes.AddSeries(new ScatterSeries(X, Y) { Color = Colors.Red });
        axes.AddSeries(new ScatterSeries(X, Y));
        axes.AddSeries(new LineSeries(X, Y) { ColorGroup = 3 });
        axes.AddSeries(new ScatterSeries(X, Y) { ColorGroup = 3 });
        axes.AddSeries(new LineSeries(X, Y));

        Assert.Equal([0, 0, 0, 0, 1], axes.ColorSlots(Theme.MatplotlibClassic));
        Assert.Equal([0, 0, 0, 0, 1], axes.ColorSlots(Theme.MatplotlibV2));   // a named scatter asks nothing under v2 either
        Assert.Equal([0, 1, 2, 2, 3], axes.ColorSlots(Theme.Default));
    }
}
