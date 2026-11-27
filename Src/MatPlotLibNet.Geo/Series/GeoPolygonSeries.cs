// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Geo.Series;

/// <summary>Renders projected geographic polygons from GeoJSON features, drawn by its renderer
/// through the library's series pass. With <see cref="Values"/> it is a choropleth: each feature is
/// filled with the colour its value has in <see cref="ColorMap"/>, and the colour bar reads the range of the values. The axes it
/// stands on is a map axes: it keeps the projection's shape, one unit of Y as long as one of X, centred in its cell, and the
/// projection's bounds as its limits under every theme, as cartopy's <c>GeoAxes</c> does.</summary>
public sealed class GeoPolygonSeries : ChartSeries, IHasColor, IColormappable, INormalizable, IColorBarDataProvider, IColorScaled, IMapSeries, ISeriesKind<GeoPolygonSeries>
{
    private double[]? _values;

    /// <summary>The geographic features to render.</summary>
    public List<GeoFeature> Features { get; }

    /// <summary>The map projection applied to all features.</summary>
    public IGeoProjection Projection { get; }

    /// <summary>Fill color for polygons. A region that has a value (<see cref="Values"/>) is filled by the colour map instead. A map
    /// that is given none is filled in the colour its verb chose, if one did (a cartogram), as every series that names none is drawn
    /// in the colour its theme gives it, and not at all otherwise.</summary>
    public Color? Color { get; set; }

    /// <summary>The value of each feature, parallel to <see cref="Features"/>; <see langword="null"/> for a map of one fill
    /// colour. A missing value (NaN) is drawn in the colour map's bad colour, transparent when it names none.</summary>
    /// <exception cref="ArgumentException">The array does not hold one value for each feature; the series keeps the values it
    /// had.</exception>
    public double[]? Values
    {
        get => _values;
        set => _values = value.ThrowIfNotOnePer(Features.Count, nameof(Values));
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
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(Values);

    /// <summary>Stroke color for polygon outlines. <see langword="null"/> draws no outline, unless the series wears the theme's
    /// neutral grey in its place (the outline of <c>Borders</c>, a choropleth and a cartogram until a colour is named).</summary>
    public Color? StrokeColor { get; set; }

    private const double RestingStrokeWidth = 0.5;

    /// <summary>Stroke width. Default <see cref="RestingStrokeWidth"/>.</summary>
    public double StrokeWidth { get; set; } = RestingStrokeWidth;

    /// <summary>When true, ring coordinates are treated as already-projected (X, Y) values
    /// instead of (lon, lat). Used by background fills like <c>Ocean</c> that span the
    /// projection bounds rectangle directly.</summary>
    public bool IsRawProjected { get; init; }

    /// <summary>Creates a map series that draws the given features through the given projection.</summary>
    /// <param name="features">The geographic features to draw; the list is kept, not copied.</param>
    /// <param name="projection">The map projection that places each feature's coordinates on the plot.</param>
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
        return DataRangeContribution.Pinned(new(bounds.XMin, bounds.XMax), new(bounds.YMin, bounds.YMax));
    }

    /// <inheritdoc />
    /// <remarks>The projection's outline (<see cref="ProjectionOutlineExtensions.Outline"/>): the oval of a world map, the circle of a
    /// globe, the rectangle of the bounds for a cylindrical or a conic map, placed by the transform of the axes.</remarks>
    IReadOnlyList<Point> IMapSeries.OutlineIn(DataTransform transform) =>
        GeoPixelMapping.For(Projection, rawProjected: true, transform).Ring(Projection.Outline());

    static string ISeriesKind<GeoPolygonSeries>.Kind => "geo_polygon";

    /// <summary>Registers the reader that rebuilds a map from the figure's JSON, so a figure holding a map reads back whole.
    /// Done once when this package loads; call it once in a process that reads JSON before it has used any type of this package,
    /// and again after the series registry was reset. Registering twice is the same as once.</summary>
    public static void RegisterReader() => SeriesRegistry.RegisterReader<GeoPolygonSeries>(FromSeriesDto);

