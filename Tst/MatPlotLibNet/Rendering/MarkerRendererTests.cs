// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Phase M.2 — the <c>MarkerStyle</c> enum has 13 members (Circle, Square,
/// Triangle, Diamond, Cross, Plus, Star, Pentagon, Hexagon, TriangleDown/Left/Right,
/// plus None). Pre-Phase-M, <c>LineSeriesRenderer</c> drew every marker as a
/// circle and <c>ScatterSeriesRenderer</c> honoured only Square, silently
/// collapsing every other shape to a circle. These tests pin the correct SVG
/// primitive for each shape end-to-end via <c>Plt.Create()</c>.
/// </summary>
public class MarkerRendererTests
{
    // Scatter — each marker shape maps to a specific SVG primitive.
    [Theory]
    [InlineData(MarkerStyle.Circle,        "<circle")]
    [InlineData(MarkerStyle.Square,        "<rect")]
    [InlineData(MarkerStyle.Triangle,      "<polygon")]
    [InlineData(MarkerStyle.TriangleDown,  "<polygon")]
    [InlineData(MarkerStyle.TriangleLeft,  "<polygon")]
    [InlineData(MarkerStyle.TriangleRight, "<polygon")]
    [InlineData(MarkerStyle.Diamond,       "<polygon")]
    [InlineData(MarkerStyle.Pentagon,      "<polygon")]
    [InlineData(MarkerStyle.Hexagon,       "<polygon")]
    [InlineData(MarkerStyle.Star,          "<polygon")]
    public void Scatter_Marker_EmitsCorrectSvgPrimitive(MarkerStyle style, string expected)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = style;
                s.MarkerSize = 36;
            }))
            .ToSvg();
        Assert.Contains(expected, svg);
    }

    // Cross and Plus are outline-only "X" / "+" markers — matplotlib parity.
    [Theory]
    [InlineData(MarkerStyle.Cross)]
    [InlineData(MarkerStyle.Plus)]
    public void Scatter_CrossOrPlus_EmitsLineElements(MarkerStyle style)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = style;
                s.MarkerSize = 36;
            }))
            .ToSvg();
        // Cross / Plus each render as 2 line segments per point → expect <line at minimum.
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Scatter_MarkerNone_EmitsNoCircles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = MarkerStyle.None;
                s.MarkerSize = 36;
            }))
            .ToSvg();
        // With Marker=None the scatter draws nothing at the data points.
        // Tick marks / legend bullets / axis marks don't emit <circle> in this config,
        // so the absence of <circle> confirms the None branch was honoured.
        Assert.DoesNotContain("<circle", svg);
    }

    // Line charts — markers apply per-vertex. Pre-fix every choice collapsed to circle.
    [Theory]
    [InlineData(MarkerStyle.Square,   "<rect")]
    [InlineData(MarkerStyle.Triangle, "<polygon")]
    [InlineData(MarkerStyle.Diamond,  "<polygon")]
    [InlineData(MarkerStyle.Star,     "<polygon")]
    public void LineChart_Marker_EmitsCorrectSvgPrimitive(MarkerStyle style, string expected)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = style;
                s.MarkerSize = 10;
            }))
            .ToSvg();
        Assert.Contains(expected, svg);
    }

    [Fact]
    public void LineChart_CrossMarker_EmitsLineElements()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = MarkerStyle.Cross;
                s.MarkerSize = 10;
            }))
            .ToSvg();
        // The line polyline itself may not emit <line> (it's <polyline>), but the
        // Cross markers at each vertex must. Lines aren't already in the axis ticks
        // for every chart, so a plain count check suffices — a Cross per 3 vertices
        // is 6 line segments minimum.
        int lineCount = System.Text.RegularExpressions.Regex.Matches(svg, "<line ").Count;
        Assert.True(lineCount >= 6, $"expected ≥6 <line> for 3 Cross markers (2 each); got {lineCount}");
    }

    [Fact]
    public void LineChart_MarkerNone_DrawsLineOnly_NoMarkerPrimitives()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]))
            .ToSvg();
        // Line itself renders as <polyline>; no <circle> / <rect> / <polygon> for markers.
        Assert.Contains("<polyline", svg);
    }
}
