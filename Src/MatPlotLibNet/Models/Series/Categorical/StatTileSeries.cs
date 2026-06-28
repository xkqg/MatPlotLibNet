// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A single-value "stat tile": a big formatted headline number with the series
/// <see cref="ChartSeries.Label"/> beneath it, filling its plot area. Built for compact dashboard KPIs
/// ("12 participants", "0 alerts"). It carries no axes/data, so <see cref="ComputeDataRange"/> contributes
/// nothing — the tile occupies the whole region it is placed in (use a mosaic / sub-plot per tile).</summary>
public sealed class StatTileSeries : ChartSeries
{
    /// <summary>The value displayed as the tile's headline number.</summary>
    public double Value { get; }

    /// <summary>An optional accent colour for the headline number (e.g. a warning colour for an alarm count);
    /// the theme cycle colour is used when null.</summary>
    public Color? AccentColor { get; set; }

    /// <summary>The numeric format string applied to <see cref="Value"/> (invariant culture; default <c>"0.##"</c>).</summary>
    public string Format { get; set; } = "0.##";

    /// <summary>Creates a stat tile displaying <paramref name="value"/>.</summary>
    public StatTileSeries(double value) => Value = value;

    /// <summary>The headline value rendered with <see cref="Format"/> under the invariant culture.</summary>
    internal string FormattedValue => Value.ToString(Format, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "stattile",
        GaugeValue = Value,
        Color = AccentColor,
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
