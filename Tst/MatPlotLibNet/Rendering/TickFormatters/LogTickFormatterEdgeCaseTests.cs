// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Edge-case coverage for <see cref="LogTickFormatter"/>: pushes
/// 57/31 → ≥90/90 by exercising the value≤0 branch, the multi-digit superscript
/// fallback (<c>10¹⁰</c>, <c>10²⁰</c>), the negative-exponent path (<c>10⁻¹</c>)
/// and the non-power-of-10 G4 fallback.</summary>
public class LogTickFormatterEdgeCaseTests
{
    [Theory]
    [InlineData(0,    "0")]    // value <= 0 → "0"
    [InlineData(-5,   "0")]    // negative → "0"
    [InlineData(-100, "0")]
    public void Format_NonPositive_ReturnsZero(double value, string expected)
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
    [InlineData(50,   "50")]      // non-power-of-ten → G4 fallback
    [InlineData(2.5,  "2.5")]
    [InlineData(1.234, "1.234")]
    public void Format_NonPowerOfTen_UsesG4Fallback(double value, string expected)
        => Assert.Equal(expected, new LogTickFormatter().Format(value));
}
