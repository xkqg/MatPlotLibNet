// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pie chart series displaying proportional data as circular slices.</summary>
public sealed class PieSeries : ChartSeries
{
    /// <summary>Gets the numeric sizes of each pie slice.</summary>
    public double[] Sizes { get; }

    /// <summary>Gets or sets the labels for each pie slice.</summary>
    public string[]? Labels { get; set; }

    /// <summary>Gets or sets the colors for each pie slice.</summary>
    public Color[]? Colors { get; set; }

    /// <summary>Gets or sets the starting angle in degrees for the first slice.</summary>
    public double StartAngle { get; set; } = 90;

    /// <summary>Gets or sets whether slices are drawn counter-clockwise.</summary>
    public bool CounterClockwise { get; set; }

    /// <summary>Gets or sets per-slice radial offsets (0.0–0.3). Null means no explosion.</summary>
    public double[]? Explode { get; set; }

    /// <summary>Gets or sets the percentage format string (e.g. <c>"{0:F1}%"</c>). Null disables percentage labels.</summary>
    public string? AutoPct { get; set; }

    /// <summary>Gets or sets whether a drop-shadow is drawn behind the pie.</summary>
    public bool Shadow { get; set; }

    /// <summary>Gets or sets a custom radius. Null uses the default auto-computed radius.</summary>
    public double? Radius { get; set; }


    /// <summary>Initializes a new instance of <see cref="PieSeries"/> with the specified slice sizes.</summary>
    /// <param name="sizes">The numeric sizes of each pie slice.</param>
    public PieSeries(double[] sizes)
    {
        Sizes = sizes;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "pie",
        Sizes = Sizes, PieLabels = Labels
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
