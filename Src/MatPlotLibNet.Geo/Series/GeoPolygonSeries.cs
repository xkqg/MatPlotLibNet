// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Geo.Series;

/// <summary>Renders projected geographic polygons from GeoJSON features. Coordinates are
/// pre-projected and self-rendered via <see cref="Accept"/> — no visitor registration needed
/// because this series lives in a separate package. With <see cref="Values"/> it is a choropleth: each feature is
/// filled with the colour its value has in <see cref="ColorMap"/>, and the colour bar reads the range of the values.</summary>
public sealed class GeoPolygonSeries : ChartSeries, IHasColor, IColormappable, INormalizable, IColorBarDataProvider
{
    private double[]? _values;

    /// <summary>The geographic features to render.</summary>
    public List<GeoFeature> Features { get; }

    /// <summary>The map projection applied to all features.</summary>
    public IGeoProjection Projection { get; }

    /// <summary>Fill color for polygons. A region that has a value (<see cref="Values"/>) is filled by the colour map instead.</summary>
    public Color? Color { get; set; }

    /// <summary>The value of each feature, parallel to <see cref="Features"/>; <see langword="null"/> for a map of one fill
    /// colour. A missing value (NaN) is drawn in the colour map's bad colour, transparent when it names none.</summary>
    /// <exception cref="ArgumentException">The array does not hold one value for each feature; the series keeps the values it
    /// had.</exception>
    public double[]? Values
    {
        get => _values;
        set
        {
            if (value is not null && value.Length != Features.Count)
                throw new ArgumentException($"Array lengths must match. Got {value.Length} values for {Features.Count} features.");
            _values = value;
        }
    }

    /// <inheritdoc />
    /// <remarks>Only a map with values: an ocean or a land, a plain fill, backs no colour bar.</remarks>
    internal override bool ProvidesColorBar => Values is not null;

    /// <summary>The colour map the values are drawn through; <see langword="null"/> for viridis.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>How a value is placed on the colour map between the smallest and the largest value; <see langword="null"/> for
    /// a linear scale.</summary>
    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    /// <remarks>The range of the values that can be drawn: a missing value and an infinity are left out; a single value is a
    /// range of one above it, as the colours are measured; with none the range is 0 to 1.</remarks>
    public MinMaxRange GetColorBarRange() =>
        Values?.DrawableRange() is { } range
            ? new(range.Lo, range.Lo == range.Hi ? range.Lo + 1 : range.Hi)
            : new(0, 1);

