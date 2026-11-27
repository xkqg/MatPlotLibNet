// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>Edge-case coverage for <see cref="LinearColorMap"/>: pushes branch
/// coverage from 83.3% to ≥90% by hitting the empty-name guard, value-clamping
/// (below 0 / above 1), and the binary-search bracketing in <c>FromPositions</c>.</summary>
public class LinearColorMapTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromList_ABlankName_Throws(string name)
    {
        var c = new Color(0, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            LinearColorMap.FromList(name, [new(0.0, c), new(1.0, c)]));
    }

    [Theory]
    [InlineData(-100, true)]
    [InlineData(100, false)]
    public void GetColor_OutsideZeroToOne_ClampsToTheNearestStop(double value, bool first)
    {
        var low = new Color(255, 0, 0);
        var high = new Color(0, 0, 255);
        var map = new LinearColorMap("test_clamp", new[] { low, high });
        Assert.Equal(first ? low : high, map.GetColor(value));
    }

    [Fact]
    public void GetColor_FromPositions_BinarySearchHitsMiddleStop()
    {
        // Three stops with explicit positions force the binary-search branch
        var red    = new Color(255, 0, 0);
        var green  = new Color(0, 255, 0);
        var blue   = new Color(0, 0, 255);
        var map = LinearColorMap.FromPositions("bs_test",
            [new(0.0, red), new(0.5, green), new(1.0, blue)]);

        // Values that fall in each bracket
        Assert.Equal(red, map.GetColor(0.0));
        Assert.Equal(green, map.GetColor(0.5));
        Assert.Equal(blue, map.GetColor(1.0));
        // mid-bracket interpolation
        Color mid = map.GetColor(0.25); // halfway between red and green
        Assert.True(mid.R > 0 && mid.G > 0);
    }
}
