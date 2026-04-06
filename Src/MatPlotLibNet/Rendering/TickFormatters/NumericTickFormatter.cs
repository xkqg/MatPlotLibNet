// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats tick values as plain numbers, matching the default ChartRenderer behavior.</summary>
public sealed class NumericTickFormatter : ITickFormatter
{
    /// <inheritdoc />
    public string Format(double value)
    {
        if (Math.Abs(value) < 1e-10) return "0";
        if (Math.Abs(value) >= 1e6 || (Math.Abs(value) < 0.01 && value != 0))
            return value.ToString("G3", CultureInfo.InvariantCulture);
        return value.ToString("G5", CultureInfo.InvariantCulture);
    }
}
