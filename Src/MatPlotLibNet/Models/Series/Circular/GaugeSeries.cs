// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a gauge (speedometer) chart with a semi-circular dial, needle, and colored range bands.</summary>
public sealed class GaugeSeries : ChartSeries
{
    public double Value { get; }

    public double Min { get; set; }

    public double Max { get; set; } = 100;

    public (double Threshold, Color Color)[]? Ranges { get; set; }

    public Color NeedleColor { get; set; } = Colors.Black;

    /// <summary>Creates a new gauge series displaying the given value.</summary>
    public GaugeSeries(double value) => Value = value;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "gauge",
        GaugeValue = Value, GaugeMin = Min, GaugeMax = Max,
        NeedleColor = NeedleColor
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
