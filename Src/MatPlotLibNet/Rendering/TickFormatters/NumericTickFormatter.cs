// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats tick values as plain numbers, matching the default ChartRenderer behavior.</summary>
public sealed class NumericTickFormatter : ITickFormatter, IJsonDescribed<TickFormatterDto>
{
    /// <inheritdoc />
    TickFormatterDto IJsonDescribed<TickFormatterDto>.Describe() => new NumericTickFormatterDto();

    /// <inheritdoc />
    public string Format(double value) => FormatValue(value);

    /// <summary>The one plain number format of a tick, which the formatter writes and an axis falls back to when its ticks
    /// have no step to measure: "0" for a value within a ten-billionth of 0, three significant digits below a hundredth and
    /// from a million up, five otherwise.</summary>
    /// <param name="value">The tick value.</param>
    /// <returns>The value written.</returns>
    internal static string FormatValue(double value)
    {
        if (Math.Abs(value) < 1e-10) return "0";
        if (Math.Abs(value) >= 1e6 || (Math.Abs(value) < 0.01 && value != 0))
            return value.InvariantText("G3");
        return value.InvariantText("G5");
    }
}
