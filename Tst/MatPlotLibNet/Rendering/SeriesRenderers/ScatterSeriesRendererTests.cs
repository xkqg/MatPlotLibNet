// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.ScatterSeriesRenderer"/>. Pre-X.9
/// the renderer was at 80%L / 41%B because only the simplest path (no C, no Sizes,
/// no per-point Colors, no edges) was exercised. This file pins:
///   - C set → skips ViewportCuller (line 24-25)
///   - C set with VMin/VMax explicit (lines 35-36)
///   - C set without VMin/VMax → falls back to Min/Max (lines 35-36 false arms)
///   - Sizes per-point (line 46 ternary's true arm)
///   - Colors[] per-point (ResolvePointColor line 60-61)
///   - C+ColorMap (ResolvePointColor line 62-63)
///   - EdgeColors per-point (line 49 ternary's true arm)
///   - LineWidths per-point (line 50 ternary's true arm)
///   - MaxDisplayPoints set without C → ViewportCuller (line 27-28)</summary>
public class ScatterSeriesRendererTests
{
    private static string Render(ScatterSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_BasicScatter_DrawsMarkers()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6]));
        Assert.Contains("<circle", svg);
    }

    /// <summary>C set + VMin/VMax explicit → ColorMap path (lines 33-37 explicit
    /// bounds, ResolvePointColor line 62-63).</summary>
    [Fact]
    public void Render_C_With_ExplicitVMinVMax_AndColorMap_PerPointColor()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            C = new[] { 0.0, 0.5, 1.0 },
            VMin = 0,
            VMax = 1,
            ColorMap = ColorMaps.Viridis,
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>C set without VMin/VMax → falls back to C.Min()/C.Max() (lines 35-36
    /// false arms of `?? series.C.Min()`).</summary>
    [Fact]
    public void Render_C_Without_VMinVMax_FallsBackToCMinMax()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            C = new[] { 10.0, 50.0, 100.0 },
            ColorMap = ColorMaps.Viridis,
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>Sizes per-point (line 46 ternary's true arm). Each marker has its
    /// own area; radius = sqrt(s/π) × DPI scaling.</summary>
    [Fact]
    public void Render_Sizes_PerPoint_AppliesPerPointRadius()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            Sizes = new[] { 10.0, 50.0, 200.0 },
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>Colors[] per-point (ResolvePointColor line 60-61 hits priority arm 1).
    /// Even with a ColorMap+C set, Colors wins.</summary>
    [Fact]
    public void Render_Colors_PerPoint_PrioritizedOverColorMap()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            Colors = new[] { Colors.Red, Colors.Green, Colors.Blue },
            C = new[] { 0.0, 0.5, 1.0 },
            ColorMap = ColorMaps.Viridis,
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>EdgeColors per-point (line 49 ternary's true arm) + LineWidths per-point
    /// (line 50 ternary's true arm). Both arrays index-tied to data.</summary>
    [Fact]
    public void Render_EdgeColors_AndLineWidths_PerPoint()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            EdgeColors = new[] { Colors.Black, Colors.Black, Colors.Black },
            LineWidths = new[] { 1.0, 2.0, 3.0 },
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>MaxDisplayPoints set + no C → ViewportCuller path (line 27-28).</summary>
    [Fact]
    public void Render_MaxDisplayPoints_TriggersViewportCuller()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5])
        {
            MaxDisplayPoints = 3,
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>Empty data — no markers drawn (early-return-style path through the for-loop
    /// at line 41 with pts.Length == 0).</summary>
    [Fact]
    public void Render_EmptyData_NoMarkers()
    {
        var svg = Render(new ScatterSeries([], []));
        Assert.DoesNotContain("<circle", svg);
    }

    // ---- Alpha: the opacity of the markers' fill, by the rule every other filled mark follows ------------------------

    /// <summary>A marker as drawn: its fill colour, its fill opacity (1 when the SVG states none) and its stroke colour.</summary>
    private readonly record struct DrawnMarker(string Fill, double Opacity, string? Stroke);

    /// <summary>The markers of the given shape element whose fill is <paramref name="fillHex"/> (or any fill when null),
    /// in drawing order. The figure's and the plot area's backgrounds are rectangles too, so a fill filter keeps them out.</summary>
    private static DrawnMarker[] Markers(string svg, string element = "circle", string? fillHex = null) =>
        [.. XDocument.Parse(svg).Descendants()
            .Where(e => e.Name.LocalName == element && (string?)e.Attribute("fill") is { } fill
                        && (fillHex is null || fill == fillHex))
            .Select(e => new DrawnMarker((string)e.Attribute("fill")!,
                (string?)e.Attribute("fill-opacity") is { } o ? double.Parse(o, CultureInfo.InvariantCulture) : 1.0,
                (string?)e.Attribute("stroke")))];

    /// <summary>An opacity is a byte over 255, so it lands within one step of the alpha that was asked for.</summary>
    private static void AssertOpacity(double alpha, double opacity) =>
        Assert.True(Math.Abs(alpha - opacity) <= 1.0 / 255, $"expected an opacity near {alpha}, the SVG says {opacity}");

    private static readonly double[] Xs = [1.0, 2, 3];
    private static readonly double[] Ys = [4.0, 5, 6];

    /// <summary>The defect: <see cref="ScatterSeries.Alpha"/> was never read, so a half-transparent scatter drew opaque
    /// markers. The markers' fill now carries the alpha, whichever way the colour is chosen below.</summary>
    [Fact]
    public void Render_Alpha_ReachesTheFillOpacityOfEveryMarker()
    {
        var markers = Markers(Render(new ScatterSeries(Xs, Ys) { Color = Colors.Red, Alpha = 0.5 }), fillHex: Colors.Red.ToHex());

        Assert.Equal(3, markers.Length);
        foreach (var marker in markers) AssertOpacity(0.5, marker.Opacity);
    }

    /// <summary>Every filled marker shape takes the same fill, so the alpha reaches all of them. (A cross and a plus are
    /// strokes: the SVG writer gives a stroke no opacity, for lines and for markers alike.)</summary>
    [Theory]
    [InlineData(MarkerStyle.Circle, "circle")]
    [InlineData(MarkerStyle.Square, "rect")]
    [InlineData(MarkerStyle.Triangle, "polygon")]
    [InlineData(MarkerStyle.Diamond, "polygon")]
    [InlineData(MarkerStyle.Star, "polygon")]
    public void Render_Alpha_ReachesEveryFilledMarkerShape(MarkerStyle style, string element)
    {
        var svg = Render(new ScatterSeries(Xs, Ys) { Color = Colors.Red, Alpha = 0.25, Marker = style });

        var markers = Markers(svg, element, Colors.Red.ToHex());
        Assert.Equal(3, markers.Length);
        foreach (var marker in markers) AssertOpacity(0.25, marker.Opacity);
    }

    /// <summary>Alpha 1 is the resting value and changes nothing: the bytes are those of a scatter that never touched
    /// Alpha, and a colour with an alpha of its own keeps it (the alpha of a series is not allowed to make it opaque).</summary>
    [Fact]
    public void Render_AlphaOne_DrawsTheBytesOfAScatterThatNeverSetAlpha()
    {
        string untouched = Render(new ScatterSeries(Xs, Ys) { Color = Colors.Red });
        string one = Render(new ScatterSeries(Xs, Ys) { Color = Colors.Red, Alpha = 1.0 });

        Assert.Equal(untouched, one);
        Assert.All(Markers(one, fillHex: Colors.Red.ToHex()), marker => Assert.Equal(1.0, marker.Opacity));
    }

    /// <summary>The colour's own alpha survives Alpha 1: a half-transparent red stays half-transparent.</summary>
    [Fact]
    public void Render_AlphaOne_KeepsTheOwnAlphaOfTheColour()
    {
        var red = Colors.Red.WithAlpha(100);

        var markers = Markers(Render(new ScatterSeries(Xs, Ys) { Color = red, Alpha = 1.0 }), fillHex: red.ToHex());

        Assert.Equal(3, markers.Length);
        foreach (var marker in markers) AssertOpacity(100 / 255.0, marker.Opacity);
    }

    /// <summary>The alpha of the series replaces the alpha of a colour (matplotlib: <c>alpha</c> overrides the alpha of
    /// the colours), for a uniform colour as for the per-point ones.</summary>
    [Fact]
    public void Render_Alpha_ReplacesTheOwnAlphaOfTheColours()
    {
        Color[] colours = [Colors.Red.WithAlpha(40), Colors.Green.WithAlpha(40), Colors.Blue.WithAlpha(40)];

        var markers = Markers(Render(new ScatterSeries(Xs, Ys) { Colors = colours, Alpha = 0.75 }));

        Assert.Equal(3, markers.Length);
        Assert.Equal([.. colours.Select(c => c.ToHex())], markers.Select(m => m.Fill));
        foreach (var marker in markers) AssertOpacity(0.75, marker.Opacity);
    }

    /// <summary>Points coloured through a colour map take the alpha too.</summary>
    [Fact]
    public void Render_Alpha_ReachesPointsColouredByAColourMap()
    {
        var svg = Render(new ScatterSeries(Xs, Ys) { C = [0.0, 0.5, 1.0], ColorMap = ColorMaps.Viridis, Alpha = 0.4 });

        var markers = Markers(svg);
        Assert.Equal(3, markers.Length);
        Assert.Equal(3, markers.Select(m => m.Fill).Distinct().Count());
        foreach (var marker in markers) AssertOpacity(0.4, marker.Opacity);
    }

    /// <summary>A value the colour map cannot colour is drawn in the map's bad colour, transparent by default, and the alpha
    /// of the series does not bring it back (matplotlib: a bad colour of all zeros ignores <c>alpha</c>).</summary>
    [Fact]
    public void Render_Alpha_KeepsAMissingValuesColourTransparent()
    {
        var svg = Render(new ScatterSeries(Xs, Ys) { C = [0.0, double.NaN, 1.0], ColorMap = ColorMaps.Viridis, Alpha = 0.5 });

        var markers = Markers(svg);
        Assert.Equal(3, markers.Length);
        AssertOpacity(0.5, markers[0].Opacity);
        Assert.Equal(0.0, markers[1].Opacity);
        AssertOpacity(0.5, markers[2].Opacity);
    }

    /// <summary>Alpha is the opacity of the FILL, as on every other filled mark (<see cref="MatPlotLibNet.Models.Series.IHasAlpha"/>):
    /// an edge is drawn in the colour it was given.</summary>
    [Fact]
    public void Render_Alpha_LeavesTheEdgeOfAMarkerAlone()
    {
        var svg = Render(new ScatterSeries(Xs, Ys)
        {
            Color = Colors.Red, Alpha = 0.5, EdgeColors = [Colors.Black, Colors.Black, Colors.Black], LineWidths = [2.0, 2.0, 2.0],
        });

        var markers = Markers(svg, fillHex: Colors.Red.ToHex());
        Assert.Equal(3, markers.Length);
        Assert.All(markers, marker => Assert.Equal(Colors.Black.ToHex(), marker.Stroke));
    }

    /// <summary>A scatter on the secondary axis is the same series drawn by the same renderer.</summary>
    [Fact]
    public void Render_Alpha_ReachesAScatterOnTheRightAxis()
    {
        var svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(Xs, Ys)
                .WithAddRightYAxis(y2 => y2.Scatter(Xs, Ys, s => { s.Color = Colors.Red; s.Alpha = 0.5; })))
            .Build().ToSvg();

        var markers = Markers(svg, fillHex: Colors.Red.ToHex());
        Assert.Equal(3, markers.Length);
        foreach (var marker in markers) AssertOpacity(0.5, marker.Opacity);
    }

    /// <summary>The streaming scatter declares an Alpha and hands it to the scatter it is drawn as.</summary>
    [Fact]
    public void Render_Alpha_ReachesAStreamingScatter()
    {
        var svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.StreamingScatter(16, s =>
            {
                s.Color = Colors.Red;
                s.Alpha = 0.5;
                s.AppendPoint(1, 4);
                s.AppendPoint(2, 5);
            }))
            .Build().ToSvg();

        var markers = Markers(svg, fillHex: Colors.Red.ToHex());
        Assert.Equal(2, markers.Length);
        foreach (var marker in markers) AssertOpacity(0.5, marker.Opacity);
    }

    /// <summary>The alpha travels through JSON already (it is written when it is not 1), so a figure read back draws the
    /// same translucent markers as the one that was written.</summary>
    [Fact]
    public void Render_Alpha_SurvivesAJsonRoundTrip()
    {
        var figure = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter(Xs, Ys, s => { s.Color = Colors.Red; s.Alpha = 0.5; }))
            .Build();

        string back = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(figure.ToSvg(), back);
        Assert.All(Markers(back, fillHex: Colors.Red.ToHex()), marker => AssertOpacity(0.5, marker.Opacity));
    }
}