    /// <inheritdoc />
    /// <remarks>The fill, the outline and its width travel in the fields every series shares; the projection and the features in
    /// the package data.</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Label = Label,
        Color = Color,
        EdgeColor = StrokeColor,
        LineWidth = StrokeWidth.OrNullAtRest(RestingStrokeWidth),
        Values = Values,
        ColorScale = this.ColorScaleOnTheWire(),
        PackageData = new GeoMapData(Projection, Features, IsRawProjected).Write(),
    };

    /// <summary>The reader of a map that <see cref="SeriesRegistry"/> takes.</summary>
    /// <param name="axes">The axes the map is added to.</param>
    /// <param name="dto">The record of the map.</param>
    /// <returns>The map; no map and the reason for a DTO that does not describe one this package can make (a figure written before
    /// maps carried their data, a projection or a source this version does not know) - the figure then reads without it, and the
    /// drop is reported through the chart diagnostics with that reason.</returns>
    internal static SeriesRead FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var read = GeoMapData.Read(dto.PackageData);
        if (read.Map is not { } map) return SeriesRead.Unreadable(read.Why!);
        var (projection, features, rawProjected) = map;

        var series = new GeoPolygonSeries(features, projection)
        {
            Color = dto.Color,
            StrokeColor = dto.EdgeColor,
            StrokeWidth = dto.LineWidth ?? RestingStrokeWidth,
            IsRawProjected = rawProjected,
        };
        if (dto.Values is { } values) series.Values = values;
        series.RestoreColorScale(dto);
        return SeriesRead.Of(axes.AddSeries(series));
    }

    /// <inheritdoc />
    /// <remarks>Drawn by its renderer through the library's series pass, which the public visitor reaches
    /// for a series of a package through the host it is (<c>ISeriesRendererHost</c>); a visitor of your own is handed no map.</remarks>
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Draw(this, static context => new GeoPolygonRenderer(context));

    /// <inheritdoc />
    /// <remarks>Drawn in the colour the map is drawn in, by the rules its renderer draws with: a map that fills its regions is
    /// a rectangle in its fill (a choropleth in its first region's colour, as matplotlib's <c>HandlerPolyCollection</c> takes the first
    /// face colour); a map of lines, or one that only outlines its regions (the coastlines, the borders), is a line as wide and in the
    /// colour of its stroke, as <c>HandlerLineCollection</c> draws a collection of lines.</remarks>
    internal override IReadOnlyList<LegendPart> LegendPartsUnder(Theme theme)
    {
        if (string.IsNullOrEmpty(Label)) return [];
        if (Values is { Length: > 0 } values)
            return [new LegendPart(Label, LegendSwatch.Patch, Color: this.ColorScaleUnder(theme).ColorOf(values[0]))];
        if ((Color ?? VerbColor) is not null && Features.Exists(feature => feature.Geometry.Kind.IsArea))
            return base.LegendPartsUnder(theme);
        return [new LegendPart(Label, LegendSwatch.Line(GivenStroke.Solid(StrokeWidth)),
            Color: LineColorOver(OutlineUnder(theme)))];
    }

    /// <summary>The one colour every region is filled in: the colour named, else the colour the verb chose, as the pass of the series
    /// hands it over (the verb's under the library's own themes, the theme's cycle under the matplotlib themes); none when neither was
    /// given.</summary>
    /// <param name="unnamed">The colour the pass gives a series that names none.</param>
    internal Color? FillOfAll(Color unnamed) => Color ?? (VerbColor is not null ? unnamed : null);

    /// <summary>The colour the outline of a region and a line are drawn in: the colour named, or the theme's neutral grey when the
    /// series wears it in that place; none when neither says one.</summary>
    internal Color? OutlineUnder(Theme theme) => StrokeColor ?? (WearsNeutral ? theme.Neutral : null);

    /// <summary>The colour a line is drawn in: its outline, else the fill named, else black.</summary>
    internal Color LineColorOver(Color? outline) => outline ?? Color ?? Colors.Black;
}
