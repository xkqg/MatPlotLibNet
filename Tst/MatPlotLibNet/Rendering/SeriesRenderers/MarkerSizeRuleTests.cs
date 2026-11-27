// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>One rule for the size of a marker: it is stated in points, drawn that many points across at 100 / 72 pixels to the
/// point, and a scatter states it as an area in points squared, so its square root across. matplotlib 3.11.2, read with
/// <c>inspect</c>: <c>Axes.scatter</c> "s: The marker size in points**2 ... Default is <c>rcParams['lines.markersize'] ** 2</c>"
/// (36 for the 6 pt default); <c>MarkerStyle._set_circle</c> scales the unit circle by 0.5 and <c>_set_square</c> shifts the unit
/// rectangle by -0.5, and a scatter scales the unit marker by sqrt(s), so a circle is sqrt(s) points across and a square sqrt(s)
/// points on a side; <c>Line2D</c> draws <c>markersize</c> (points) the same way; <c>HandlerPathCollection.get_sizes</c> gives a
/// legend handle the series' own sizes at <c>legend.markerscale = 1</c>. The library drew a scatter at a radius of sqrt(s / pi)
/// points (1.128 times matplotlib's circle) and a line's marker at its size read as pixels; the documentation of both says
/// points. The figures are drawn under the library's own theme, which leaves a marker as its series says; what the matplotlib themes
/// do besides is <see cref="ScatterLookTests"/>.</summary>
public class MarkerSizeRuleTests
{
    // The SVG writes a number with one decimal, so every comparison is to one decimal.
    private const double SixPointsInPixels = 6 * 100.0 / 72.0;

