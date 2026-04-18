// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that the geo extension methods (Coastlines, Borders, Land, Ocean)
/// actually add their series to the axes — bug found 2026-04-18 where the methods
/// constructed a series but never attached it, so SVGs rendered as blank.</summary>
public class GeoExtensionRenderTests
{
    [Fact]
    public void Coastlines_AddsSeriesToAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Coastlines(GeoProjection.Robinson))
            .Build();

        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Borders_AddsSeriesToAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Borders(GeoProjection.Robinson))
            .Build();

        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Land_AddsSeriesToAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Land(GeoProjection.Robinson, Colors.Green))
            .Build();

        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Ocean_AddsSeriesToAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Ocean(GeoProjection.Robinson, Colors.Blue))
            .Build();

        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void FullStack_AddsAllFourSeries()
    {
        var proj = GeoProjection.Robinson;
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(proj)
                .Ocean(proj, Colors.Blue)
                .Land(proj, Colors.Green)
                .Coastlines(proj, Colors.Black)
                .Borders(proj, Colors.Gray))
            .Build();

        Assert.Equal(4, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Coastlines_SvgContainsRenderedGeometry()
    {
        // Coastlines render as line segments — SVG must contain at least one <polyline>
        // (the GeoPolygonSeries draws LineString features via DrawLines → polyline).
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Coastlines(GeoProjection.Robinson, Colors.Black))
            .ToSvg();

        // Must contain polyline OR path (depending on backend); not just chrome.
        // The blank-SVG bug rendered nothing past the title/background rect.
        Assert.True(
            svg.Contains("<polyline") || svg.Contains("<path"),
            "SVG should contain rendered geometry from coastlines");
    }

    [Fact]
    public void Land_SvgContainsRenderedPolygons()
    {
        // Land renders countries as filled polygons — must contain at least one <polygon>.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Land(GeoProjection.Robinson, Colors.Green))
            .ToSvg();

        Assert.Contains("<polygon", svg);
    }
}
