// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a shaded span and a trend line add to the range of an axes. Under the matplotlib themes the numbers are matplotlib
/// 3.11.2's, printed with python: <c>axvspan</c> adds its rectangle to the data limits of X and asks for X to be scaled
/// (<c>axes/_axes.py</c> 1099-1110), <c>axhspan</c> the same on Y (1036-1047), and <c>axline</c> adds its two points to the data limits
/// of both axes and asks for both (975, 990-992). A rectangle with an edge that is not finite adds nothing (its corners are not
/// numbers), but the span still asks. The library's own themes keep their rule: a span is clipped to the range the data give, and a
/// trend line is drawn over it, neither ranging the axes.</summary>
public class OverlayAutoscaleTests
{
    private static void Range(double lo, double hi, double drawnLo, double drawnHi)
    {
        lo.AssertCloseTo(drawnLo, relative: 1e-12, floor: 1);
        hi.AssertCloseTo(drawnHi, relative: 1e-12, floor: 1);
    }

    private static Axes WithALine()
    {
        var axes = new Axes();
        axes.Plot(new double[] { 0, 5, 10 }, new double[] { 0, 5, 10 });
        return axes;
    }

    /// <summary>The row's repro: <c>ax.axvspan(1, 2)</c> alone: xlim (0.95, 2.05) default, (1, 2) classic; ylim stays (0, 1), as
    /// nothing asks for Y. Before the fix X kept the first view (0, 1) and the span stood past it.</summary>
    [Theory]
    [InlineData("v2", 0.95, 2.05)]
    [InlineData("classic", 1.0, 2.0)]
    public void AVerticalSpanAlone_RangesX(string theme, double lo, double hi)
    {
        var axes = new Axes();
        axes.AxVSpan(1, 2);

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.XMin, range.XMax);
        Range(0, 1, range.YMin, range.YMax);
    }

    /// <summary><c>ax.axhspan(1, 2)</c> alone: ylim (0.95, 2.05) default, (1, 2) classic; xlim stays (0, 1).</summary>
    [Theory]
    [InlineData("v2", 0.95, 2.05)]
    [InlineData("classic", 1.0, 2.0)]
    public void AHorizontalSpanAlone_RangesY(string theme, double lo, double hi)
    {
        var axes = new Axes();
        axes.AxHSpan(1, 2);

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(0, 1, range.XMin, range.XMax);
        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary><c>ax.axline((1, 1), (3, 3))</c> alone: xlim = ylim (0.9, 3.1) default, (1, 3) classic.</summary>
    [Theory]
    [InlineData("v2", 0.9, 3.1)]
    [InlineData("classic", 1.0, 3.0)]
    public void ATrendlineAlone_RangesBothAxesByItsPoints(string theme, double lo, double hi)
    {
        var axes = new Axes();
        axes.AddTrendline(1, 1, 3, 3);

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.XMin, range.XMax);
        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>Beside <c>ax.plot([0, 5, 10], [0, 5, 10])</c>: <c>axvspan(12, 14)</c> gives xlim (-0.7, 14.7) default, (0, 14) classic,
    /// ylim the line's (-0.5, 10.5), (0, 10); <c>axhspan(12, 14)</c> the same the other way round.</summary>
    [Theory]
    [InlineData("v2", -0.7, 14.7, -0.5, 10.5)]
    [InlineData("classic", 0.0, 14.0, 0.0, 10.0)]
    public void ASpanPastTheData_WidensItsAxis(string theme, double spanLo, double spanHi, double lineLo, double lineHi)
    {
        var vertical = WithALine();
        vertical.AxVSpan(12, 14);
        var horizontal = WithALine();
        horizontal.AxHSpan(12, 14);

        var v = vertical.ComputeDataRanges(theme.AsTheme());
        var h = horizontal.ComputeDataRanges(theme.AsTheme());

        Range(spanLo, spanHi, v.XMin, v.XMax);
        Range(lineLo, lineHi, v.YMin, v.YMax);
        Range(lineLo, lineHi, h.XMin, h.XMax);
        Range(spanLo, spanHi, h.YMin, h.YMax);
    }

    /// <summary>Beside the line, <c>ax.axline((0, 0), (20, 30))</c>: xlim (-1, 21), ylim (-1.5, 31.5) default; (0, 20), (0, 30) classic.</summary>
    [Theory]
    [InlineData("v2", -1.0, 21.0, -1.5, 31.5)]
    [InlineData("classic", 0.0, 20.0, 0.0, 30.0)]
    public void ATrendlinePastTheData_WidensBothAxes(string theme, double xLo, double xHi, double yLo, double yHi)
    {
        var axes = WithALine();
        axes.AddTrendline(0, 0, 20, 30);

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(xLo, xHi, range.XMin, range.XMax);
        Range(yLo, yHi, range.YMin, range.YMax);
    }

    /// <summary><c>ax.axvspan(0, np.inf)</c> and <c>ax.axvspan(np.nan, 1)</c> alone add nothing and ask for X: xlim (-0.055, 0.055)
    /// default, (-0.06, 0.06) classic (an empty axis that is asked, as <c>ax.plot([], [])</c>); ylim stays (0, 1).
    /// <c>ax.axline((0, 0), (np.inf, 1))</c> adds the one finite point, (0, 0), and asks for both: (-0.055, 0.055) on both, (-0.06, 0.06)
    /// classic.</summary>
    [Theory]
    [InlineData("v2", -0.055, 0.055)]
    [InlineData("classic", -0.06, 0.06)]
    public void AnOverlayWithAnEdgeThatIsNotAValue_AsksWithoutAddingIt(string theme, double lo, double hi)
    {
        var infinite = new Axes();
        infinite.AxVSpan(0, double.PositiveInfinity);
        var missing = new Axes();
        missing.AxVSpan(double.NaN, 1);
        var trend = new Axes();
        trend.AddTrendline(0, 0, double.PositiveInfinity, 1);

        var i = infinite.ComputeDataRanges(theme.AsTheme());
        var m = missing.ComputeDataRanges(theme.AsTheme());
        var t = trend.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, i.XMin, i.XMax);
        Range(0, 1, i.YMin, i.YMax);
        Range(lo, hi, m.XMin, m.XMax);
        Range(0, 1, m.YMin, m.YMax);
        Range(lo, hi, t.XMin, t.XMax);
        Range(lo, hi, t.YMin, t.YMax);
    }

    /// <summary>A threshold's zone runs to ±<see cref="double.MaxValue"/>, the library's spelling of "to the plot edge": it adds nothing to
    /// the range under any theme, so the range is the data's and the threshold's line's.</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    [InlineData("default")]
    public void AThresholdZone_AddsNothingButItsLine(string theme)
    {
        var look = theme.AsTheme();
        var threshold = WithALine();
        threshold.AddThreshold(8, Orientation.Horizontal, ThresholdBreach.Above);
        threshold.AddThreshold(2, Orientation.Vertical, ThresholdBreach.Below);
        var lines = WithALine();
        lines.AxHLine(8);
        lines.AxVLine(2);

        Assert.Equal(lines.ComputeDataRanges(look), threshold.ComputeDataRanges(look));
    }

    /// <summary><c>ax.axvspan(1, 2)</c> with <c>ax.add_patch(Rectangle((0, 0), 5, 5))</c>: the span asks for X, so the rectangle ranges X
    /// too, (-0.25, 5.25) default, (0, 5) classic; nothing asks for Y, which stays (0, 1).</summary>
    [Theory]
    [InlineData("v2", -0.25, 5.25)]
    [InlineData("classic", 0.0, 5.0)]
    public void ASpanAsks_SoAPatchRangesItsAxisToo(string theme, double lo, double hi)
    {
        var axes = new Axes();
        axes.AxVSpan(1, 2);
        axes.AddPatch(new Rectangle(new(0, 0), 5, 5));

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.XMin, range.XMax);
        Range(0, 1, range.YMin, range.YMax);
    }

    /// <summary>The patches range every axis something asks for, whether or not what asks adds a value: <c>ax.plot([], [])</c> with
    /// <c>Rectangle((0, 0), 5, 5)</c> gives (-0.25, 5.25) on both axes default, (0, 5) classic (an empty plot asks for both);
    /// <c>axvspan(0, np.inf)</c> with it gives X (-0.25, 5.25), Y (0, 1); <c>axline((0, 0), (np.inf, 1))</c> with
    /// <c>Rectangle((1, 1), 5, 5)</c> gives (-0.3, 6.3) on both, (0, 6) classic (its finite point and the rectangle). Before, the
    /// patches ranged only an axis a value was added to, so these kept the empty axis' (-0.055, 0.055).</summary>
    [Theory]
    [InlineData("v2", -0.25, 5.25, -0.3, 6.3)]
    [InlineData("classic", 0.0, 5.0, 0.0, 6.0)]
    public void WhatAsksWithoutAValue_StillLetsThePatchesRange(string theme, double lo, double hi, double trendLo, double trendHi)
    {
        var empty = new Axes();
        empty.Plot(Array.Empty<double>(), Array.Empty<double>());
        empty.AddPatch(new Rectangle(new(0, 0), 5, 5));
        var span = new Axes();
        span.AxVSpan(0, double.PositiveInfinity);
        span.AddPatch(new Rectangle(new(0, 0), 5, 5));
        var trend = new Axes();
        trend.AddTrendline(0, 0, double.PositiveInfinity, 1);
        trend.AddPatch(new Rectangle(new(1, 1), 5, 5));

        var e = empty.ComputeDataRanges(theme.AsTheme());
        var s = span.ComputeDataRanges(theme.AsTheme());
        var t = trend.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, e.XMin, e.XMax);
        Range(lo, hi, e.YMin, e.YMax);
        Range(lo, hi, s.XMin, s.XMax);
        Range(0, 1, s.YMin, s.YMax);
        Range(trendLo, trendHi, t.XMin, t.XMax);
        Range(trendLo, trendHi, t.YMin, t.YMax);
    }

    /// <summary>The library's own themes keep their look: a span is clipped to the range the data give and a trend line is drawn over
    /// it, neither moving the range.</summary>
    [Fact]
    public void UnderTheLibrarysTheme_SpansAndTrendlinesRangeNothing()
    {
        var overlays = WithALine();
        overlays.AxVSpan(12, 14);
        overlays.AxHSpan(-9, -4);
        overlays.AddTrendline(0, 0, 20, 30);

        Assert.Equal(WithALine().ComputeDataRanges(Theme.Default), overlays.ComputeDataRanges(Theme.Default));
    }

    /// <summary>A probability plot's reference line is a trend line from the first theoretical quantile to the last, and scipy draws it
    /// with <c>ax.plot</c> (<c>scipy.stats.probplot(..., plot=ax)</c>, <c>_morestats.py</c> 681), so its ends range Y where they pass the
    /// sample. scipy 1.18.1 under the default style: the returns sample of the Q-Q sample gives ylim (-3.6951830046782925,
    /// 2.7988430982441415) (the line ends at 2.5037, the sample at 2.1); the waits against the exponential distribution ylim
    /// (-0.3635675250031408, 8.817312739285864) and xlim (-0.13258596436868494, 3.5467671503583227).</summary>
    [Fact]
    public void AProbabilityPlotsReferenceLine_RangesYAsProbplotsLineDoes()
    {
        double[] returns = [0.8, -1.2, 0.3, 2.1, -0.4, 0.9, -2.6, 0.1, 1.4, -0.7, 0.5, -0.2, 1.1, -0.9, 0.6, -3.4, 0.2, 1.7, -0.5, 0.4];
        double[] waits = [2.1, 0.4, 5.6, 1.3, 0.9, 3.8, 0.2, 1.7, 2.6, 0.6, 8.4, 1.1, 0.7, 4.2, 1.9, 0.3, 3.1, 1.5, 0.8, 2.3];
        var qq = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.QQPlot(returns)).Build().SubPlots[0].ComputeDataRanges(Theme.MatplotlibV2);
        var expon = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ProbabilityPlot(waits, QQReference.Exponential)).Build()
            .SubPlots[0].ComputeDataRanges(Theme.MatplotlibV2);

        Assert.InRange(qq.YMin, -3.6951830046782925 - 1e-9, -3.6951830046782925 + 1e-9);
        Assert.InRange(qq.YMax, 2.7988430982441415 - 1e-9, 2.7988430982441415 + 1e-9);
        Assert.InRange(expon.YMin, -0.3635675250031408 - 1e-9, -0.3635675250031408 + 1e-9);
        Assert.InRange(expon.YMax, 8.817312739285864 - 1e-9, 8.817312739285864 + 1e-9);
        Assert.InRange(expon.XMin, -0.13258596436868494 - 1e-9, -0.13258596436868494 + 1e-9);
        Assert.InRange(expon.XMax, 3.5467671503583227 - 1e-9, 3.5467671503583227 + 1e-9);
    }

    /// <summary>An axes that shares X with one that holds a span is ranged by the span too, as matplotlib's shared axes are ranged over
    /// the data limits of every axes of the share.</summary>
    [Theory]
    [InlineData("v2", -0.7, 14.7)]
    [InlineData("classic", 0.0, 14.0)]
    public void AnAxesSharingX_IsRangedByTheSpanOfTheOther(string theme, double lo, double hi)
    {
        var withSpan = WithALine();
        withSpan.AxVSpan(12, 14);
        var sharing = WithALine();
        sharing.ShareXWith = withSpan;

        var range = sharing.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.XMin, range.XMax);
    }
}
