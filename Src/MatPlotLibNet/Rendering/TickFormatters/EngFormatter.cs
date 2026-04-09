// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>
/// Formats tick values using SI engineering prefixes (k, M, G, T for large values;
/// m, µ, n for small values). Equivalent to matplotlib's <c>EngFormatter</c>.
/// </summary>
public sealed class EngFormatter : ITickFormatter
{
    private static readonly (double Factor, string Prefix)[] Prefixes =
    [
        (1e12, "T"),
        (1e9,  "G"),
        (1e6,  "M"),
        (1e3,  "k"),
        (1e0,  ""),
        (1e-3, "m"),
        (1e-6, "µ"),
        (1e-9, "n"),
    ];

    /// <inheritdoc />
    public string Format(double value)
    {
        if (Math.Abs(value) < 1e-15) return "0";

        bool negative = value < 0;
        double abs = Math.Abs(value);

        foreach (var (factor, prefix) in Prefixes)
        {
            if (abs >= factor)
            {
                double scaled = abs / factor;
                string number = FormatScaled(scaled);
                return negative ? $"-{number}{prefix}" : $"{number}{prefix}";
            }
        }

        // Fallback for very small values (< 1e-9)
        return value.ToString("G3", CultureInfo.InvariantCulture);
    }

    private static string FormatScaled(double scaled)
    {
        // Use up to 3 significant figures, strip trailing zeros
        if (scaled == Math.Floor(scaled))
            return ((long)scaled).ToString(CultureInfo.InvariantCulture);

        string s = scaled.ToString("G3", CultureInfo.InvariantCulture);
        return s;
    }
}
