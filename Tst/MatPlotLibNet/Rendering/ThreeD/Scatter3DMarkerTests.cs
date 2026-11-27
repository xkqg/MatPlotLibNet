// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering.Svg.Interaction;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>The marker a 3-D scatter names (<see cref="Scatter3DSeries.MarkerStyle"/>) is the marker it draws, in the plot and in its
/// legend entry, through the same shapes a 2-D scatter draws (<c>MarkerShape</c>), so a square, a triangle or a cross is one shape
/// wherever a marker stands. It was always a circle whatever the property held. matplotlib 3.11.2, read with <c>inspect</c>:
/// <c>Axes3D.scatter</c> takes <c>marker</c> as <c>Axes.scatter</c> does and draws a <c>Path3DCollection</c> of that marker, sized by
/// <c>s</c> in points squared; <c>MarkerStyle('^')</c> fills a 1 x 1 box and <c>'s'</c> a unit square, so a marker of size s is sqrt(s)
/// points across whatever its shape.</summary>
public class Scatter3DMarkerTests
{
    private const double SixPointsInPixels = 6 * 100.0 / 72.0;

    private static XDocument Svg(MarkerStyle marker, bool legend = false, Theme? theme = null, bool interactive = false)
    {
        var builder = Plt.Create().WithSize(400, 300).WithTheme(theme ?? Theme.Default);
        if (interactive) builder = builder.WithBrowserInteraction();
        return builder.AddSubPlot(1, 1, 1, ax =>
        {
            var scatter = ax.Scatter3D([1.0, 2.0], [1.0, 2.0], [1.0, 2.0], s => { s.Color = Colors.Red; s.MarkerStyle = marker; if (legend) s.Label = "q"; });
            // A label alone lists the series: an axes starts with a visible legend (LegendAutoShowTests). The call only says it again.
            if (legend) scatter.WithLegend();
        }).Build().ToSvg().Xml();
    }

    private static List<XElement> Drawn(XDocument svg, string tag, string colourAttribute = "fill") =>
        [.. svg.Each(tag).Where(e => (string?)e.Attribute(colourAttribute) == Colors.Red.ToHex())];

