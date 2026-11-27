// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The owner of what a colour bar names (<see cref="ColorBarScale"/>), the number of ticks a bar of a length asks
/// for (a ruler's <see cref="TickRequest"/>) and the range a bar covers.</summary>
public class ColorBarScaleTests
{
    /// <summary>The scale of a bar under the linear normaliser, the way every test below states a bar: the range, how long it
    /// is, the size of the tick font and its orientation.</summary>
    private static ColorBarScale Linear(double min, double max, double lengthPx, double fontPx, bool vertical) =>
        ColorBarScale.For(min, max, lengthPx, vertical, LinearNormalizer.Instance, new SvgRenderContext(), new Font { Size = fontPx }, Theme.Default);

    /// <summary>The room a tick label takes along its bar, as the thinning of the ticks counts it: its measured width along a
    /// horizontal bar, the size of its font up a vertical one (a label stacked up a bar takes a line of its font and is never
    /// measured). A rule of the colour bar, not a pick of the X or the Y member of a pair.</summary>
    [Fact]
    public void ATickLabel_TakesItsWidthAlongAHorizontalBar_AndItsFontSizeUpAVerticalOne()
    {
        var ctx = new SvgRenderContext();
        var font = new Font { Size = 11 };

        Assert.Equal(ctx.ExtentOf(TextLabel.Plain("123.5"), font).Width, ColorBarScale.LabelRoomAlong(vertical: false, "123.5", font, ctx));
        Assert.Equal(11, ColorBarScale.LabelRoomAlong(vertical: true, "123.5", font, ctx));
    }

    // ---- the number of ticks by the length of the bar ------------------------------------------------------------------

    [Theory]
    [InlineData(300.0, 13.0, false, 9)]     // 300 / 26 = 11.5: held at the most
    [InlineData(400.0, 13.0, true, 9)]     // 400 / 39 = 10.3
    [InlineData(130.0, 13.0, false, 5)]     // 130 / 26
    [InlineData(45.0, 13.0, false, 2)]      // 45 / 26 = 1.7: held at the fewest
    [InlineData(0.0, 13.0, false, 2)]
    [InlineData(double.NaN, 13.0, false, 9)]       // a room that is no number: no axis to ask, the most (TickBudget)
    [InlineData(double.PositiveInfinity, 13.0, false, 9)]   // a room without end: the most
    public void ARuler_AsksForAsManyTicksAsFit_BetweenTheFewestAndTheMost(double length, double font, bool alongX, int expected)
    {
        double[] ticks = new TickRequest(new Range1D(0, 1000), length, font, alongX, Theme.Default, TickAxisKind.Ruler).Majors();

        // 0..1000 has a tick for every 100 at 9 or fewer; the number of ticks the locator may return shows in its result.
        Assert.True(ticks.Length <= expected);
        Assert.Equal(new MaxNLocator(expected).Locate(0, 1000), ticks);
    }

    // ---- the ticks of a range ------------------------------------------------------------------------------------------

    [Fact]
    public void ARangeOf0To11_HasTheTicks0To10InTwos_WrittenInOneFormat()
    {
        var scale = Linear(0, 11, 300, 13, vertical: true);

        Assert.Equal([0.0, 2, 4, 6, 8, 10], scale.Ticks);
        Assert.Equal(["0", "2", "4", "6", "8", "10"], scale.Labels);
    }

    [Fact]
    public void ARangeOfFractions_WritesEveryLabelWithTheSameNumberOfDecimals()
    {
        var scale = Linear(0, 1, 300, 13, vertical: true);

        Assert.Equal(["0.0", "0.2", "0.4", "0.6", "0.8", "1.0"], scale.Labels);
    }

    [Fact]
    public void ATickPastTheEndOfTheRange_IsLeftOut()
    {
        // The locator may reach a hundredth of a step past the range: the tick 10 is 0.01 past 9.99 and is not drawn.
        var scale = Linear(0, 9.99, 300, 13, vertical: true);

        Assert.All(scale.Ticks, tick => Assert.InRange(tick, 0, 9.99));
        Assert.DoesNotContain(10.0, scale.Ticks);
    }

