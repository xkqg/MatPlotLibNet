// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a progress bar showing a value as a fraction of a track (0.0 to 1.0).</summary>
public sealed class ProgressBarSeries : ChartSeries, IHasDataRange
{
    /// <summary>Gets the progress value (0.0 = empty, 1.0 = full).</summary>
    public double Value { get; }

    /// <summary>Gets or sets the fill color of the progress portion.</summary>
    public Color FillColor { get; set; } = Color.FromHex("#1f77b4");

    /// <summary>Gets or sets the track (background) color.</summary>
    public Color TrackColor { get; set; } = Color.LightGray;

    /// <summary>Gets or sets the bar height as a fraction of the available plot height.</summary>
    public double BarHeight { get; set; } = 0.3;

    /// <summary>Creates a new progress bar series with the given value (0.0 to 1.0).</summary>
    public ProgressBarSeries(double value) => Value = Math.Clamp(value, 0, 1);

    /// <inheritdoc />
    public DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
