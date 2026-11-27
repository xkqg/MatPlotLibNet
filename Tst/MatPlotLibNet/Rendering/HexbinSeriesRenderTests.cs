// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="HexbinSeries"/> rendering.</summary>
public class HexbinSeriesRenderTests
{
    private static readonly double[] X = Enumerable.Range(0, 30).Select(i => (double)(i % 6)).ToArray();
    private static readonly double[] Y = Enumerable.Range(0, 30).Select(i => (double)(i / 6)).ToArray();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // SVG contains polygon elements for hex cells.
        new DrawnToken("Hexbin_SvgContainsPolygons", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(X, Y))
            .ToSvg(), "<polygon"),
        // MinCount set higher than any single-bin count — all bins are filtered at the count < minCount check, but the normMin >= normMax
        // guard fires first to clamp the normalisation range. No polygons should be drawn.
        new DrawnToken("Hexbin_HighMinCount_NormalisationClampFires_NoPolygonsDrawn", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(
                [0.0, 1.0, 2.0, 3.0, 4.0],
                [0.0, 1.0, 2.0, 3.0, 4.0],
                s => s.MinCount = 100))
            .ToSvg()) { Absent = ["<polygon"] },
        new DrawnToken("Hexbin_MinCountZero_WithNoObservations_DrawsNothing", () => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Hexbin([], [], s => s.MinCount = 0)).ToSvg()) { Absent = ["<polygon"] },
        // ColorMap explicitly set — uses the provided colormap instead of falling back to Viridis. SVG must still
        // contain polygons.
        new DrawnToken("Hexbin_ExplicitColorMap_RendersPolygons", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(X, Y,
                s => s.ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma))
            .ToSvg(), "<polygon"),
        // Normalizer explicitly set — uses the provided normalizer instead of LinearNormalizer. SVG must still contain
        // polygons.
        new DrawnToken("Hexbin_ExplicitNormalizer_RendersPolygons", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(X, Y,
                s => s.Normalizer = MatPlotLibNet.Styling.ColorMaps.LinearNormalizer.Instance))
            .ToSvg(), "<polygon"),
        new DrawnToken("Hexbin_MinCountZero_DrawsTheFullestHexagonInTheHighestColour", () => Svg(0), $"fill=\"{MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis.GetColor(1).ToHex()}\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>JSON round-trip preserves type tag "hexbin".</summary>
    [Fact]
    public void Hexbin_JsonRoundTrip_PreservesType()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(X, Y))
            .Build();
        var json = new MatPlotLibNet.Serialization.ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"hexbin\"", json);

        var restored = new MatPlotLibNet.Serialization.ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<HexbinSeries>().FirstOrDefault();
        Assert.NotNull(series);
    }

    // ── matplotlib's hexagons ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The first hexagon of the picture: a polygon that has a fill and a stroke.</summary>
    private static SvgElement Hexagon(string svg)
    {
        var hexagon = svg.Elements("polygon").FirstOrDefault(polygon => polygon["points"] is not null && polygon["fill"]?.StartsWith('#') == true && polygon["stroke"]?.StartsWith('#') == true);
        Assert.True(hexagon.Attributes is not null, "a polygon with a fill and a stroke");
        return hexagon;
    }

    /// <summary>matplotlib 3.11.2, <c>Axes.hexbin</c>: the polygon is <c>[sx, sy / 3] * [[.5, -.5], [.5, .5], [0, 1], [-.5, .5], [-.5, -.5],
    /// [0, -1]]</c>, a hexagon with a vertex up and down and a vertical edge on each side; the hexagons of a row touch.</summary>
    [Fact]
    public void Hexbin_DrawsHexagonsWithAVertexUpAndDown_AndAVerticalEdgeEachSide()
    {
        var p = Hexagon(Svg(1)).Points();

        Assert.Equal(6, p.Length);
        Assert.Equal(p[0].X, p[1].X, 2);   // the right edge is vertical
        Assert.Equal(p[3].X, p[4].X, 2);   // the left edge is vertical
        Assert.Equal(p[2].X, p[5].X, 2);   // the top and bottom vertices stand over one another
        Assert.True(p[2].Y < p[1].Y && p[5].Y > p[0].Y, "a vertex up (smaller Y in the picture) and one down");
    }

    /// <summary>matplotlib draws a hexagon with <c>edgecolors='face'</c>: its outline in its own colour, so neighbours leave no seam.</summary>
    [Fact]
    public void Hexbin_StrokesEachHexagonInItsOwnColour()
    {
        var hexagon = Hexagon(Svg(1));

        Assert.Equal(hexagon["fill"], hexagon["stroke"], ignoreCase: true);
    }

    // ── MinCount 0: the grid is drawn whole, the empty hexagons too ─────────────────────────────────────────────

    /// <summary>Three observations at one corner of the data and one at the opposite corner: two hexagons hold something and the
    /// rest of the grid is empty.</summary>
    private static readonly double[] CornerX = [0.0, 0.1, 0.2, 10.0];
    private static readonly double[] CornerY = [0.0, 0.1, 0.2, 10.0];

    private static int Polygons(string svg) => svg.Elements("polygon").Count;

    private static string Svg(int minCount) => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Hexbin(CornerX, CornerY, s => { s.GridSize = 5; s.MinCount = minCount; }))
        .ToSvg();

    /// <summary>matplotlib's <c>hexbin</c> draws every hexagon of its grid when no <c>mincnt</c> is given, the empty ones in the
    /// colour of a count of 0; a <see cref="HexbinSeries.MinCount"/> of 0 says the same: a hexagon holding fewer than 0
    /// observations is none, so none is left blank. matplotlib 3.11.2, <c>Axes.hexbin</c>: with <c>mincnt=None</c> no
    /// accumulated count is set to NaN, so every hexagon of the grid is drawn.</summary>
    [Fact]
    public void Hexbin_MinCountZero_DrawsEveryHexagonOfTheGrid()
    {
        Assert.Equal(2, Polygons(Svg(1)));
        Assert.True(Polygons(Svg(0)) > 20, "the whole grid is drawn, " + Polygons(Svg(0)) + " hexagons");
    }

    [Fact]
    public void Hexbin_MinCountZero_DrawsAnEmptyHexagonInTheLowestColour()
    {
        string lowest = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis.GetColor(0).ToHex();
        string svg = Svg(0);

        Assert.True(svg.Elements("polygon").Count(polygon => polygon["fill"] == lowest) > 20,
            "the empty hexagons wear the colour of a count of 0, " + lowest);
    }
}
