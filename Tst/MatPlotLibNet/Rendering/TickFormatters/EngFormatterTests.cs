// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Verifies <see cref="EngFormatter"/> behavior.</summary>
public class EngFormatterTests
{
    /// <summary>Verifies that the formatter implements ITickFormatter.</summary>
    [Fact]
    public void ImplementsITickFormatter()
    {
        ITickFormatter formatter = new EngFormatter();
        Assert.NotNull(formatter);
    }

    /// <summary>Verifies SI prefix mapping for common engineering values. Default <see cref="EngFormatter.Sep"/>
    /// is a single space (matching matplotlib), so the expected outputs include a space before the prefix.</summary>
    [Theory]
    [InlineData(1e12, "1 T")]
    [InlineData(1e9, "1 G")]
    [InlineData(1e6, "1 M")]
    [InlineData(1e3, "1 k")]
    [InlineData(1.0, "1")]
    [InlineData(1e-3, "1 m")]
    [InlineData(1e-6, "1 µ")]
    [InlineData(1e-9, "1 n")]
    public void Format_SiPrefixes(double value, string expected)
    {
        var formatter = new EngFormatter();
        Assert.Equal(expected, formatter.Format(value));
    }

    /// <summary>Verifies that zero formats as "0".</summary>
    [Fact]
    public void Format_Zero_ReturnsZero()
    {
        var formatter = new EngFormatter();
        Assert.Equal("0", formatter.Format(0));
    }

    /// <summary>Verifies that 1500 formats as "1.5 k" (matplotlib default separator).</summary>
    [Fact]
    public void Format_15Hundred_Returns1Point5k()
    {
        var formatter = new EngFormatter();
        Assert.Equal("1.5 k", formatter.Format(1500));
    }

    /// <summary>Verifies that setting <see cref="EngFormatter.Sep"/> to empty gives the compact form.</summary>
    [Fact]
    public void Format_EmptySep_CompactForm()
    {
        var formatter = new EngFormatter { Sep = "" };
        Assert.Equal("1.5k", formatter.Format(1500));
        Assert.Equal("30k", formatter.Format(30_000));
    }

    /// <summary>Verifies that 1,234,567 formats with M suffix.</summary>
    [Fact]
    public void Format_MillionRange_UsesMSuffix()
    {
        var formatter = new EngFormatter();
        string result = formatter.Format(1_234_567);
        Assert.Contains("M", result);
    }

    /// <summary>Verifies that negative values keep the minus sign.</summary>
    [Fact]
    public void Format_NegativeValue_KeepsMinusSign()
    {
        var formatter = new EngFormatter();
        string result = formatter.Format(-5000);
        Assert.StartsWith("-", result);
        Assert.Contains("k", result);
    }

    /// <summary>Verifies that values between -1 and 1 (non-zero) use m or µ.</summary>
    [Fact]
    public void Format_SmallValue_UsesSubUnitPrefix()
    {
        var formatter = new EngFormatter();
        string result = formatter.Format(0.005);
        Assert.Contains("m", result);
    }
}
