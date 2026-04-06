// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats tick values for logarithmic axes, showing powers of ten as superscript notation.</summary>
public sealed class LogTickFormatter : ITickFormatter
{
    private static readonly string[] Superscripts = ["\u2070", "\u00B9", "\u00B2", "\u00B3", "\u2074", "\u2075", "\u2076", "\u2077", "\u2078", "\u2079"];

    /// <inheritdoc />
    public string Format(double value)
    {
        if (value <= 0) return "0";

        double log = Math.Log10(value);
        if (Math.Abs(log - Math.Round(log)) < 1e-10)
        {
            int exp = (int)Math.Round(log);
            return $"10{ToSuperscript(exp)}";
        }

        return value.ToString("G4", CultureInfo.InvariantCulture);
    }

    private static string ToSuperscript(int n)
    {
        if (n >= 0 && n <= 9) return Superscripts[n];

        var chars = n.ToString(CultureInfo.InvariantCulture);
        var sb = new System.Text.StringBuilder();
        foreach (var c in chars)
        {
            if (c == '-') sb.Append('\u207B');
            else if (c >= '0' && c <= '9') sb.Append(Superscripts[c - '0']);
        }
        return sb.ToString();
    }
}
