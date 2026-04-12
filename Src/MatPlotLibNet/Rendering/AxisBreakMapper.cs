// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>Helper that maps data coordinates to compressed coordinates when one or more
/// <see cref="AxisBreak"/> regions are active.</summary>
/// <remarks>All methods are O(B) where B is the number of breaks — typically very small.</remarks>
internal static class AxisBreakMapper
{
    /// <summary>Returns <c>true</c> when <paramref name="value"/> falls strictly inside
    /// any break in <paramref name="breaks"/> (i.e., From &lt; value &lt; To).</summary>
    /// <param name="breaks">List of axis breaks to test against.</param>
    /// <param name="value">Data-space value to test.</param>
    public static bool IsInBreak(IReadOnlyList<AxisBreak> breaks, double value)
    {
        foreach (var b in breaks)
            if (value > b.From && value < b.To) return true;
        return false;
    }

    /// <summary>Returns the compressed [min, max] after removing all break gaps from
    /// [<paramref name="fullMin"/>, <paramref name="fullMax"/>].</summary>
    /// <param name="breaks">List of axis breaks whose gap widths are to be removed.</param>
    /// <param name="fullMin">Minimum of the full (uncompressed) data range.</param>
    /// <param name="fullMax">Maximum of the full (uncompressed) data range.</param>
    /// <returns>Tuple (<c>fullMin</c>, <c>fullMax − totalGap</c>) suitable for <see cref="DataTransform"/>.</returns>
    public static (double Min, double Max) CompressedRange(
        IReadOnlyList<AxisBreak> breaks, double fullMin, double fullMax)
    {
        double totalGap = 0;
        foreach (var b in breaks)
        {
            double lo = Math.Max(b.From, fullMin);
            double hi = Math.Min(b.To, fullMax);
            if (hi > lo) totalGap += hi - lo;
        }
        return (fullMin, fullMax - totalGap);
    }

    /// <summary>Maps <paramref name="value"/> from data space to compressed coordinate space.
    /// Returns <see cref="double.NaN"/> when the value is inside a break region.</summary>
    /// <param name="breaks">List of axis breaks defining hidden regions.</param>
    /// <param name="value">Data-space value to remap.</param>
    /// <param name="fullMin">Minimum of the full data range (used to clip break boundaries).</param>
    /// <param name="fullMax">Maximum of the full data range (used to clip break boundaries).</param>
    /// <returns>Compressed data-space coordinate, or <see cref="double.NaN"/> if inside a break.</returns>
    public static double Remap(IReadOnlyList<AxisBreak> breaks, double value, double fullMin, double fullMax)
    {
        if (IsInBreak(breaks, value)) return double.NaN;

        double shift = 0;
        // Sort breaks by From so we apply shifts in order
        foreach (var b in breaks.OrderBy(b => b.From))
        {
            if (b.To <= value)
            {
                // Break is entirely to the left of value — add its gap to the shift
                double lo = Math.Max(b.From, fullMin);
                double hi = Math.Min(b.To, fullMax);
                if (hi > lo) shift += hi - lo;
            }
        }
        return value - shift;
    }
}
