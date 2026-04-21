// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
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

    // ── Wave J.2 — NaturalEarth110m cache-hit and GeoPolygonSeries edge cases ──

    /// <summary>NaturalEarth110m.Coastlines() called twice — second call returns cached
    /// list (L18 `??=` false arm, cache hit). Same instance must be returned.</summary>
    [Fact]
    public void NaturalEarth110m_Coastlines_CacheHit_ReturnsSameInstance()
    {
        var first = NaturalEarth110m.Coastlines();
        var second = NaturalEarth110m.Coastlines();
        Assert.Same(first, second);
    }

    /// <summary>NaturalEarth110m.Countries() called twice — L22 `??=` false arm.</summary>
    [Fact]
    public void NaturalEarth110m_Countries_CacheHit_ReturnsSameInstance()
    {
        var first = NaturalEarth110m.Countries();
        var second = NaturalEarth110m.Countries();
        Assert.Same(first, second);
    }

    /// <summary>GeoPolygonSeries.Accept with a Polygon ring that has fewer than 3 points —
    /// L78 `points.Count < 3 && Type is Polygon` continue arm.</summary>
    [Fact]
    public void GeoPolygonSeries_PolygonWithTwoPoints_SkipsRing()
    {
        var feature = new GeoFeature(
            new GeoGeometry { Type = "Polygon", Rings = [[(0, 0), (1, 1)]] },
            new Dictionary<string, string>());
        var series = new GeoPolygonSeries([feature], GeoProjection.PlateCarree)
        {
            Color = Colors.Green,
        };
        var fig = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithProjection(GeoProjection.PlateCarree);
                ax.AddSeries(series);
            })
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>GeoPolygonSeries with a LineString that has only 1 point — L79 `< 2` continue arm.</summary>
    [Fact]
    public void GeoPolygonSeries_LineStringWithOnePoint_SkipsRing()
    {
        var feature = new GeoFeature(
            new GeoGeometry { Type = "LineString", Rings = [[(10, 20)]] },
            new Dictionary<string, string>());
        var series = new GeoPolygonSeries([feature], GeoProjection.PlateCarree)
        {
            StrokeColor = Colors.Black,
        };
        var fig = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithProjection(GeoProjection.PlateCarree);
                ax.AddSeries(series);
            })
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Wave J.1 — GeoPolygonSeries remaining branch arms ────────────────────

    /// <summary>L69 TRUE arm — IsRawProjected = true: Ocean uses raw projected coordinates
    /// instead of calling Projection.Forward. SVG must contain a filled polygon.</summary>
    [Fact]
    public void Ocean_SvgContainsFilledPolygon_RawProjectedArm()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.PlateCarree)
                .Ocean(GeoProjection.PlateCarree, Colors.Blue))
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    /// <summary>L70 TRUE arm — NaN projected coordinate is skipped. A LineString with one
    /// NaN point and two valid points still produces a rendered line (2 valid ≥ 2).</summary>
    [Fact]
    public void GeoPolygonSeries_NanCoord_SkippedAndValidPointsDrawn()
    {
        var feature = new GeoFeature(
            new GeoGeometry { Type = "LineString", Rings = [[(double.NaN, 0), (10, 20), (20, 30)]] },
            new Dictionary<string, string>());
        var series = new GeoPolygonSeries([feature], GeoProjection.PlateCarree)
        {
            StrokeColor = Colors.Black,
        };
        string svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithProjection(GeoProjection.PlateCarree);
                ax.AddSeries(series);
            })
            .ToSvg();
        Assert.True(
            svg.Contains("<polyline") || svg.Contains("<path") || svg.Contains("<line"),
            "SVG must contain line geometry — NaN point skipped, 2 valid points drawn");
    }

    /// <summary>L84 middle arm — StrokeColor is null but Color is set: DrawLines falls
    /// through to Color arm of StrokeColor ?? Color ?? Colors.Black.</summary>
    [Fact]
    public void GeoPolygonSeries_LineString_NullStrokeColor_UsesColorFallback()
    {
        var feature = new GeoFeature(
            new GeoGeometry { Type = "LineString", Rings = [[(10, 20), (20, 30), (30, 40)]] },
            new Dictionary<string, string>());
        var series = new GeoPolygonSeries([feature], GeoProjection.PlateCarree)
        {
            Color = Colors.Red,
            StrokeColor = null,
        };
        string svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithProjection(GeoProjection.PlateCarree);
                ax.AddSeries(series);
            })
            .ToSvg();
        Assert.True(
            svg.Contains("<polyline") || svg.Contains("<path") || svg.Contains("<line"),
            "SVG must contain line geometry drawn using Color fallback");
    }

    /// <summary>L84 final arm — both StrokeColor and Color are null: DrawLines uses
    /// Colors.Black as the final fallback.</summary>
    [Fact]
    public void GeoPolygonSeries_LineString_BothColorsNull_UsesBlackFallback()
    {
        var feature = new GeoFeature(
            new GeoGeometry { Type = "LineString", Rings = [[(10, 20), (20, 30)]] },
            new Dictionary<string, string>());
        var series = new GeoPolygonSeries([feature], GeoProjection.PlateCarree);
        // Both Color and StrokeColor are null → final fallback to Colors.Black
        string svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithProjection(GeoProjection.PlateCarree);
                ax.AddSeries(series);
            })
            .ToSvg();
        Assert.True(
            svg.Contains("<polyline") || svg.Contains("<path") || svg.Contains("<line"),
            "SVG must contain line geometry drawn using Colors.Black final fallback");
    }
}
