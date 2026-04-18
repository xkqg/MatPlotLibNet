// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Edge-case coverage for <see cref="PercentFormatter"/>: pushes branch
/// coverage from 50% to 100% by exercising the <c>_max == 0</c> guard and the
/// floor vs G4-decimal arms of <see cref="PercentFormatter.Format"/>.</summary>
public class PercentFormatterEdgeCaseTests
{
    [Fact]
    public void Format_MaxIsZero_ReturnsZeroPercent()
    {
        // Hits the `_max == 0 → 0` branch: 5/0 would be NaN but the guard returns 0.
        var f = new PercentFormatter(0.0);
        Assert.Equal("0%", f.Format(5));
        Assert.Equal("0%", f.Format(-5));
        Assert.Equal("0%", f.Format(0));
    }

    [Theory]
    [InlineData(33.333, 100, "33.33%")]   // non-integer → G4 branch (4 sig figs)
    [InlineData(12.5,   100, "12.5%")]
    [InlineData(0.1,    1.0, "10%")]      // 0.1/1 *100 = 10 → integer → floor branch
    [InlineData(0.125,  1.0, "12.5%")]    // 12.5 → G4 branch
    public void Format_FloorVsG4_Branches(double value, double max, string expected)
        => Assert.Equal(expected, new PercentFormatter(max).Format(value));

    [Fact]
    public void Format_NegativePercent_KeepsSign()
    {
        // -25/100*100 = -25 → integer → floor cast handles long with sign
        var f = new PercentFormatter(100.0);
        Assert.Equal("-25%", f.Format(-25));
    }
}
