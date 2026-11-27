// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The ticks the classic theme places on a 2-D and a 3-D axis that has no locator or spacing of its own are matplotlib's
/// classic <c>AutoLocator</c>'s. matplotlib 3.11.2, read with inspect: <c>AutoLocator.__init__</c>: <c>if
/// mpl.rcParams['_internal.classic_mode']: nbins = 9; steps = [1, 2, 5, 10]</c> (else <c>nbins = 'auto'</c> and the steps
/// <c>[1, 2, 2.5, 5, 10]</c>); <c>MaxNLocator.tick_values</c> widens a singular range (<c>_nonsingular(vmin, vmax, expander=1e-13,
/// tiny=1e-14)</c>) and lays <c>_raw_ticks</c> over it: the smallest step of the staircase that is at least the range over the bins
/// (and, under <c>axes.autolimit_mode = 'round_numbers'</c>, one from which the bins reach the end of the range), on a 3-D axis
/// with the raw step taken as <c>raw_step * 23 / 24</c>; the axis draws the ticks in view and formats every one the locator gives.
/// Every expected list was printed by matplotlib for the same view limits under <c>plt.style.use('classic')</c>. The library's own
/// placement (8 intervals, a step of 2.5 among the steps) laid ticks every 0.25 over -1 to 1, where matplotlib lays them every 0.5.
/// The rounding of the axis' ends (<c>Range1D.RoundedOutToTicks</c>) reads the same ladder.</summary>
public sealed class ClassicAxisLocatorTests
{
    private static readonly TickSource NoSource = new(null, null, false);

    private static double[] Ticks(Range1D view, Theme theme, double plotPixels = 400) =>
        new Axis().TickValuesBetween(NoSource, view, plotPixels, theme, isX: true);

    // ── 2-D: the ticks in view ──────────────────────────────────────────────────────────────────────────────────

    public static TheoryData<double, double, double[]> Views { get; } = new()
    {
        { -1, 1, [-1, -0.5, 0, 0.5, 1] },
        { 0, 18, [0, 2, 4, 6, 8, 10, 12, 14, 16, 18] },
        { 0, 1, [0, 0.2, 0.4, 0.6, 0.8, 1.0] },
        { 0, 10, [0, 2, 4, 6, 8, 10] },
        { 1, 3, [1, 1.5, 2, 2.5, 3] },
        { 0.5, 3.5, [0.5, 1, 1.5, 2, 2.5, 3, 3.5] },
        // matplotlib's run goes to 6 and a tick past the view is not drawn.
        { 0, 5.1, [0, 1, 2, 3, 4, 5] },
        { -0.2, 1.2, [-0.2, 0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2] },
        { 1000, 1010, [1000, 1002, 1004, 1006, 1008, 1010] },
        { 0, 0.0007, [0, 0.0001, 0.0002, 0.0003, 0.0004, 0.0005, 0.0006, 0.0007] },
        { 2, 97, [20, 40, 60, 80] },
        { -3, 5.5, [-3, -2, -1, 0, 1, 2, 3, 4, 5] },
    };

    [Theory]
    [MemberData(nameof(Views))]
    public void Classic_TheTicksInView_AreMatplotlibsClassicAutoLocators(double lo, double hi, double[] expected) =>
        expected.AssertCloseTo(Ticks(new Range1D(lo, hi), Theme.MatplotlibClassic), relative: 1e-9, floor: 1);

    [Fact]
    public void Classic_ALengthOfThePlot_DoesNotChangeTheBins()
    {
        // nbins = 9 whatever the axis' length (v2's 'auto' reads it; the library's own placement thins the ticks of a short plot).
        (new double[] { -1, -0.5, 0, 0.5, 1 }).AssertCloseTo(Ticks(new Range1D(-1, 1), Theme.MatplotlibClassic, plotPixels: 60), relative: 1e-9, floor: 1);
    }

    [Fact]
    public void ARangeOfOneEndGivesTheTicksOfTheSameIntervalWhicheverWayItRuns() =>
        (new double[] { -1, -0.5, 0, 0.5, 1 }).AssertCloseTo(Ticks(new Range1D(1, -1), Theme.MatplotlibClassic), relative: 1e-9, floor: 1);

    [Fact]
    public void ALocatorOrASpacingOfTheAxis_StillWinsOverTheTheme()
    {
        var spaced = new Axis { MajorTicks = new TickConfig { Spacing = 0.25 } };
        (new double[] { -1, -0.75, -0.5, -0.25, 0, 0.25, 0.5, 0.75, 1 }).AssertCloseTo(spaced.TickValuesBetween(NoSource, new Range1D(-1, 1), 400, Theme.MatplotlibClassic, isX: true), relative: 1e-9, floor: 1);

        var located = new TickSource(new FixedLocator([0.1, 0.7]), null, false);
        (new double[] { 0.1, 0.7 }).AssertCloseTo(new Axis().TickValuesBetween(located, new Range1D(-1, 1), 400, Theme.MatplotlibClassic, isX: true), relative: 1e-9, floor: 1);
    }

