// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Provides canonical dash-gap ratio patterns for each <see cref="LineStyle"/>.</summary>
public static class DashPatterns
{
    private static readonly double[] DashedPattern = [8, 4];
    private static readonly double[] DottedPattern = [2, 4];
    private static readonly double[] DashDotPattern = [8, 4, 2, 4];
    private static readonly double[] EmptyPattern = [];

    /// <summary>Returns the dash-gap ratio array for the given line style.</summary>
    public static ReadOnlySpan<double> GetPattern(LineStyle style) => style switch
    {
        LineStyle.Dashed => DashedPattern,
        LineStyle.Dotted => DottedPattern,
        LineStyle.DashDot => DashDotPattern,
        _ => EmptyPattern
    };
}
