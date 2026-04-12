// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a progress bar showing a value as a fraction of a track (0.0 to 1.0).</summary>
public sealed class ProgressBarSeries : ChartSeries
{
    public double Value { get; }

    public Color FillColor { get; set; } = Colors.Tab10Blue;

    public Color TrackColor { get; set; } = Colors.LightGray;

    public double BarHeight { get; set; } = 0.3;

    /// <summary>Creates a new progress bar series with the given value (0.0 to 1.0).</summary>
    public ProgressBarSeries(double value) => Value = Math.Clamp(value, 0, 1);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "progressbar",
        GaugeValue = Value, FillColor = FillColor,
        TrackColor = TrackColor, BarHeight = BarHeight
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
