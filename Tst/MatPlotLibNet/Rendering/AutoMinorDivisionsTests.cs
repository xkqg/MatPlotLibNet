// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Under the matplotlib themes the minor ticks of a linear axis split the step between two major ticks as matplotlib 3.11.2's
/// <c>AutoMinorLocator</c> does with <c>xtick.minor.ndivs</c> and <c>ytick.minor.ndivs</c> at <c>'auto'</c> (both styles):
/// <c>majorstep_mantissa = 10 ** (np.log10(majorstep) % 1)</c>, five parts when it is close to 1, 2.5, 5 or 10, four otherwise.
/// Printed with <c>ax.set_xlim(lo, hi); ax.minorticks_on()</c>: a major step of 2 gives minor ticks every 0.5, 0.2 every 0.05, 0.5
/// every 0.1, 1 every 0.2, 5 every 1, a date axis whose major ticks stand 4 days apart every day. The library's own themes keep
/// their five parts.</summary>
public sealed class AutoMinorDivisionsTests
{
    private static double[] Between(double[] majors, Theme theme)
    {
        var values = new List<double>();
        foreach (double v in theme.Looks.Ticks.MinorValuesBetween(new TickConfig(), majors)) values.Add(v);
        return [.. values];
    }

    private static double Step(double[] majors, Theme theme)
    {
        var values = Between(majors, theme);
        return Math.Round(values[1] - values[0], 12);
    }

    [Theory]
    [InlineData(2.0, 0.5)]
    [InlineData(0.2, 0.05)]
    [InlineData(0.5, 0.1)]
    [InlineData(1.0, 0.2)]
    [InlineData(5.0, 1.0)]
    [InlineData(25.0, 5.0)]
    [InlineData(4.0, 1.0)]
    [InlineData(20.0, 5.0)]
    [InlineData(0.25, 0.05)]
    [InlineData(10.0, 2.0)]
    public void UnderTheMatplotlibThemes_TheStepIsSplitInFourUnlessItsMantissaIsOneTwoAndAHalfFiveOrTen(double majorStep, double minorStep)
    {
        double[] majors = [0, majorStep, 2 * majorStep, 3 * majorStep];

        Assert.Equal(minorStep, Step(majors, Theme.MatplotlibV2), 9);
        Assert.Equal(minorStep, Step(majors, Theme.MatplotlibClassic), 9);
    }

    [Theory]
    [InlineData(2.0)]
    [InlineData(0.2)]
    [InlineData(1.0)]
    public void UnderTheLibrarysThemes_TheStepIsSplitInFive(double majorStep)
    {
        double[] majors = [0, majorStep, 2 * majorStep];

        Assert.Equal(majorStep / 5, Step(majors, Theme.Default), 9);
        Assert.Equal(majorStep / 5, Step(majors, Theme.Dark), 9);
    }

    [Fact]
    public void AGivenMinorSpacing_WinsUnderEveryTheme()
    {
        var spaced = new TickConfig { Spacing = 0.25 };
        var values = new List<double>();
        foreach (double v in Theme.MatplotlibV2.Looks.Ticks.MinorValuesBetween(spaced, [0.0, 2.0, 4.0])) values.Add(v);

        Assert.Equal(0.25, Math.Round(values[1] - values[0], 12));
    }

    private static double[] RedMarkXs(Theme theme)
    {
        var figure = Plt.Create().WithSize(640, 480).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 1.0]).SetXLim(0, 10).WithMinorTicks())
            .Build();
        var x = figure.SubPlots[0].XAxis;
        x.MinorTicks = x.MinorTicks with { Color = Colors.Red };
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        // The bottom edge's marks (classic marks the top edge too), one X each.
        return [.. ctx.Of<DrawLineCall>()
            .Where(a => a.Color == Colors.Red)
            .Select(a => Math.Round((a.P1).X, 6)).Distinct().Order()];
    }

    [Fact]
    public void UnderV2_ALinearAxisOverZeroToTen_HasAMinorTickEveryHalf()
    {
        // ax.set_xlim(0, 10); ax.minorticks_on(): majors 0, 2, .., 10, minors 0.5, 1, 1.5, 2.5, .. (15 in view).
        Assert.Equal(15, RedMarkXs(Theme.MatplotlibV2).Length);
        Assert.Equal(15, RedMarkXs(Theme.MatplotlibClassic).Length);
    }

    [Fact]
    public void UnderV2_The3DMinorMarks_SplitAStepOfTwoInFour()
    {
        var figure = Plt.Create().WithSize(600, 500).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4]))
            .Build();
        var axes = figure.SubPlots[0];
        axes.Emit3DVertexData = true;
        axes.XAxis.TickLocator = new FixedLocator([0.0, 2, 4]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Color = Colors.Blue };
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = true, Color = Colors.Red };

        var svg = figure.ToSvg();

        svg.AssertMinorMarksSplitEachMajorStepIn(4, 6);   // three between each two neighbouring majors, at a quarter of the step
    }
}
