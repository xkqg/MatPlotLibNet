// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Verifies <see cref="NumericTickFormatter"/> behavior.</summary>
public class NumericTickFormatterTests
{
    /// <summary>Verifies that zero formats as "0".</summary>
    [Fact]
    public void Format_Zero_ReturnsZero()
    {
        var formatter = new NumericTickFormatter();
        Assert.Equal("0", formatter.Format(0));
    }

    /// <summary>Verifies that normal values format with G5 precision.</summary>
    [Fact]
    public void Format_NormalValue_ReturnsFormatted()
    {
        var formatter = new NumericTickFormatter();
        Assert.Equal("42.5", formatter.Format(42.5));
    }

    /// <summary>Verifies that large values use scientific notation.</summary>
    [Fact]
    public void Format_LargeValue_UsesScientific()
    {
        var formatter = new NumericTickFormatter();
        string result = formatter.Format(1_500_000);
        Assert.Contains("E", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that the formatter implements ITickFormatter.</summary>
    [Fact]
    public void ImplementsITickFormatter()
    {
        ITickFormatter formatter = new NumericTickFormatter();
        Assert.NotNull(formatter);
    }

    /// <summary>Tiny non-zero values take the G3 branch — covers the
    /// `Math.Abs(value) &lt; 0.01 &amp;&amp; value != 0` arm. We only assert the
    /// formatter returns a non-empty result; the exact text format is the .NET
    /// G3 contract and not what we're testing.</summary>
    [Theory]
    [InlineData(0.005)]
    [InlineData(-0.005)]
    [InlineData(1e-5)]
    [InlineData(-1e-5)]
    public void Format_TinyNonZero_ReturnsNonEmpty(double v)
        => Assert.False(string.IsNullOrEmpty(new NumericTickFormatter().Format(v)));

    /// <summary>Very small values that exceed double's negative exponent go scientific.</summary>
    [Fact]
    public void Format_VerySmall_UsesScientific()
        => Assert.Contains("E", new NumericTickFormatter().Format(1e-7), StringComparison.OrdinalIgnoreCase);

    /// <summary>Negative large values also take the scientific branch.</summary>
    [Fact]
    public void Format_LargeNegative_UsesScientific()
        => Assert.Contains("E", new NumericTickFormatter().Format(-2_500_000), StringComparison.OrdinalIgnoreCase);
}