    private static XDocument Svg(Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(400, 300).WithTheme(Theme.Default).AddSubPlot(1, 1, 1, axes).Build().ToSvg().Xml();

    private static List<double> CircleRadii(XDocument svg, Color fill) =>
        [.. svg.Each("circle").Where(e => (string?)e.Attribute("fill") == fill.ToHex())
            .Select(e => e.Number("r"))];

    [Fact]
    public void AScatterMarker_AtTheRestingSize_IsSixPointsAcross()
    {
        var svg = Svg(ax => ax.AddSeries(new ScatterSeries([1.0, 2.0], [1.0, 2.0]) { Color = Colors.Red }));

        Assert.All(CircleRadii(svg, Colors.Red), r => Assert.Equal(SixPointsInPixels / 2, r, 1));
        Assert.Equal(2, CircleRadii(svg, Colors.Red).Count);
    }

    [Theory]
    [InlineData(16.0, 4.0)]
    [InlineData(100.0, 10.0)]
    public void AScatterMarker_IsTheSquareRootOfItsAreaAcross_InPoints(double area, double pointsAcross)
    {
        var svg = Svg(ax => ax.AddSeries(new ScatterSeries([1.0, 2.0], [1.0, 2.0]) { Color = Colors.Red, Sizes = [area, area] }));

        Assert.All(CircleRadii(svg, Colors.Red), r => Assert.Equal(pointsAcross * 100.0 / 72.0 / 2, r, 1));
    }

    [Fact]
    public void AScatterSquare_IsSixPointsOnASide()
    {
        var svg = Svg(ax => ax.AddSeries(new ScatterSeries([1.0, 2.0], [1.0, 2.0]) { Color = Colors.Red, Marker = MarkerStyle.Square }));

        var sides = svg.Each("rect").Where(e => (string?)e.Attribute("fill") == Colors.Red.ToHex())
            .Select(e => e.Number("width")).ToList();
        Assert.Equal(2, sides.Count);
        Assert.All(sides, w => Assert.Equal(SixPointsInPixels, w, 1));
    }

    [Fact]
    public void ALineMarker_AtTheRestingSize_IsSixPointsAcross_NotSixPixels()
    {
        var svg = Svg(ax => ax.AddSeries(new LineSeries([1.0, 2.0], [1.0, 2.0]) { Color = Colors.Red, Marker = MarkerStyle.Circle }));

        Assert.All(CircleRadii(svg, Colors.Red), r => Assert.Equal(SixPointsInPixels / 2, r, 1));
        Assert.Equal(2, CircleRadii(svg, Colors.Red).Count);
    }

    [Fact]
    public void AStepMarker_AtTheRestingSize_IsSixPointsAcross()
    {
        var svg = Svg(ax => ax.AddSeries(new StepSeries([1.0, 2.0], [1.0, 2.0]) { Color = Colors.Red, Marker = MarkerStyle.Circle }));

        Assert.NotEmpty(CircleRadii(svg, Colors.Red));
        Assert.All(CircleRadii(svg, Colors.Red), r => Assert.Equal(SixPointsInPixels / 2, r, 1));
    }

    [Fact]
    public void AScatterAndALineMarker_OfTheSameSize_AreDrawnTheSameSize()
    {
        // 36 points squared and 6 points are the same marker: one rule, one owner.
        var scatter = Svg(ax => ax.AddSeries(new ScatterSeries([1.0], [1.0]) { Color = Colors.Red, MarkerSize = 36 }));
        var line = Svg(ax => ax.AddSeries(new LineSeries([1.0], [1.0]) { Color = Colors.Red, Marker = MarkerStyle.Circle, MarkerSize = 6 }));

        Assert.Equal(CircleRadii(scatter, Colors.Red).Single(), CircleRadii(line, Colors.Red).Single(), 1);
    }

    [Fact]
    public void AStreamingScatterMarker_KeepsItsDocumentedDiameterInPixels()
    {
        // StreamingScatterSeries.MarkerSize is "marker diameter in pixels": it is drawn that wide, whatever a scatter's unit is.
        var streaming = new StreamingScatterSeries { Color = Colors.Red, MarkerSize = 10 };
        streaming.AppendPoint(1.0, 1.0);
        streaming.AppendPoint(2.0, 2.0);
        var svg = Svg(ax => ax.AddSeries(streaming));

        Assert.All(CircleRadii(svg, Colors.Red), r => Assert.Equal(5.0, r, 1));
        Assert.Equal(2, CircleRadii(svg, Colors.Red).Count);
    }

    [Fact]
    public void AScatterLegendHandle_ShowsTheMarkerAtTheSizeThePlotDrawsIt()
    {
        var svg = Svg(ax => ax.AddSeries(new ScatterSeries([1.0, 2.0], [1.0, 2.0]) { Color = Colors.Red, Label = "points" }).WithLegend());

        var radii = CircleRadii(svg, Colors.Red);
        Assert.Equal(3, radii.Count);   // two points and the handle
        Assert.All(radii, r => Assert.Equal(SixPointsInPixels / 2, r, 1));
    }

    [Fact]
    public void ALineLegendHandle_ShowsTheMarkerAtTheSizeThePlotDrawsIt()
    {
        var svg = Svg(ax => ax.AddSeries(new LineSeries([1.0, 2.0], [1.0, 2.0]) { Color = Colors.Red, Marker = MarkerStyle.Circle, Label = "line" }).WithLegend());

        var radii = CircleRadii(svg, Colors.Red);
        Assert.Equal(3, radii.Count);   // two points and the handle
        Assert.All(radii, r => Assert.Equal(SixPointsInPixels / 2, r, 1));
    }

    [Fact]
    public void A3DStemTip_IsItsSizeInPointsAcross()
    {
        var svg = Svg(ax => ax.Stem3D([1.0, 2.0], [1.0, 2.0], [1.0, 2.0], s => s.Color = Colors.Red));

        var radii = CircleRadii(svg, Colors.Red);
        Assert.Equal(2, radii.Count);
        Assert.All(radii, r => Assert.Equal(SixPointsInPixels / 2, r, 1));
    }

    [Fact]
    public void A3DScatterMarker_IsItsSizeInPointsAcross_AtTheNearestPoint()
    {
        // A 3-D scatter draws the nearest point at its full size and the farthest at half of it (the depth cue).
        var svg = Svg(ax => ax.Scatter3D([1.0, 2.0], [1.0, 2.0], [1.0, 2.0], s => s.Color = Colors.Red));

        var radii = CircleRadii(svg, Colors.Red);
        Assert.Equal(2, radii.Count);
        Assert.Equal(SixPointsInPixels / 2, radii.Max(), 1);
        Assert.Equal(SixPointsInPixels / 4, radii.Min(), 1);
    }

    [Fact]
    public void APolarScatterLegendHandle_IsItsMarker_AtTheSizeThePlotDrawsIt()
    {
        // matplotlib's legend handle for a scatter is its marker (HandlerPathCollection), a polar scatter's included; a filled rectangle
        // is the handle of a patch.
        var svg = Svg(ax => ax.PolarScatter([0.5, 1.0], [0.0, 1.0], s => { s.Color = Colors.Red; s.Label = "p"; }).WithLegend());

        var radii = CircleRadii(svg, Colors.Red);
        Assert.Equal(3, radii.Count);   // two points and the handle
        Assert.All(radii, r => Assert.Equal(SixPointsInPixels / 2, r, 1));
    }

    [Fact]
    public void A3DScatterLegendHandle_IsItsMarker_AtTheFullSizeThePlotDrawsTheNearestPoint()
    {
        var svg = Svg(ax => ax.Scatter3D([1.0, 2.0], [1.0, 2.0], [1.0, 2.0], s => { s.Color = Colors.Red; s.Label = "q"; }).WithLegend());

        var radii = CircleRadii(svg, Colors.Red);
        Assert.Equal(3, radii.Count);   // two points and the handle
        Assert.Equal(2, radii.Count(r => Math.Abs(r - SixPointsInPixels / 2) < 0.06));   // the nearest point and the handle
    }

    [Fact]
    public void A3DScatterLegendHandle_TakesTheThemesSizeOf3DScatter_NotTheSizeOfA2DScatter()
    {
        // Axes3D.scatter's own s=20 applies in every matplotlib style, where a 2-D scatter keeps lines.markersize ** 2 under v2: the
        // handle is the size the 3-D plot draws (SeriesSlot.Scatter3DSizeOf), not the 2-D rule's.
        var svg = Plt.Create().WithSize(400, 300).WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1,
            ax => ax.Scatter3D([1.0, 2.0], [1.0, 2.0], [1.0, 2.0], s => { s.Color = Colors.Red; s.Label = "q"; }).WithLegend()).Build().ToSvg().Xml();

        double drawn = Math.Sqrt(20) * 100.0 / 72.0 / 2;
        var radii = CircleRadii(svg, Colors.Red);
        Assert.Equal(3, radii.Count);
        Assert.All(radii, r => Assert.Equal(drawn, r, 1));   // the two markers keep one size, as matplotlib keeps them, and so does the handle
    }
}