    [Fact]
    public void TheLibrarysOwnThemes_KeepTheirOwnPlacement()
    {
        Assert.Null(Theme.Default.Looks.Ticks.Ladder);
        (new AutoLocator(8).Locate(-1, 1)).AssertCloseTo(Ticks(new Range1D(-1, 1), Theme.Default), relative: 1e-9, floor: 1);
    }

    // ── 3-D: the ticks the locator gives ─────────────────────────────────────────────────────────────────────────

    public static TheoryData<double, double, double[]> CubeViews { get; } = new()
    {
        { 0, 1, [0, 0.2, 0.4, 0.6, 0.8, 1.0] },
        { -1, 1, [-1, -0.5, 0, 0.5, 1] },
        { 0, 10, [0, 2, 4, 6, 8, 10] },
        { 0, 18, [0, 2, 4, 6, 8, 10, 12, 14, 16, 18] },
        { 1, 3, [1, 1.5, 2, 2.5, 3] },
        // Every value the locator gives, the ones past the view included: matplotlib formats them all and draws the ones in view.
        { -0.2292, 1.2292, [-0.4, -0.2, 0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2, 1.4] },
        { 0, 5.1, [0, 1, 2, 3, 4, 5, 6] },
    };

    [Theory]
    [MemberData(nameof(CubeViews))]
    public void Classic_TheTicksOfA3DAxis_AreMatplotlibsClassicAutoLocatorsWithItsRawStepOf23Over24(double lo, double hi, double[] expected) =>
        expected.AssertCloseTo(ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(new Axis(), new Range1D(lo, hi), Theme.MatplotlibClassic, axesWidth: 400), relative: 1e-9, floor: 1);

    [Fact]
    public void A3DAxisUnderAThemeWithNoLadder_KeepsTheLibrarysPlacement() =>
        (new double[] { 0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 }).AssertCloseTo(ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(new Axis(), new Range1D(0, 1), Theme.Default, axesWidth: 400), relative: 1e-9, floor: 1);

    // ── the rounding of the axis' ends and the ticks are one rule ───────────────────────────────────────────────

    [Fact]
    public void Classic_AnAxisRoundedOutTo18_HasATickAt18()
    {
        // 1 .. 17.5 rounds out to 0 .. 18 (matplotlib's view_limits); the classic ticks are every 2, so 18 is one. The library's
        // placement laid every 2.5 over 0 .. 18 and 18 was not a tick.
        var ladder = Theme.MatplotlibClassic.Looks.Ticks.Ladder!.Value;
        var rounded = ladder.RoundedOut(new Range1D(1, 17.5));

        Assert.Equal(0, rounded.Lo, 9);
        Assert.Equal(18, rounded.Hi, 9);
        Assert.Contains(18.0, Ticks(rounded, Theme.MatplotlibClassic));
    }

    [Fact]
    public void Classic_ALineChart_LabelsItsYAxisEveryHalf()
    {
        var svg = Plt.Create().WithSize(500, 400).WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new LineSeries([0.0, 1.0, 2.0], [-1.0, 0.0, 1.0]))).Build().ToSvg().Xml();

        var labels = svg.Each("text").Where(e => (string?)e.Attribute("text-anchor") == "end")
            .Select(e => e.Value.Replace('−', '-')).ToList();
        Assert.Equal(["-1.0", "-0.5", "0.0", "0.5", "1.0"], labels);
    }

    [Fact]
    public void ARoundingModeThatNamesNoLocator_TakesTheClassicOne()
    {
        var range = new Range1D(1, 17.5);

        var rounded = new AxisAutoscale(new Axis(), isX: true, Theme.Default).Autolimited(range, AutolimitRule.RoundNumbers, ladder: null, range, hasSticky: false);

        Assert.Equal(0, rounded.Lo, 9);
        Assert.Equal(18, rounded.Hi, 9);
    }

    // ── the theme carries it ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheClassicThemeCarriesTheLadder_AndAClone_AndTheJson_KeepIt()
    {
        var classic = Theme.MatplotlibClassic.Looks.Ticks.Ladder!.Value;
        Assert.Equal(9, classic.Bins);
        Assert.Equal(2, classic.MinTicks);
        Assert.Equal([1.0, 2.0, 5.0, 10.0], classic.Steps);

        var cloned = Theme.MatplotlibClassic.Cloned().Looks.Ticks.Ladder!.Value;
        Assert.Equal(classic.Steps, cloned.Steps);
        Assert.Equal(classic.Bins, cloned.Bins);

        var read = Theme.MatplotlibClassic.ThroughDto().Looks.Ticks.Ladder!.Value;
        Assert.Equivalent(classic, read);

        Assert.Null(Theme.Default.ThroughDto().Looks.Ticks.Ladder);
    }
}
