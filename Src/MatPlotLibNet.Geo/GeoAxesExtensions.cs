// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo;

/// <summary>Extension methods for adding geographic features to an <see cref="AxesBuilder"/>.
/// Provides a cartopy-like API: <c>ax.WithProjection(Robinson).Coastlines().Borders()</c>.</summary>
public static class GeoAxesExtensions
{
    /// <summary>Sets the map projection for this axes and configures it for geographic display
    /// (hides standard axes chrome). Subsequent calls to <see cref="Coastlines"/>, <see cref="Borders"/>,
    /// <see cref="Ocean"/>, and <see cref="Land"/> use the projection passed to those methods.</summary>
    public static AxesBuilder WithProjection(this AxesBuilder ax, IGeoProjection projection)
    {
        ax.HideAllAxes();
        return ax;
    }

    /// <summary>Adds Natural Earth 110m coastlines to the axes. Coastlines are LineString
    /// geometries — <paramref name="color"/> sets the stroke color (default: black).</summary>
    public static AxesBuilder Coastlines(this AxesBuilder ax, IGeoProjection projection,
        Color? color = null, double lineWidth = 0.5)
    {
        var features = NaturalEarth110m.Coastlines();
        var series = new GeoPolygonSeries(features, projection)
        {
            StrokeColor = color ?? Colors.Black,
            StrokeWidth = lineWidth,
            Label = "Coastlines"
        };
        return ax.AddSeries(series);
    }

    /// <summary>Adds Natural Earth 110m country borders to the axes. <paramref name="color"/>
    /// sets the stroke color (default: gray).</summary>
    public static AxesBuilder Borders(this AxesBuilder ax, IGeoProjection projection,
        Color? color = null, double lineWidth = 0.3)
    {
        var features = NaturalEarth110m.Countries();
        var series = new GeoPolygonSeries(features, projection)
        {
            StrokeColor = color ?? Colors.Gray,
            StrokeWidth = lineWidth,
            Label = "Borders"
        };
        return ax.AddSeries(series);
    }

    /// <summary>Fills ocean areas with the specified color. Ocean is rendered as a single
    /// polygon spanning the full projection bounds (drawn first so land/coastlines layer on top).</summary>
    public static AxesBuilder Ocean(this AxesBuilder ax, IGeoProjection projection, Color color)
    {
        var bounds = projection.Bounds;
        var oceanFeature = new GeoFeature(
            new GeoGeometry
            {
                Type = "Polygon",
                Rings = [[(bounds.XMin, bounds.YMin), (bounds.XMax, bounds.YMin),
                          (bounds.XMax, bounds.YMax), (bounds.XMin, bounds.YMax),
                          (bounds.XMin, bounds.YMin)]]
            },
            new Dictionary<string, string>());

        var series = new GeoPolygonSeries([oceanFeature], projection)
        {
            Color = color,
            Label = "Ocean",
            ZOrder = -2,  // behind everything
            IsRawProjected = true,  // ring uses projected (X, Y), not (lon, lat)
        };
        return ax.AddSeries(series);
    }

    /// <summary>Fills land areas with the specified color using country polygons.</summary>
    public static AxesBuilder Land(this AxesBuilder ax, IGeoProjection projection, Color color)
    {
        var features = NaturalEarth110m.Countries();
        var series = new GeoPolygonSeries(features, projection)
        {
            Color = color,
            Label = "Land",
            ZOrder = -1,  // above ocean, below coastlines/borders
        };
        return ax.AddSeries(series);
    }
}
