// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="DashPatterns"/> behavior.</summary>
public class DashPatternTests
{
    /// <summary>Verifies that Solid line style returns an empty dash pattern.</summary>
    [Fact]
    public void Solid_ReturnsEmptyArray()
        => Assert.Empty(DashPatterns.GetPattern(LineStyle.Solid).ToArray());

    /// <summary>Verifies that None line style returns an empty dash pattern.</summary>
    [Fact]
    public void None_ReturnsEmptyArray()
        => Assert.Empty(DashPatterns.GetPattern(LineStyle.None).ToArray());

    /// <summary>Verifies that non-solid line styles return the correct dash-gap ratios.</summary>
    [Theory]
    [InlineData(LineStyle.Dashed, new double[] { 8, 4 })]
    [InlineData(LineStyle.Dotted, new double[] { 2, 4 })]
    [InlineData(LineStyle.DashDot, new double[] { 8, 4, 2, 4 })]
    public void NonSolid_ReturnsCorrectRatios(LineStyle style, double[] expected)
        => Assert.Equal(expected, DashPatterns.GetPattern(style).ToArray());
}
