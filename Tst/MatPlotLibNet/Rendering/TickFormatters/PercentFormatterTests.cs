// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Verifies <see cref="PercentFormatter"/> behavior.</summary>
public class PercentFormatterTests
{
    /// <summary>Verifies that the formatter implements ITickFormatter.</summary>
    [Fact]
    public void ImplementsITickFormatter()
    {
        ITickFormatter formatter = new PercentFormatter(1.0);
        Assert.NotNull(formatter);
    }

    [Theory]
    [InlineData(1.0, "50%", 0.5)] // Format_HalfOfOne_Returns50Percent
    [InlineData(100.0, "0%", 0)] // Format_Zero_ReturnsZeroPercent
    [InlineData(100.0, "25%", 25)] // Format_25Of100_Returns25Percent
    public void Format_WritesTheFractionOfTheMaximumAsAPercent(double maximum, string expected, double value)
    {
        var formatter = new PercentFormatter(maximum);
        Assert.Equal(expected, formatter.Format(value));
    }

    /// <summary>Verifies that the maximum value formats as 100%.</summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(100.0)]
    [InlineData(200.0)]
    public void Format_MaxValue_Returns100Percent(double max)
    {
        var formatter = new PercentFormatter(max);
        Assert.Equal("100%", formatter.Format(max));
    }

    /// <summary>Verifies that the result always ends with %.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void Format_AlwaysEndsWithPercent(double value)
    {
        var formatter = new PercentFormatter(100.0);
        Assert.EndsWith("%", formatter.Format(value));
    }

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