    private static (double Width, double Height) Extent(XElement polygon)
    {
        var points = ((string)polygon.Attribute("points")!).Split(' ').Select(p => p.Split(','))
            .Select(c => (X: double.Parse(c[0], CultureInfo.InvariantCulture), Y: double.Parse(c[1], CultureInfo.InvariantCulture))).ToList();
        return (points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y));
    }

    [Fact]
    public void TheDefaultMarker_IsACircle()
    {
        var svg = Svg(MarkerStyle.Circle);

        Assert.Equal(2, Drawn(svg, "circle").Count);
        Assert.Empty(Drawn(svg, "rect"));
    }

    [Fact]
    public void ASquare_IsDrawnAsASquare_NotACircle()
    {
        var svg = Svg(MarkerStyle.Square);

        var squares = Drawn(svg, "rect");
        Assert.Empty(Drawn(svg, "circle"));
        Assert.Equal(2, squares.Count);
        Assert.All(squares, square => Assert.Equal(square.Number("width"), square.Number("height"), 1));
        // The library's own themes shrink a marker with its distance, to half at the farthest: the nearest is the full size across.
        Assert.Equal(SixPointsInPixels, squares.Max(square => square.Number("width")), 1);
        Assert.Equal(SixPointsInPixels / 2, squares.Min(square => square.Number("width")), 1);
    }

    [Theory]
    [InlineData(MarkerStyle.Triangle)]
    [InlineData(MarkerStyle.TriangleDown)]
    [InlineData(MarkerStyle.TriangleLeft)]
    [InlineData(MarkerStyle.TriangleRight)]
    [InlineData(MarkerStyle.Diamond)]
    [InlineData(MarkerStyle.Pentagon)]
    [InlineData(MarkerStyle.Hexagon)]
    [InlineData(MarkerStyle.Star)]
    public void APolygonMarker_IsDrawnAsItsPolygon_NotACircle(MarkerStyle marker)
    {
        var svg = Svg(marker);

        Assert.Empty(Drawn(svg, "circle"));
        Assert.Equal(2, Drawn(svg, "polygon").Count);
    }

    [Theory]
    [InlineData(MarkerStyle.Cross)]
    [InlineData(MarkerStyle.Plus)]
    public void AStrokedMarker_IsTwoLinesForEachPoint_NotACircle(MarkerStyle marker)
    {
        var svg = Svg(marker);

        Assert.Empty(Drawn(svg, "circle"));
        Assert.Equal(4, Drawn(svg, "line", "stroke").Count);
    }

    [Fact]
    public void NoMarker_DrawsNoPoint()
    {
        var svg = Svg(MarkerStyle.None);

        Assert.Empty(Drawn(svg, "circle"));
        Assert.Empty(Drawn(svg, "rect"));
        Assert.Empty(Drawn(svg, "polygon"));
        Assert.Empty(Drawn(svg, "line", "stroke"));
    }

    /// <summary>matplotlib's <c>'^'</c> fills its 1 x 1 box (the isosceles triangle (0, 1), (-1, -1), (1, -1) scaled by 0.5), so under a
    /// matplotlib theme a triangle is as wide and as high as the size says, as a 2-D scatter's is.</summary>
    [Fact]
    public void UnderAMatplotlibTheme_ATriangleFillsItsBox_AsAnyOtherMarkerOfThatShapeDoes()
    {
        var svg = Svg(MarkerStyle.Triangle, theme: Theme.MatplotlibV2);

        var triangles = Drawn(svg, "polygon");
        Assert.Equal(2, triangles.Count);
        // A 3-D scatter under a matplotlib theme draws every marker one size, sqrt(20) points across.
        Assert.All(triangles, triangle =>
        {
            var (width, height) = Extent(triangle);
            Assert.Equal(Math.Sqrt(20) * 100.0 / 72.0, width, 1);
            Assert.Equal(width, height, 1);
        });
    }

    // ── the legend entry ─────────────────────────────────────────────────────

    [Fact]
    public void ALegendHandle_IsTheMarker_NotACircle()
    {
        var svg = Svg(MarkerStyle.Square, legend: true);

        // Two points and the handle, each a square at most the full marker size across (a patch handle is longer than it is high).
        var squares = Drawn(svg, "rect");
        Assert.Empty(Drawn(svg, "circle"));
        Assert.Equal(3, squares.Count);
        Assert.All(squares, square =>
        {
            Assert.Equal(square.Number("width"), square.Number("height"), 1);
            Assert.True(square.Number("width") <= SixPointsInPixels + 0.06);
        });
    }

    [Fact]
    public void ALegendHandleOfTheDefaultMarker_IsStillACircle()
    {
        var svg = Svg(MarkerStyle.Circle, legend: true);

        Assert.Equal(3, Drawn(svg, "circle").Count);
    }

    [Fact]
    public void TheLegendSwatch_SaysTheMarkerOfTheSeries()
    {
        var swatch = new Scatter3DSeries(new double[] { 1.0 }, new double[] { 1.0 }, new double[] { 1.0 }) { MarkerStyle = MarkerStyle.Diamond }.LegendSwatch;

        Assert.Equal(LegendSwatchKind.Scatter3D, swatch.Kind);
        Assert.Equal(MarkerStyle.Diamond, swatch.Marker);
    }

    // ── the scene a script rotates ───────────────────────────────────────────

    [Fact]
    public void InAnInteractiveScene_ACircleStaysABareElementTheScriptMoves()
    {
        var svg = Svg(MarkerStyle.Circle, interactive: true);

        Assert.All(Drawn(svg, "circle"), circle => Assert.NotNull(circle.Attribute("data-v3d")));
        Assert.DoesNotContain(svg.Each("g"), e => (string?)e.Attribute("class") == "mpl-3d-marker");
    }

    /// <summary>A marker that is not a circle is a group the script moves as a whole: the group names where the marker stands in the
    /// scene (<c>data-v3d</c>) and where the server drew it (<c>data-c</c>), its shape stays as drawn.</summary>
    [Theory]
    [InlineData(MarkerStyle.Square)]
    [InlineData(MarkerStyle.Triangle)]
    [InlineData(MarkerStyle.Cross)]
    public void InAnInteractiveScene_AMarkerThatIsNotACircle_StandsInAGroupTheScriptMoves(MarkerStyle marker)
    {
        var svg = Svg(marker, interactive: true);

        var groups = svg.Each("g").Where(e => (string?)e.Attribute("class") == "mpl-3d-marker").ToList();
        Assert.Equal(2, groups.Count);
        Assert.All(groups, group =>
        {
            Assert.NotNull(group.Attribute("data-v3d"));
            Assert.NotNull(group.Attribute("data-c"));
            Assert.NotEmpty(group.Elements());
        });
    }

    [Fact]
    public void DraggingTheScene_MovesAMarkerGroupAsAWhole()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60, distance: 8)
                .Scatter3D([1.0, 2.0, 3.0], [1.0, 3.0, 2.0], [1.0, 3.0, 2.0], s => { s.Color = Colors.Red; s.MarkerStyle = MarkerStyle.Square; })));
        var before = h.GetAttribute("g.mpl-3d-marker", "transform");

        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 100; e.clientY = 100; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 200; e.clientY = 100; });
        h.Simulate(".mpl-3d-scene", "pointerup", e => { e.clientX = 200; e.clientY = 100; });

        var after = h.GetAttribute("g.mpl-3d-marker", "transform");
        Assert.Null(before);
        Assert.NotNull(after);
        Assert.StartsWith("translate(", after);
    }
}
