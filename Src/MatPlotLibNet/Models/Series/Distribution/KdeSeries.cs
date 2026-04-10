// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a kernel density estimation (KDE) series that draws a smooth density curve for a data sample.</summary>
public sealed class KdeSeries : ChartSeries
{
    /// <summary>Gets the data values used to estimate the density.</summary>
    public double[] Data { get; }

    /// <summary>Gets or sets the bandwidth for the Gaussian kernel. If <see langword="null"/>, Silverman's rule-of-thumb is used automatically.</summary>
    public double? Bandwidth { get; set; }

    /// <summary>Gets or sets whether to fill the area under the density curve.</summary>
    public bool Fill { get; set; } = true;

    /// <summary>Gets or sets the opacity of the fill area (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 0.3;

    /// <summary>Gets or sets the width of the density curve line in pixels.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Gets or sets the color of the series. If <see langword="null"/>, the current cycle color is used.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the dash style of the density curve line.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Initializes a new instance of <see cref="KdeSeries"/> with the specified data.</summary>
    /// <param name="data">The data values used to estimate the density.</param>
    public KdeSeries(double[] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Data.Length == 0) return new(0, 1, 0, 1);
        double min = Data.Min(), max = Data.Max();
        double range = max - min;
        if (range == 0) range = 1.0;
        double padding = range * 0.3;
        return new(min - padding, max + padding, 0, null);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "kde",
        Data = Data,
        Bandwidth = Bandwidth,
        Alpha = Alpha,
        LineWidth = LineWidth,
        Color = Color,
        LineStyle = LineStyle == LineStyle.Solid ? null : LineStyle.ToString().ToLowerInvariant()
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
