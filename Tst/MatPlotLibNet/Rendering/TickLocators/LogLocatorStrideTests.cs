// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The powers of ten a log scale names when more decades stand in view than labels fit. matplotlib 3.11.2's
/// <c>LogLocator.tick_values</c> (ticker.py): <c>n_request = numticks if numticks != 'auto' else np.clip(self.axis.get_tick_space(), 2, 9)
/// if self.axis is not None else 9</c>; under the default style (<c>numticks='auto'</c>) "<c>stride = n_avail // (n_request + 1) + 1</c>",
/// then "Pick the largest stride that yields this actual n_request" and an offset that puts the ticks on multiples of the stride if it
/// can; in classic mode (<c>numticks = 15</c>) "<c>stride = max(math.ceil((n_avail - 1) / (n_request - 1)), 1)</c>" from
/// <c>emin - stride</c>. Printed with <c>ax.set_yscale('log'); ax.set_ylim(lo, hi)</c> (the major ticks in view) and with
/// <c>fig.colorbar</c> of a <c>LogNorm</c> on a 6 x 4 inch figure at 100 dpi. The library named every power in view.</summary>
public sealed class LogLocatorStrideTests
{
    private static void AssertPowers(int[] exponents, double[] actual)
    {
        double[] expected = [.. exponents.Select(e => Math.Pow(10, e))];
        Assert.True(expected.Length == actual.Length, $"expected {string.Join(", ", expected)}, got {string.Join(", ", actual)}");
        for (int i = 0; i < expected.Length; i++)
            Assert.True(Math.Abs(expected[i] - actual[i]) <= 1e-9 * expected[i], $"tick {i}: expected {expected[i]:R}, got {actual[i]:R}");
    }

    private static int[] Every(int from, int to, int step) => [.. Enumerable.Range(0, (to - from) / step + 1).Select(k => from + k * step)];

    /// <summary>The plot lengths, in pixels at 100 dpi, of the axes of matplotlib's default figure (6.4 x 4.8 inches: 496 x 369.6),
    /// and of a 3 x 2 and a 1.6 x 1.2 inch figure (Y: 154 and 92.4), whose tick spaces matplotlib prints as 11 (X), 13, 5 and 3 (Y).</summary>
    private const double DefaultWidth = 496, DefaultHeight = 369.6, ThreeByTwoHeight = 154, TinyHeight = 92.4;

    private static double[] YTicks(double lo, double hi, double plotPixels, Theme theme)
    {
        var axis = new Axis { Scale = AxisScale.Log };
        return axis.TickValuesBetween(axis.OwnTickSource(isX: false, Theme.Default), new Range1D(lo, hi), plotPixels, theme, isX: false);
    }

    [Fact]
    public void ALocatorWithNoAxis_AsksForNine_AndStrides()
    {
        // LogLocator().tick_values(1, 1e11) and (1e-10, 1e10) with no axis: n_request 9, stride 2 and 3.
        AssertPowers(Every(0, 10, 2), new LogLocator().Locate(1, 1e11));
        AssertPowers(Every(-9, 9, 3), new LogLocator().Locate(1e-10, 1e10));
    }

    public static TheoryData<double, double, int, bool, int[]> DecadeRuns { get; } = new()
    {
        // LogLocator(numticks=n).tick_values(lo, hi), the decade outside each end included (printed): none in view leaves the two just
        // outside; a single power stands with one past each side; three asked over nine decades; the classic stride from emin - stride.
        { 2, 8, 9, false, [0, 1] },
        { 5, 50, 9, false, [0, 1, 2] },
        { 1, 1e9, 3, false, Every(-4, 12, 4) },
        { 1e-20, 1e20, 15, true, Every(-23, 22, 3) },
    };

    [Theory]
    [MemberData(nameof(DecadeRuns))]
    public void TheMajorLocatorLaysTheDecadesOfMatplotlibsLogLocator(double lo, double hi, int asked, bool classic, int[] exponents) =>
        AssertPowers(exponents, LogLocator.DecadesLaid(lo, hi, asked, classic).Powers());

