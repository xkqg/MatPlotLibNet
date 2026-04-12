// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Extends <see cref="MapSeries"/> by filling each GeoJSON feature with a color
/// derived from a data value mapped through a colormap.</summary>
public sealed class ChoroplethSeries : MapSeries, IColormappable, INormalizable
{
    /// <summary>One data value per feature in <see cref="MapSeries.GeoData"/>.</summary>
    public double[]? Values { get; set; }

    /// <summary>Colormap used to map normalized values to fill colors (default: Viridis).</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Optional normalizer for value-to-[0,1] mapping (default: linear).</summary>
    public INormalizer? Normalizer { get; set; }

    /// <summary>Optional minimum value override for normalization.</summary>
    public double? VMin { get; set; }

    /// <summary>Optional maximum value override for normalization.</summary>
    public double? VMax { get; set; }

    /// <summary>Initializes an empty <see cref="ChoroplethSeries"/>.</summary>
    public ChoroplethSeries() { }

    /// <summary>Initializes a new <see cref="ChoroplethSeries"/> with geography and values.</summary>
    /// <param name="geoData">GeoJSON document whose features are filled by <paramref name="values"/>.</param>
    /// <param name="values">One data value per feature.</param>
    public ChoroplethSeries(GeoJsonDocument? geoData, double[]? values = null) : base(geoData)
    {
        Values = values;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "choropleth",
        GeoJson = GeoData is not null ? GeoJsonWriter.ToJson(GeoData) : null,
        Projection = ProjectionName(Projection),
        Values = Values,
        ColorMapName = ColorMap?.Name,
        VMin = VMin,
        VMax = VMax,
        Color = EdgeColor,
        LineWidth = LineWidth
    };
}
