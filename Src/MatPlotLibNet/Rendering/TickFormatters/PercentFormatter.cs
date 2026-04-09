// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>
/// Formats tick values as percentages relative to a given maximum.
/// <c>value / max * 100</c> is displayed with a "%" suffix.
/// Equivalent to matplotlib's <c>PercentFormatter</c>.
/// </summary>
public sealed class PercentFormatter : ITickFormatter
{
    private readonly double _max;

    /// <summary>Initialises with the value that represents 100%.</summary>
    public PercentFormatter(double max) => _max = max;

    /// <inheritdoc />
    public string Format(double value)
    {
        double pct = _max == 0 ? 0 : value / _max * 100;
        // Strip unnecessary decimals
        if (pct == Math.Floor(pct))
            return $"{(long)pct}%";
        return $"{pct.ToString("G4", CultureInfo.InvariantCulture)}%";
    }
}
