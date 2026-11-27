// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Verifies <see cref="NumericTickFormatter"/> behavior.</summary>
public class NumericTickFormatterTests
{
    /// <summary>Verifies that zero formats as "0", by the numeric formatter and by the engineering one.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Format_Zero_ReturnsZero(bool numeric)
    {
        ITickFormatter formatter = numeric ? new NumericTickFormatter() : new EngFormatter();
        Assert.Equal("0", formatter.Format(0));
    }

    /// <summary>Verifies that normal values format with G5 precision.</summary>
    [Fact]
    public void Format_NormalValue_ReturnsFormatted()
    {
        var formatter = new NumericTickFormatter();
        Assert.Equal("42.5", formatter.Format(42.5));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that large values use scientific notation.
        new DrawnToken("Format_LargeValue_UsesScientific", () =>
        {
            var formatter = new NumericTickFormatter();
            return formatter.Format(1_500_000);
        }, "E") { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

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

    /// <summary>Very small values that exceed double's negative exponent go scientific, and so do negative large ones.</summary>
    [Theory]
    [InlineData(1e-7)]
    [InlineData(-2_500_000)]
    public void Format_VerySmallOrLargeNegative_UsesScientific(double value)
        => Assert.Contains("E", new NumericTickFormatter().Format(value), StringComparison.OrdinalIgnoreCase);

    /// <summary>The one plain number format of a tick writes what the axes renderer's own tick format wrote, on both sides of
    /// every edge it has: zero below a ten-billionth, three significant digits below a hundredth and from a million up,
    /// five otherwise.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(5e-11)]
    [InlineData(-5e-11)]
    [InlineData(1e-10)]
    [InlineData(0.0099)]
    [InlineData(-0.0099)]
    [InlineData(0.01)]
    [InlineData(42.5)]
    [InlineData(123456.789)]
    [InlineData(999999.5)]
    [InlineData(1e6)]
    [InlineData(-1e6)]
    public void FormatValue_WritesWhatTheAxesTickFormatWrote(double value)
    {
        Assert.Equal(OldAxesTickFormat(value), NumericTickFormatter.FormatValue(value));
        Assert.Equal(NumericTickFormatter.FormatValue(value), new NumericTickFormatter().Format(value));
    }

    /// <summary>The body the axes renderer formatted a tick with before the formatter held it, kept here as the oracle.</summary>
    private static string OldAxesTickFormat(double value)
    {
        if (Math.Abs(value) < 1e-10) return "0";
        if (Math.Abs(value) >= 1e6 || (Math.Abs(value) < 0.01 && value != 0))
            return value.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
        return value.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    }
}
