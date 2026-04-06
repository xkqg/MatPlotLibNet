// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a radar (spider) chart series displaying multi-axis categorical data.</summary>
public sealed class RadarSeries : ChartSeries, IHasDataRange, ISeriesSerializable
{
    /// <summary>Gets the category labels for each axis.</summary>
    public string[] Categories { get; }

    /// <summary>Gets the data values for each axis.</summary>
    public double[] Values { get; }

    /// <summary>Gets or sets the line color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the fill color. When null, uses <see cref="Color"/> with <see cref="Alpha"/>.</summary>
    public Color? FillColor { get; set; }

    /// <summary>Gets or sets the fill opacity.</summary>
    public double Alpha { get; set; } = 0.25;

    /// <summary>Gets or sets the line width.</summary>
    public double LineWidth { get; set; } = 2.0;

    /// <summary>Gets or sets the optional explicit maximum value for normalization. When null, auto-computed from data.</summary>
    public double? MaxValue { get; set; }

    /// <summary>Creates a new radar series from the given categories and values.</summary>
    public RadarSeries(string[] categories, double[] values)
    {
        Categories = categories;
        Values = values;
    }

    /// <inheritdoc />
    public DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public SeriesDto ToSeriesDto() => new()
    {
        Type = "radar",
        Categories = Categories, Values = Values,
        Color = Color, FillColor = FillColor,
        Alpha = Alpha, LineWidth = LineWidth, MaxValue = MaxValue
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
