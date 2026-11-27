// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="DashPatterns"/> behavior.</summary>
public class DashPatternTests
{
    /// <summary>Verifies that the Solid and None line styles return an empty dash pattern, and that the other styles return the correct
    /// dash-gap ratios. Patterns are calibrated to match matplotlib defaults at ~96 dpi:
    /// '--' ≈ 3.7pt/1.6pt → 5/2px; ':' ≈ 1pt/3pt → 1/3px; '-.' ≈ 5/2/1/2px.</summary>
    [Theory]
    [InlineData(LineStyle.Solid,   new double[] { })]
    [InlineData(LineStyle.None,    new double[] { })]
    [InlineData(LineStyle.Dashed,  new double[] { 5, 2 })]
    [InlineData(LineStyle.Dotted,  new double[] { 1, 3 })]
    [InlineData(LineStyle.DashDot, new double[] { 5, 2, 1, 2 })]
    public void ALineStyle_ReturnsItsDashGapRatios(LineStyle style, double[] expected)
        => Assert.Equal(expected, DashPatterns.GetPattern(style).ToArray());
}
