// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo.Series;

/// <summary>Renders projected geographic polygons from GeoJSON features. Coordinates are
/// pre-projected and self-rendered via <see cref="Accept"/> — no visitor registration needed
/// because this series lives in a separate package.</summary>
public sealed class GeoPolygonSeries : ChartSeries, IHasColor
{
    /// <summary>The geographic features to render.</summary>
    public List<GeoFeature> Features { get; }

    /// <summary>The map projection applied to all features.</summary>
    public IGeoProjection Projection { get; }

    /// <summary>Fill color for polygons.</summary>
    public Color? Color { get; set; }

    /// <summary>Stroke color for polygon outlines.</summary>
    public Color? StrokeColor { get; set; }

    /// <summary>Stroke width. Default 0.5.</summary>
    public double StrokeWidth { get; set; } = 0.5;

    /// <summary>When true, ring coordinates are treated as already-projected (X, Y) values
    /// instead of (lon, lat). Used by background fills like <c>Ocean</c> that span the
    /// projection bounds rectangle directly.</summary>
    public bool IsRawProjected { get; init; }

    public GeoPolygonSeries(List<GeoFeature> features, IGeoProjection projection)
    {
        Features = features;
        Projection = projection;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        var bounds = Projection.Bounds;
        return new(bounds.XMin, bounds.XMax, bounds.YMin, bounds.YMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "geo_polygon", Label = Label };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area)
    {
        // Self-rendering: project features and draw directly on the render context.
        // Uses PlotBounds + ComputeDataRange to map projected coords to pixels.
        var bounds = Projection.Bounds;
        var plot = area.PlotBounds;

        foreach (var feature in Features)
        {
            foreach (var ring in feature.Geometry.Rings)
            {
                var points = new List<Point>();
                foreach (var (lon, lat) in ring)
                {
                    var (px, py) = IsRawProjected ? (lon, lat) : Projection.Forward(lat, lon);
                    if (double.IsNaN(px) || double.IsNaN(py)) continue;

                    // Map projected coords to pixel space
                    double fracX = (px - bounds.XMin) / (bounds.XMax - bounds.XMin);
                    double fracY = 1.0 - (py - bounds.YMin) / (bounds.YMax - bounds.YMin);
                    points.Add(new Point(plot.X + fracX * plot.Width, plot.Y + fracY * plot.Height));
                }

                if (points.Count < 3 && feature.Geometry.Type is "Polygon" or "MultiPolygon") continue;
                if (points.Count < 2) continue;

                if (feature.Geometry.Type is "Polygon" or "MultiPolygon")
                    area.Context.DrawPolygon(points.ToArray(), Color, StrokeColor, StrokeWidth);
                else
                    area.Context.DrawLines(points, StrokeColor ?? Color ?? Colors.Black, StrokeWidth, LineStyle.Solid);
            }
        }
    }
}
