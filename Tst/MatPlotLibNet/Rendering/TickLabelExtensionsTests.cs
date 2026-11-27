// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one number format of a set of ticks, <see cref="TickLabelExtensions"/>: every tick of an axis written with
/// the decimals its step needs, as matplotlib's <c>ScalarFormatter</c> writes them.</summary>
public class TickLabelExtensionsTests
{
    /// <summary>The fixed formats are the ones the step's decimals name, written once instead of per axis per draw.</summary>
    [Fact]
    public void TheFixedFormats_AreF0ToF10()
    {
        Assert.Equal(11, TickLabelExtensions.FixedFormats.Length);
        for (int d = 0; d <= 10; d++) Assert.Equal($"F{d}", TickLabelExtensions.FixedFormats[d]);
    }

    /// <summary>One tick has no step to measure: it is written in the plain number format.</summary>
    [Fact]
    public void UniformFormat_OfOneTick_IsThePlainNumberFormat()
    {
        var format = new[] { 5.0 }.UniformFormat();
        Assert.Equal("5", format(5.0));
        Assert.Equal(NumericTickFormatter.FormatValue(1234.5678), format(1234.5678));
    }

    /// <summary>No ticks at all are written in the plain number format too.</summary>
    [Fact]
    public void UniformFormat_OfNoTicks_IsThePlainNumberFormat()
    {
        double[] none = null!;
        Assert.Equal("0", none.UniformFormat()(0.0));
    }

    /// <summary>Two equal ticks step by 0, which needs no decimal.</summary>
    [Fact]
    public void UniformFormat_OfAZeroStep_WritesNoDecimal()
    {
        Assert.Equal("2", new[] { 2.0, 2.0 }.UniformFormat()(2.0));
    }

    /// <summary>Every tick is written with the decimals the step needs, and a value a rounding error from 0 is written as 0
    /// (no "-0.0"). A step below a billionth already counts as whole, so it is written with no decimal.</summary>
    [Theory]
    [InlineData(new[] { 0.0, 0.5, 1.0 }, 1.0, "1.0")]
    [InlineData(new[] { 0.0, 0.25, 0.5 }, 0.5, "0.50")]
    [InlineData(new[] { 0.0, 2.0, 4.0 }, 4.0, "4")]
    [InlineData(new[] { -0.5, 0.0, 0.5 }, -1e-12, "0.0")]
    [InlineData(new[] { 1e-10, 2e-10 }, 1e-10, "0")]
    public void UniformFormat_WritesTheDecimalsOfTheStep(double[] ticks, double value, string expected)
    {
        Assert.Equal(expected, ticks.UniformFormat()(value));
    }

    /// <summary>A step no power of ten up to 10^10 makes whole (a third), or a step that is no number, is written with no
    /// decimal.</summary>
    [Theory]
    [InlineData(1.0 / 3)]
    [InlineData(double.PositiveInfinity)]
    public void UniformFormat_OfAStepWithNoShortDecimal_WritesNoDecimal(double step)
    {
        Assert.Equal("1", new[] { 0.0, step }.UniformFormat()(1.0));
    }

    /// <summary>The labels of a set of ticks are the ticks in that one format.</summary>
    [Fact]
    public void Labelled_WritesEveryTickInTheOneFormat()
    {
        Assert.Equal(["0.0", "0.5", "1.0"], TickLabelLook.Library.Labelled([0.0, 0.5, 1.0]));
    }
}
