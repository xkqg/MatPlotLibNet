// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The ticks of a colour bar of continuous colour under the matplotlib themes, as matplotlib's: the bar's long axis is an
/// axis with matplotlib's <c>AutoLocator</c>, so the default style lays <c>nbins = 'auto'</c> (the axis' tick space: its length over
/// twice the label size along Y, three times along X, at most nine) with the steps 1, 2, 2.5, 5 and 10, and the classic style nine bins
/// with the steps 1, 2, 5 and 10 and round numbers. Printed with matplotlib 3.11.2 for <c>fig.colorbar(im, ax=ax, orientation=...,
/// shrink=...)</c> on a 6 x 4 inch figure at 100 dpi, the bar's length in pixels beside each row. The library laid them by a rule of
/// its own for the bar's length (0.1 to 0.9 over 0.0016 to 0.93 where matplotlib names 0.2 to 0.8); its own themes keep that rule.</summary>
public sealed class ColorBarAutoLocatorTests
{
    private static double[] TicksOf(Theme theme, double lo, double hi, double lengthPx, bool vertical) =>
        ColorBarScale.For(lo, hi, lengthPx, vertical, LinearNormalizer.Instance, new RecordingRenderContext(), theme.TickFont(),
            theme).Ticks;

    [Theory]
    [InlineData(0, 11, 308.0, true, new[] { 0.0, 2, 4, 6, 8, 10 })]
    [InlineData(0.0016, 0.93, 308.0, true, new[] { 0.2, 0.4, 0.6, 0.8 })]
    [InlineData(0, 11, 465.0, false, new[] { 0.0, 2, 4, 6, 8, 10 })]
    [InlineData(0, 11, 92.4, true, new[] { 0.0, 5, 10 })]
    [InlineData(-0.685, 1.056, 308.0, true, new[] { -0.6, -0.4, -0.2, 0.0, 0.2, 0.4, 0.6, 0.8, 1.0 })]
    [InlineData(0, 11, 186.0, false, new[] { 0.0, 5, 10 })]
    public void UnderV2_ABarNamesTheTicksOfMatplotlibsAutoLocator_ForItsLength(double lo, double hi, double length, bool vertical, double[] expected)
    {
        Assert.Equal(expected, TicksOf(Theme.MatplotlibV2, lo, hi, length, vertical), new Tolerance(1e-9));
    }

    [Theory]
    [InlineData(0, 11, 320.0, true, new[] { 0.0, 2, 4, 6, 8, 10 })]
    [InlineData(0.0016, 0.93, 320.0, true, new[] { 0.2, 0.4, 0.6, 0.8 })]
    [InlineData(0, 11, 465.0, false, new[] { 0.0, 2, 4, 6, 8, 10 })]
    [InlineData(-0.685, 1.056, 320.0, true, new[] { -0.5, 0.0, 0.5, 1.0 })]
    public void UnderClassic_ABarNamesTheTicksOfTheClassicAutoLocator(double lo, double hi, double length, bool vertical, double[] expected)
    {
        Assert.Equal(expected, TicksOf(Theme.MatplotlibClassic, lo, hi, length, vertical), new Tolerance(1e-9));
    }

    /// <summary>The layout reserves the widest label any length can give, so it holds the labels the theme's locator names.</summary>
    [Fact]
    public void TheWidestLabelUnderV2_HoldsEveryLabelTheBarCanName()
    {
        var ctx = new RecordingRenderContext();
        var font = Theme.MatplotlibV2.TickFont();
        double widest = ColorBarScale.WidestLabel(-0.685, 1.056, ctx, font, LinearNormalizer.Instance, Theme.MatplotlibV2);

        foreach (double length in new[] { 40.0, 92.4, 150, 308, 600 })
        {
            var scale = ColorBarScale.For(-0.685, 1.056, length, vertical: true, LinearNormalizer.Instance, ctx, font, Theme.MatplotlibV2);
            Assert.All(scale.Labels, label => Assert.True(ctx.MeasureText(label, font).Width <= widest + 1e-9, label));
        }
    }

    /// <summary>Under the classic theme the locator's nine bins do not follow the length, so the widest label is that of its one run
    /// (-0.5, 0, 0.5, 1 over -0.685 to 1.056: the true minus of "−0.5").</summary>
    [Fact]
    public void TheWidestLabelUnderClassic_IsThatOfItsOneRun()
    {
        var ctx = new RecordingRenderContext();
        var font = Theme.MatplotlibClassic.TickFont();

        double widest = ColorBarScale.WidestLabel(-0.685, 1.056, ctx, font, LinearNormalizer.Instance, Theme.MatplotlibClassic);

        Assert.Equal(new[] { "−0.5", "0.0", "0.5", "1.0" }.Max(label => ctx.MeasureText(label, font).Width), widest, 9);
    }

    /// <summary>The library's own theme keeps its rule: 0.1 to 0.9 over 0.0016 to 0.93 on a bar 308 pixels long.</summary>
    [Fact]
    public void UnderTheLibrarysOwnTheme_ABarKeepsItsOwnRule()
    {
        Assert.Equal([0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9], TicksOf(Theme.Default, 0.0016, 0.93, 308, true), new Tolerance(1e-9));
    }

    /// <summary>matplotlib's tick space is the length of the bar's inner axes, which a triangle extend shrinks; a bar of rectangles is
    /// measured whole, as the library's own rule measured it.</summary>
    [Fact]
    public void TheTicksOfABarWithTriangles_AreLaidAlongItsInterior()
    {
        var bar = new Rect(500, 40, 20, 330);

        Assert.Equal(300, new ColorBarStrip(bar, true, ColorBarExtend.Both, false, 50) { Triangles = true }.AxisLength, 9);
        Assert.Equal(330, new ColorBarStrip(bar, true, ColorBarExtend.Both, false, 50).AxisLength, 9);
        Assert.Equal(330, new ColorBarStrip(new Rect(0, 0, 330, 20), false, ColorBarExtend.Neither, false, 50) { Triangles = true }.AxisLength, 9);
    }

    private sealed class Tolerance(double eps) : IEqualityComparer<double>
    {
        public bool Equals(double a, double b) => Math.Abs(a - b) <= eps;
        public int GetHashCode(double value) => 0;
    }
}
