// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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
}
