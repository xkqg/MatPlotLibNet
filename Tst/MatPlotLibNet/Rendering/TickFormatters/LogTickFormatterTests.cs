// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

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

    /// <summary>Verifies that non-power-of-ten values format as plain numbers.</summary>
    [Fact]
    public void Format_NonPowerOfTen_ShowsPlainNumber()
    {
        var formatter = new LogTickFormatter();
        string result = formatter.Format(50);
        Assert.Equal("50", result);
    }

    /// <summary>Verifies that the formatter implements ITickFormatter.</summary>
    [Fact]
    public void ImplementsITickFormatter()
    {
        ITickFormatter formatter = new LogTickFormatter();
        Assert.NotNull(formatter);
    }
}
