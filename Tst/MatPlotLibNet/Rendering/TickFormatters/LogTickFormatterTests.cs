// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Verifies <see cref="LogTickFormatter"/> behavior.</summary>
public class LogTickFormatterTests
{
    /// <summary>Verifies that powers of ten show as exponent notation.</summary>
    [Theory]
    [InlineData(1, "10\u2070")]
    [InlineData(10, "10\u00B9")]
    [InlineData(100, "10\u00B2")]
    [InlineData(1000, "10\u00B3")]
    public void Format_PowersOfTen_ShowsSuperscript(double value, string expected)
    {
        var formatter = new LogTickFormatter();
        Assert.Equal(expected, formatter.Format(value));
    }

    /// <summary>A value off a power of ten is written as matplotlib's <c>LogFormatterSciNotation</c> writes it (<c>_non_decade_format</c>:
    /// the coefficient, times, the base to the power at or below).</summary>
    [Fact]
    public void Format_NonPowerOfTen_ShowsTheCoefficientTimesThePower()
    {
        var formatter = new LogTickFormatter();
        string result = formatter.Format(50);
        Assert.Equal("5×10¹", result);
    }

    /// <summary>Verifies that the formatter implements ITickFormatter.</summary>
    [Fact]
    public void ImplementsITickFormatter()
    {
        ITickFormatter formatter = new LogTickFormatter();
        Assert.NotNull(formatter);
    }

    [Theory]
    [InlineData(0,    "0")]               // LogFormatterMathtext: x == 0 → '0' (a symmetric-log axis' zero)
    [InlineData(-5,   "−5×10⁰")]     // below zero: the sign, then the label of abs(x), the minus mathtext draws
    [InlineData(-100, "−10²")]
    public void Format_ZeroAndBelow_AsLogFormatterSciNotation(double value, string expected)
        => Assert.Equal(expected, new LogTickFormatter().Format(value));

    [Theory]
    // Single-digit positive exponents (covered already, sanity)
    [InlineData(NumpyReference.Log10.X1,     1.0,    "10\u2070")]   // 10^0
    [InlineData(NumpyReference.Log10.X10,    10.0,   "10\u00B9")]
    [InlineData(NumpyReference.Log10.X100,   100.0,  "10\u00B2")]
    [InlineData(NumpyReference.Log10.X1000,  1000.0, "10\u00B3")]
    public void Format_SingleDigitExponent_UsesSuperscript(double _, double value, string expected)
        => Assert.Equal(expected, new LogTickFormatter().Format(value));

    [Theory]
    // Multi-digit positive exponent → loops the ToSuperscript char-by-char branch
    [InlineData(1e10,  "10\u00B9\u2070")]    // 10¹⁰
    [InlineData(1e15,  "10\u00B9\u2075")]    // 10¹⁵
    [InlineData(1e20,  "10\u00B2\u2070")]    // 10²⁰
    public void Format_LargeExponent_BuildsMultiCharSuperscript(double value, string expected)
        => Assert.Equal(expected, new LogTickFormatter().Format(value));

    [Theory]
    // Negative exponent — exercises the '-' branch in ToSuperscript
    [InlineData(0.1,   "10\u207B\u00B9")]    // 10⁻¹
    [InlineData(0.01,  "10\u207B\u00B2")]    // 10⁻²
    [InlineData(0.001, "10\u207B\u00B3")]    // 10⁻³
    public void Format_NegativeExponent_UsesMinusSuperscript(double value, string expected)
        => Assert.Equal(expected, new LogTickFormatter().Format(value));

    [Theory]
    [InlineData(50,   "5×10¹")]      // off a power of ten → coefficient × power, as LogFormatterSciNotation
    [InlineData(2.5,  "2.5×10⁰")]
    [InlineData(1.234, "1.234×10⁰")]
    public void Format_NonPowerOfTen_WritesTheCoefficientTimesThePower(double value, string expected)
        => Assert.Equal(expected, new LogTickFormatter().Format(value));
}
