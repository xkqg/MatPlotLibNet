// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Provides canonical dash-gap ratio patterns for each <see cref="LineStyle"/>.</summary>
public static class DashPatterns
{
    // Patterns calibrated to match matplotlib's default dash styles at ~96 dpi.
    // matplotlib '--': 3.7pt on / 1.6pt off ≈ 5px/2px at 96 dpi.
    // matplotlib ':' : 1pt   on / 3pt   off ≈ 1px/4px.
    // matplotlib '-.': 5pt on / 2pt off / 1pt on / 2pt off ≈ 5/2/1/2.
    private static readonly double[] DashedPattern  = [5, 2];
    private static readonly double[] DottedPattern  = [1, 3];
    private static readonly double[] DashDotPattern = [5, 2, 1, 2];
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
