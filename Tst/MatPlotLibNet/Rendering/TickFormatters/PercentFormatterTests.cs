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

    /// <summary>Verifies 0..1 scale: half = 50%.</summary>
    [Fact]
    public void Format_HalfOfOne_Returns50Percent()
    {
        var formatter = new PercentFormatter(1.0);
        Assert.Equal("50%", formatter.Format(0.5));
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

    /// <summary>Verifies that zero always formats as 0%.</summary>
    [Fact]
    public void Format_Zero_ReturnsZeroPercent()
    {
        var formatter = new PercentFormatter(100.0);
        Assert.Equal("0%", formatter.Format(0));
    }

    /// <summary>Verifies 0..100 scale: 25 = 25%.</summary>
    [Fact]
    public void Format_25Of100_Returns25Percent()
    {
        var formatter = new PercentFormatter(100.0);
        Assert.Equal("25%", formatter.Format(25));
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
}