    public static TheoryData<double, double, double, int[]> DefaultStyleViews { get; } = new()
    {
        // The default figure (tick space 13): 1e-10 .. 1e10 -> 10^-9, 10^-6, .. 10^9; 1 .. 1e11 -> every other power.
        { 1e-10, 1e10, DefaultHeight, Every(-9, 9, 3) },
        { 1, 1e11, DefaultHeight, Every(0, 10, 2) },
        // Twenty and sixty decades: strides 5 and 7, the second offset to the lowest acceptable start (10^-28).
        { 1e-20, 1e20, DefaultHeight, Every(-20, 20, 5) },
        { 1e-30, 1e30, DefaultHeight, Every(-28, 28, 7) },
        // A 3 x 2 inch figure (tick space 5): 1 .. 1e6 -> 10^0, 10^2, 10^4, 10^6; 1e-10 .. 1e10 -> by five.
        { 1, 1e6, ThreeByTwoHeight, Every(0, 6, 2) },
        { 1e-10, 1e10, ThreeByTwoHeight, Every(-10, 10, 5) },
        // A 1.6 x 1.2 inch figure (tick space 3): 1 .. 1e5 -> 10^0, 10^2, 10^4.
        { 1, 1e5, TinyHeight, Every(0, 4, 2) },
    };

    [Theory]
    [MemberData(nameof(DefaultStyleViews))]
    public void UnderTheDefaultTheme_ALogAxisStridesOverDecades_ByItsLength(double lo, double hi, double plotPixels, int[] exponents) =>
        AssertPowers(exponents, YTicks(lo, hi, plotPixels, Theme.MatplotlibV2));

    [Fact]
    public void UnderTheDefaultTheme_AnXAxisStridesByItsOwnTickSpace()
    {
        // The default figure's X axis (tick space 11): 1e-10 .. 1e10 -> 10^-9 .. 10^9 by three.
        var axis = new Axis { Scale = AxisScale.Log };

        AssertPowers(Every(-9, 9, 3),
            axis.TickValuesBetween(axis.OwnTickSource(isX: true, Theme.Default), new Range1D(1e-10, 1e10), DefaultWidth, Theme.MatplotlibV2, isX: true));
    }

    public static TheoryData<double, double, int[]> ClassicViews { get; } = new()
    {
        // Classic mode asks for 15 ticks whatever the length: 21 decades by two, 12 decades every one, 41 by three from 10^-20, 61 by five.
        { 1e-10, 1e10, Every(-10, 10, 2) },
        { 1, 1e11, Every(0, 11, 1) },
        { 1e-20, 1e20, Every(-20, 19, 3) },
        { 1e-30, 1e30, Every(-30, 30, 5) },
    };

    [Theory]
    [MemberData(nameof(ClassicViews))]
    public void UnderTheClassicTheme_ALogAxisStridesForFifteenTicks(double lo, double hi, int[] exponents)
    {
        AssertPowers(exponents, YTicks(lo, hi, DefaultHeight, Theme.MatplotlibClassic));
        AssertPowers(exponents, YTicks(lo, hi, TinyHeight, Theme.MatplotlibClassic));
    }