    [Fact]
    public void ARangeFromHighToLow_HasTheSameTicks_AndTheFractionRunsFromTheStartOfTheGradient()
    {
        var scale = Linear(11, 0, 300, 13, vertical: true);

        Assert.Equal([0.0, 2, 4, 6, 8, 10], scale.Ticks);
        // The start of the gradient is the value Min: the ticks 0..10 stand at 1 - v/11 along it.
        double[] expected = [1.0, 1 - 2.0 / 11, 1 - 4.0 / 11, 1 - 6.0 / 11, 1 - 8.0 / 11, 1.0 / 11];
        Assert.Equal(expected.Length, scale.Fractions.Length);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], scale.Fractions[i], 12);
    }

    [Theory]
    [InlineData(5.0, 5.0)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    public void ARangeThatCannotBeDrawn_HasNoTick(double min, double max)
    {
        var scale = Linear(min, max, 300, 13, vertical: true);

        Assert.Empty(scale.Ticks);
        Assert.Empty(scale.Labels);
    }

    [Fact]
    public void AShorterBar_HasFewerTicksThanALongOne()
    {
        var shortBar = Linear(0, 11, 45, 13, vertical: true);
        var longBar = Linear(0, 11, 300, 13, vertical: true);

        Assert.True(shortBar.Ticks.Length < longBar.Ticks.Length);
    }

    // ---- what the layout reserves --------------------------------------------------------------------------------------

    [Fact]
    public void TheWidestLabel_IsTheWidestOverEveryNumberOfTicksTheBarCanAskFor()
    {
        var ctx = new SvgRenderContext();
        var font = Theme.Default.TickFont();

        double widest = ColorBarScale.WidestLabel(0, 123456789, ctx, font, LinearNormalizer.Instance, Theme.Default);

        // The shortest bar asks for 2 ticks: 0 and 100000000, the longest label there is.
        Assert.Equal(ctx.MeasureText("100000000", font).Width, widest, 9);
    }

    [Theory]
    [InlineData(2.0, 2.0)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    public void TheWidestLabelOfARangeThatCannotBeDrawn_IsNothing(double min, double max) =>
        Assert.Equal(0, ColorBarScale.WidestLabel(min, max, new SvgRenderContext(), Theme.Default.TickFont(), LinearNormalizer.Instance, Theme.Default));

    // ---- the range a bar covers ----------------------------------------------------------------------------------------

    [Fact]
    public void WithNoProvider_TheRangeIsTheBarsOwn()
    {
        var range = new ColorBar { Min = 2, Max = 9 }.RangeOf(null, Theme.Default);

        Assert.Equal(new MinMaxRange(2, 9), range);
    }

    [Fact]
    public void AProvidersRange_ReplacesTheBarsOwn()
    {
        double[,] data = { { 3, 30 }, { 4, 5 } };

        var range = new ColorBar { Min = 2, Max = 9 }.RangeOf(new HeatmapSeries(data), Theme.Default);

        Assert.Equal(new MinMaxRange(3, 30), range);
    }

    /// <summary>A provider whose range is one value.</summary>
    private sealed class FlatProvider : IColorBarDataProvider
    {
        public IColorMap? ColorMap { get; set; }
        public MinMaxRange GetColorBarRange() => new(4, 4);
    }

    [Fact]
    public void AProviderWithNoWidth_LeavesTheBarsOwnRange()
    {
        var range = new ColorBar { Min = 2, Max = 9 }.RangeOf(new FlatProvider(), Theme.Default);

        Assert.Equal(new MinMaxRange(2, 9), range);
    }

    [Fact]
    public void ARangeWithNoWidth_IsOpenedOneWide_AsEveryColourRangeIs()
    {
        Assert.Equal(new MinMaxRange(4, 5), new ColorBar { Min = 4, Max = 4 }.RangeOf(null, Theme.Default));
        Assert.Equal(new MinMaxRange(4, 5), new ColorBar { Min = 4, Max = 4 }.RangeOf(null, Theme.MatplotlibV2));
    }
}
