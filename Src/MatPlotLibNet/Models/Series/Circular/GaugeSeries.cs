// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a gauge (speedometer) chart with a semi-circular dial, needle, and colored range bands.</summary>
public sealed class GaugeSeries : ChartSeries
{
    /// <summary>Gets the current value displayed by the needle.</summary>
    public double Value { get; }

    /// <summary>Gets or sets the minimum scale value.</summary>
    public double Min { get; set; }

    /// <summary>Gets or sets the maximum scale value.</summary>
    public double Max { get; set; } = 100;

    /// <summary>Gets or sets the color range bands as (threshold, color) pairs.</summary>
    /// <remarks>Defaults to green (0-60), yellow (60-80), red (80-100) when null.</remarks>
    public (double Threshold, Color Color)[]? Ranges { get; set; }

    /// <summary>Gets or sets the needle color.</summary>
    public Color NeedleColor { get; set; } = Color.Black;

    /// <summary>Creates a new gauge series displaying the given value.</summary>
    public GaugeSeries(double value) => Value = value;

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