    /// <summary>Stroke color for polygon outlines. <see langword="null"/> draws no outline, unless the series wears the theme's
    /// neutral grey in its place (the outline of <c>Borders</c>, a choropleth and a cartogram until a colour is named).</summary>
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
    /// <remarks>The projection's bounds, and sticky: the series fills its plot from the bounds' edge to edge, so the axes it
    /// stands on takes exactly that range and no margin around it. A mark placed in the projected coordinates by another series
    /// then lands on the pixel the polygon of the same coordinates is drawn at.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        var bounds = Projection.Bounds;
        return new(bounds.XMin, bounds.XMax, bounds.YMin, bounds.YMax,
            StickyXMin: bounds.XMin, StickyXMax: bounds.XMax, StickyYMin: bounds.YMin, StickyYMax: bounds.YMax);
    }

    /// <summary>The discriminator a map is written under.</summary>
    private const string Discriminator = "geo_polygon";

    /// <summary>Registers the reader that rebuilds a map from the figure's JSON, so a figure holding a map reads back whole.
    /// Done once when this package loads; call it once in a process that reads JSON before it has used any type of this package,
    /// and again after the series registry was reset. Registering twice is the same as once.</summary>
    public static void RegisterReader() => SeriesRegistry.Register(Discriminator, FromSeriesDto);

    /// <inheritdoc />
    /// <remarks>The fill, the outline and its width travel in the fields every series shares; the projection and the features in
    /// the package data (<see cref="GeoPackageData"/>).</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = Discriminator,
        Label = Label,
        Color = Color,
        EdgeColor = StrokeColor,
        LineWidth = StrokeWidth.OrNullAtRest(0.5),
        Values = Values,
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        Normalizer = this.DescribeNormalizer(),
        PackageData = GeoPackageData.Write(this),
    };

    /// <summary>Rebuilds a map from its DTO and adds it to the axes.</summary>
    /// <returns>The map; <see langword="null"/> for a DTO that does not describe one this package can make (a figure written
    /// before maps carried their data, a projection or a source this version does not know) - the figure then reads without it, as
    /// for any series kind that is not known, and the drop is reported through the chart diagnostics.</returns>
    internal static GeoPolygonSeries? FromSeriesDto(Axes axes, SeriesDto dto)
    {
        if (GeoPackageData.Read(dto.PackageData) is not { } map) return null;
        var (projection, features, rawProjected) = map;

        var series = new GeoPolygonSeries(features, projection)
        {
            Color = dto.Color,
            StrokeColor = dto.EdgeColor,
            StrokeWidth = dto.LineWidth ?? 0.5,
            IsRawProjected = rawProjected,
        };
        if (dto.Values is { } values) series.Values = values;
        series.RestoreColorMap(dto);
        series.RestoreNormalizer(dto);
        return axes.AddSeries(series);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area)
    {
        // Self-rendering: project features and draw directly on the render context.
        // Uses PlotBounds + ComputeDataRange to map projected coords to pixels.
        var bounds = Projection.Bounds;
        // The outline: the colour named, or the theme's neutral grey when the series wears it in that place.
        var outline = StrokeColor ?? (WearsNeutral ? (visitor as SvgSeriesRenderer)?.Theme.Neutral : null);
        var plot = area.PlotBounds;

        // A ring as pixels; a position the projection cannot place (NaN) is left out.
        List<Point> Pixels(IEnumerable<(double Lon, double Lat)> ring)
        {
            var points = new List<Point>();
            foreach (var (lon, lat) in ring)
            {
                var (px, py) = IsRawProjected ? new ProjectedPoint(lon, lat) : Projection.Forward(lat, lon);
                if (double.IsNaN(px) || double.IsNaN(py)) continue;

                // Map projected coords to pixel space
                double fracX = (px - bounds.XMin) / (bounds.XMax - bounds.XMin);
                double fracY = 1.0 - (py - bounds.YMin) / (bounds.YMax - bounds.YMin);
                points.Add(new Point(plot.X + fracX * plot.Width, plot.Y + fracY * plot.Height));
            }

            return points;
        }

        // The colour of each region: the colour map's colour for its value, over the range of the values that can be drawn.
        var values = Values;
        var map = ColorMap ?? ColorMaps.Viridis;
        var normalizer = this.NormalizerOrLinear();
        var (min, max) = GetColorBarRange();

        for (int index = 0; index < Features.Count; index++)
        {
            var feature = Features[index];
            var fill = values is null ? Color : map.ColorOf(values[index], normalizer, min, max);

            if (feature.Geometry.Type is "Polygon" or "MultiPolygon")
            {
                // One path for each polygon, its exterior and its holes together under the non-zero fill rule, so what a hole
                // encloses is not filled.
                foreach (var polygon in feature.Geometry.Polygons)
                {
                    var exterior = Pixels(polygon.Exterior.Select(p => (p.Lon, p.Lat)));
                    if (exterior.Count < 3) continue;

                    var segments = new List<PathSegment>();
                    AddRing(segments, exterior);
                    foreach (var hole in polygon.Holes)
                    {
                        var pixels = Pixels(hole.Select(p => (p.Lon, p.Lat)));
                        if (pixels.Count >= 3) AddRing(segments, pixels);
                    }

                    area.Context.DrawPath(segments, new ShapeStyle(fill, outline, StrokeWidth));
                }
            }
            else
            {
                foreach (var ring in feature.Geometry.Rings)
                {
                    var points = Pixels(ring);
                    if (points.Count < 2) continue;
                    area.Context.DrawLines(points, new StrokeStyle(outline ?? Color ?? Colors.Black, StrokeWidth, LineStyle.Solid));
                }
            }
        }
    }

    /// <summary>A closed subpath through <paramref name="ring"/>.</summary>
    private static void AddRing(List<PathSegment> segments, List<Point> ring)
    {
        segments.Add(new MoveToSegment(ring[0]));
        for (int i = 1; i < ring.Count; i++) segments.Add(new LineToSegment(ring[i]));
        segments.Add(new CloseSegment());
    }
}