    [Fact]
    public void ALogYAxisOverTwentyDecades_LabelsSevenPowers()
    {
        // The repro: 21 Y labels, where matplotlib's default figure labels 10^-9, 10^-6, 10^-3, 10^0, 10^3, 10^6, 10^9.
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [1e-10, 1e10]).SetYScale(AxisScale.Log).SetYLim(1e-10, 1e10)).Build().ToSvg().Xml();

        var labels = svg.Each("text").Where(e => (string?)e.Attribute("text-anchor") == "end")
            .Select(e => e.Value).ToList();
        Assert.Equal(["10⁻⁹", "10⁻⁶", "10⁻³", "10⁰", "10³", "10⁶", "10⁹"], labels);
    }

    // ── the colour bar of a log norm reads the same rule ──────────────────────────────────────────────────────

    public static TheoryData<double, double, bool, int[]> DefaultStyleBars { get; } = new()
    {
        // fig.colorbar of LogNorm(lo, hi) on a 6 x 4 inch figure at 100 dpi: the vertical bar is 308 px long (tick space 11), a
        // horizontal one (tick space 8) names the same.
        { 1, 1e12, true, Every(0, 12, 2) },
        { 1, 1e20, true, Every(0, 18, 3) },
        { 1e-3, 1e9, true, Every(-3, 9, 2) },
        { 1, 1e12, false, Every(0, 12, 2) },
    };

    [Theory]
    [MemberData(nameof(DefaultStyleBars))]
    public void UnderTheDefaultTheme_ABarOfALogNormStridesOverDecades(double lo, double hi, bool vertical, int[] exponents)
    {
        var scale = ColorBarScale.For(lo, hi, vertical ? 308 : 400, vertical, LogNorm.Instance, new RecordingRenderContext(),
            Theme.MatplotlibV2.TickFont(), Theme.MatplotlibV2);

        AssertPowers(exponents, scale.Ticks);
    }

    [Fact]
    public void UnderTheClassicTheme_ABarOfALogNormStridesForFifteenTicks()
    {
        // Classic fig.colorbar: 1 .. 1e12 names all thirteen powers, 1 .. 1e20 every other one.
        static double[] Ticks(double lo, double hi) => ColorBarScale.For(lo, hi, 320, vertical: true, LogNorm.Instance,
            new RecordingRenderContext(), Theme.MatplotlibClassic.TickFont(), Theme.MatplotlibClassic).Ticks;

        AssertPowers(Every(0, 12, 1), Ticks(1, 1e12));
        AssertPowers(Every(0, 20, 2), Ticks(1, 1e20));
    }

    [Fact]
    public void ABarGivenALogLocator_StridesByTheBarsLength()
    {
        // cb.locator = LogLocator(): the locator is the bar's long axis', so it reads that axis' tick space as the norm's own does.
        var scale = ColorBarScale.For(1, 1e12, 308, vertical: true, LogNorm.Instance, new RecordingRenderContext(),
            Theme.MatplotlibV2.TickFont(), Theme.MatplotlibV2, new LogLocator());

        AssertPowers(Every(0, 12, 2), scale.Ticks);
    }

    [Fact]
    public void TheRoomABarGivenALogLocatorReserves_IsItsWidestLabelAtAnyLength()
    {
        // A given LogLocator reads the bar's tick space, so the room holds what it names at every count it can be told.
        var ctx = new RecordingRenderContext();
        var font = Theme.MatplotlibV2.TickFont();
        foreach (var (lo, hi) in new[] { (0.5, 1e30), (1e-12, 1e3) })
        {
            double drawn = 0;
            for (double length = 20; length <= 2000; length += 5)
                foreach (var label in ColorBarScale.For(lo, hi, length, vertical: true, LogNorm.Instance, ctx, font, Theme.MatplotlibV2,
                    new LogLocator()).Labels)
                    drawn = Math.Max(drawn, ctx.MeasureText(label, font).Width);

            Assert.Equal(drawn,
                ColorBarScale.WidestLabel(lo, hi, ctx, font, LogNorm.Instance, Theme.MatplotlibV2, new LogLocator()), 9);
        }
    }

    [Fact]
    public void TheRoomABarOfALogNormReserves_HoldsItsWidestLabelAtAnyLength()
    {
        // The layout reserves the room before it knows the bar's length: every label any length names fits in it.
        var ctx = new RecordingRenderContext();
        var font = Theme.MatplotlibV2.TickFont();
        foreach (var (lo, hi) in new[] { (0.5, 1e30), (1e-12, 1e3), (1, 1e12) })
        {
            double drawn = 0;
            for (double length = 20; length <= 2000; length += 5)
                foreach (var label in ColorBarScale.For(lo, hi, length, vertical: true, LogNorm.Instance, ctx, font, Theme.MatplotlibV2).Labels)
                    drawn = Math.Max(drawn, ctx.MeasureText(label, font).Width);

            Assert.True(ColorBarScale.WidestLabel(lo, hi, ctx, font, LogNorm.Instance, Theme.MatplotlibV2) >= drawn);
        }
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    [InlineData("default")]
    public void TheRoomABarOfALogNormReserves_IsItsWidestLabelAtAnyLength_NotALinearLabelItNeverNames(string themeId)
    {
        // A bar of a log norm names powers of ten in scientific notation at every length; the values' own labels of a linear locator
        // over the range ("1000000000000") are never drawn on it, so they take no room.
        var theme = themeId.AsTheme();
        var ctx = new RecordingRenderContext();
        var font = theme.TickFont();
        foreach (var (lo, hi) in new[] { (1.0, 1e12), (0.5, 1e30), (1e-12, 1e3), (2.0, 8.0) })
        {
            double drawn = 0;
            for (double length = 20; length <= 2000; length += 5)
                foreach (var label in ColorBarScale.For(lo, hi, length, vertical: true, LogNorm.Instance, ctx, font, theme).Labels)
                    drawn = Math.Max(drawn, ctx.MeasureText(label, font).Width);

            Assert.Equal(drawn, ColorBarScale.WidestLabel(lo, hi, ctx, font, LogNorm.Instance, theme), 9);
        }
    }
}
