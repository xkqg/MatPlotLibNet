// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A gauge (speedometer) chart: a semi-circular dial with coloured range bands and a
/// value needle. The dial is divided into bands by <see cref="Ranges"/>; the needle points at
/// <see cref="Value"/> within the <c>[Min, Max]</c> range.</summary>
public sealed class GaugeSeries : ChartSeries
{
    /// <summary>Current value indicated by the needle, within <c>[Min, Max]</c>.</summary>
    public double Value { get; }

    /// <summary>Lower bound of the dial. Defaults to <c>0</c>.</summary>
    public double Min { get; set; }

    /// <summary>Upper bound of the dial. Defaults to <c>100</c>.</summary>
    public double Max { get; set; } = 100;

    /// <summary>Optional coloured bands across the dial. Each <see cref="GaugeBand"/> defines the
    /// upper-bound threshold (in value space) and fill colour of one arc segment. Bands share
    /// endpoints: band <c>i</c>'s arc spans <c>[prevThreshold, bands[i].Threshold]</c>. Pass
    /// <see langword="null"/> (default) to use the built-in green/amber/red 60/80/100 bands.</summary>
    public GaugeBand[]? Ranges { get; set; }

    /// <summary>Needle stroke colour. Defaults to <see cref="Colors.Black"/>.</summary>
    public Color NeedleColor { get; set; } = Colors.Black;

    /// <summary>Creates a new gauge series displaying <paramref name="value"/> with the default
    /// <c>[0, 100]</c> range and black needle.</summary>
    /// <param name="value">Value pointed at by the needle. Clamped to <c>[Min, Max]</c> at render time.</param>
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
