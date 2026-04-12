// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Renders GeoJSON geometry (polygons, lines, points) on a projected map.</summary>
public class MapSeries : ChartSeries, IHasEdgeColor
{
    /// <summary>The GeoJSON document supplying the geometry to render.</summary>
    public GeoJsonDocument? GeoData { get; set; }

    /// <summary>The map projection used to convert geographic coordinates to screen coordinates.</summary>
    public IMapProjection Projection { get; set; } = MapProjections.Equirectangular();

    /// <summary>Fill color for polygon interiors. <c>null</c> = transparent.</summary>
    public Color? FaceColor { get; set; }

    /// <summary>Stroke color for polygon / line edges. <c>null</c> = use theme foreground color.</summary>
    public Color? EdgeColor { get; set; }

    /// <summary>Stroke width in pixels (default 0.5).</summary>
    public double LineWidth { get; set; } = 0.5;

    /// <summary>Initializes an empty <see cref="MapSeries"/>.</summary>
    public MapSeries() { }

    /// <summary>Initializes a <see cref="MapSeries"/> with the given GeoJSON document.</summary>
    /// <param name="geoData">The GeoJSON document to render.</param>
    public MapSeries(GeoJsonDocument? geoData) { GeoData = geoData; }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <inheritdoc />
    /// <remarks>MapSeries has no meaningful Cartesian data range — returns null on all axes.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "map",
        GeoJson = GeoData is not null ? GeoJsonWriter.ToJson(GeoData) : null,
        Projection = ProjectionName(Projection),
        Color = EdgeColor,
        LineWidth = LineWidth,
        FillColor = FaceColor
    };

    internal static string ProjectionName(IMapProjection p) => p switch
    {
        MercatorProjection => "mercator",
        _ => "equirectangular"
    };

    internal static IMapProjection ProjectionFromName(string? name) => name switch
    {
        "mercator" => MapProjections.Mercator(),
        _ => MapProjections.Equirectangular()
    };
}
