// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The ticks of a colour bar under matplotlib's <c>LogNorm</c>: matplotlib's <c>Colorbar</c> takes the scale of its norm (3.11.2,
/// <c>_reset_locator_formatter_scale</c>: <c>_scale = 'log'</c>), whose axis has a <c>LogLocator</c> and a <c>LogFormatterSciNotation</c>
/// (printed for a bar of <c>LogNorm(2, 200)</c>: major ticks 10 and 100 labelled 10^1 and 10^2, from 1 to 1000 the four decades
/// 10^0 to 10^3, from 0.5 to 3 the one tick at 1). The ticks stand on the powers of ten and read as the library's log axes read them,
/// at the fraction of the bar the norm sends them to; every other normaliser keeps the locator of values it had.</summary>
public class ColorBarLogTicksTests
{
    private static ColorBarScale ScaleOf(double min, double max, INormalizer normalizer) =>
        ColorBarScale.For(min, max, 300, vertical: true, normalizer, new RecordingRenderContext(), Theme.MatplotlibV2.TickFont(), Theme.Default);

    /// <summary>The ticks a bar of matplotlib's log norm names over <paramref name="min"/> .. <paramref name="max"/>, by the rule a log
    /// axis names them by (<see cref="LogLocator.TicksNamedOver"/>), with their labels.</summary>
    private static (double Value, string Label)[] Named(double min, double max) =>
        [.. LogLocator.TicksNamedOver(min, max, TickBudget.Standard.Most, Theme.MatplotlibV2).Select(v => (v, new LogTickFormatter().SciNotation(v)))];

    [Fact]
    public void ABarOfALogNorm_NamesThePowersOfTenInItsRange()
    {
        var scale = ScaleOf(2, 200, LogNorm.Instance);

        Assert.Equal([10.0, 100.0], scale.Ticks);
        Assert.Equal(["10¹", "10²"], scale.Labels);
        Assert.Equal(Math.Log(5) / Math.Log(100), scale.Fractions[0], 12);
        Assert.Equal(Math.Log(50) / Math.Log(100), scale.Fractions[1], 12);
    }

    [Fact]
    public void ABarOverThreeDecades_NamesEveryDecade()
    {
        var scale = ScaleOf(1, 1000, LogNorm.Instance);

        Assert.Equal([1.0, 10.0, 100.0, 1000.0], scale.Ticks);
        Assert.Equal(["10⁰", "10¹", "10²", "10³"], scale.Labels);
    }

    /// <summary>matplotlib's <c>LogFormatter.set_locs</c> (3.11.2, <c>minor_thresholds = (1, 0.4)</c>, printed for these bars): with one power of
    /// ten in the range, or none, and the range more than 0.4 of a decade wide the powers and the ticks at 2, 3, 4 and 6 times them are labelled
    /// (0.5 to 3: 6x10^-1, 10^0, 2x10^0, 3x10^0; 2 to 8: 2, 3, 4 and 6 times 10^0; 100 to 947: 10^2 and 2, 3, 4 and 6 times it).</summary>
    [Theory]
    [InlineData(0.5, 3.0, new[] { 0.6, 1.0, 2.0, 3.0 }, new[] { "6×10⁻¹", "10⁰", "2×10⁰", "3×10⁰" })]
    [InlineData(2.0, 8.0, new[] { 2.0, 3.0, 4.0, 6.0 }, new[] { "2×10⁰", "3×10⁰", "4×10⁰", "6×10⁰" })]
    [InlineData(100.0, 947.0, new[] { 100.0, 200.0, 300.0, 400.0, 600.0 }, new[] { "10²", "2×10²", "3×10²", "4×10²", "6×10²" })]
    public void ABarWithOnePowerOfTenInItOrNone_AlsoNamesTheMultiplesOfIt(double min, double max, double[] ticks, string[] labels)
    {
        var named = Named(min, max);

        Assert.Equal(ticks.Length, named.Length);
        for (int i = 0; i < ticks.Length; i++) Assert.Equal(ticks[i], named[i].Value, 12);
        Assert.Equal(labels, named.Select(t => t.Label).ToArray());
    }

    /// <summary>A range of 0.4 of a decade or less labels every multiple 1 to 9 (matplotlib's third arm of <c>set_locs</c>): 100 to 250 names
    /// 10^2 and 2x10^2. Over 100 to 130 only 10^2 stands in view, and matplotlib's minor <c>LogLocator</c> then lays <c>AutoLocator</c>'s
    /// ticks instead ("no more than one major or minor tick, switch to AutoLocator"), each named (printed: 10^2, 1.05x10^2, ... 1.3x10^2).</summary>
    [Fact]
    public void ANarrowBar_NamesEveryMultipleInIt_OrAutoLocatorsTicksWhenOneStandsInIt()
    {
        var named = Named(100, 250);

        Assert.Equal([100.0, 200.0], named.Select(t => t.Value).ToArray());
        Assert.Equal(["10²", "2×10²"], named.Select(t => t.Label).ToArray());
        Assert.Equal(["10²", "1.05×10²", "1.1×10²", "1.15×10²", "1.2×10²", "1.25×10²", "1.3×10²"], Named(100, 130).Select(t => t.Label).ToArray());
    }

    [Fact]
    public void ABarOfALogNorm_WithTwoPowersOfTenInIt_NamesOnlyThePowers()
    {
        Assert.Equal([10.0, 100.0], Named(2, 200).Select(t => t.Value).ToArray());
        Assert.Equal([1.0, 10.0, 100.0, 1000.0], Named(1, 1000).Select(t => t.Value).ToArray());
    }

    [Fact]
    public void TheLibrarysLogNormalizer_AndEveryOtherNormalizer_KeepTheTicksOfValues()
    {
        Assert.DoesNotContain("10²", ScaleOf(2, 200, new LogNormalizer()).Labels);
        Assert.DoesNotContain("10²", ScaleOf(2, 200, LinearNormalizer.Instance).Labels);
    }

    [Fact]
    public void TheRoomForTheLabels_CountsTheWidthOfThePowersOfTen()
    {
        var ctx = new RecordingRenderContext();
        var font = Theme.MatplotlibV2.TickFont();

        double widest = ColorBarScale.WidestLabel(2, 200, ctx, font, LogNorm.Instance, Theme.Default);

        Assert.True(widest >= ctx.MeasureText("10²", font).Width);
    }
}
