// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The minor ticks of a log axis stand where matplotlib 3.11.2's log scale places them, its minor
/// <c>LogLocator(subs='auto')</c> (<c>ticker.py</c>, <c>tick_values</c>): 2 to 9 times each power of ten from the one below the view,
/// none when ten or more powers stand in view (<c>n_avail &gt;= 10</c>) or when the major ticks stride over decades (the default
/// style's <c>stride = n_avail // (n_request + 1) + 1</c> with <c>n_request = clip(get_tick_space(), 2, 9)</c>; the classic mode asks
/// for 15 ticks, <c>numticks = 15</c>, and never strides below ten powers), and <c>AutoLocator</c>'s ticks when the view holds no more
/// than one major and minor tick together. The minor tick marks and the minor grid stand on them, a major tick's place left out
/// (<c>remove_overlapping_locs</c>); the minor locator belongs to the scale, so a major locator of the axis' own keeps them.</summary>
public sealed class LogMinorTicksTests
{
    private static readonly IMinorTickLocator Minor = new LogLocator();

    private static double[] Multiples(params int[] exponents) =>
        [.. exponents.SelectMany(e => Enumerable.Range(2, 8).Select(c => c * Math.Pow(10, e)))];

    [Fact]
    public void ALogAxisOverThreeDecades_HasTwoToNineTimesEachPower_FromThePowerBelowTheView()
    {
        // ax.plot([1, 10, 100, 1000], [1, 10, 100, 1000]); ax.set_yscale('log'): ylim 0.708 .. 1412, 26 minor ticks in view
        // (0.8, 0.9, 2 .. 9, 20 .. 90, 200 .. 900), tick space 13.
        var view = new Range1D(0.7079457843841379, 1412.5375446227545);

        ((IReadOnlyList<double>)[0.8, 0.9, .. Multiples(0, 1, 2)]).AssertCloseTo(Minor.MinorsOver(view, [1, 10, 100, 1000], 13, Theme.MatplotlibV2), relative: 1e-12, floor: 0);
    }

    [Fact]
    public void AReversedView_HasTheSameMinorTicks()
    {
        Multiples(0, 1, 2).AssertCloseTo(Minor.MinorsOver(new Range1D(1000, 1), [1, 10, 100, 1000], 13, Theme.MatplotlibV2), relative: 1e-12, floor: 0);
    }

    [Fact]
    public void UnderTheDefaultStyle_MajorsThatStrideOverDecades_HaveNoMinorTicks_ButTheClassicModeAsksForFifteen()
    {
        // A 1.2 inch tall figure over 1 .. 1e6: tick space 3 under the default style, majors 1e-3, 1, 1e3, 1e6, 1e9 and no minor
        // tick; tick space 2 under the classic style, every power a major and 2 .. 9 times each a minor (numticks = 15).
        var view = new Range1D(1, 1e6);

        Assert.Empty(Minor.MinorsOver(view, [1, 1e3, 1e6], 3, Theme.MatplotlibV2));
        Multiples(0, 1, 2, 3, 4, 5).AssertCloseTo(Minor.MinorsOver(view, [1, 10, 100, 1e3, 1e4, 1e5, 1e6], 2, Theme.MatplotlibClassic), relative: 1e-12, floor: 0);
        // A 4.8 inch tall one: tick space 13, one tick a decade, the multiples.
        Multiples(0, 1, 2, 3, 4, 5).AssertCloseTo(Minor.MinorsOver(view, [1, 10, 100, 1e3, 1e4, 1e5, 1e6], 13, Theme.MatplotlibV2), relative: 1e-12, floor: 0);
    }

    [Fact]
    public void TenPowersInView_HaveNoMinorTicks()
    {
        Assert.Empty(Minor.MinorsOver(new Range1D(1, 1e9), [1, 10, 100, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9], 13, Theme.MatplotlibV2));
    }

    [Fact]
    public void ANarrowView_LeavesOutTheMultiplesItsLabelsNameAsMajors()
    {
        // set_ylim(2, 80): the major 10, the minor ticks 2 .. 9 and 20 .. 80; the library names 2, 3, 4, 6, 10, 20, 30, 40 and 60
        // among its majors, so the minor marks stand on the rest.
        double[] majors = new LogLocator().Locate(2, 80);

        (new double[] { 5, 7, 8, 9, 50, 70, 80 }).AssertCloseTo(Minor.MinorsOver(new Range1D(2, 80), majors, 13, Theme.MatplotlibV2), relative: 1e-12, floor: 0);
    }

    [Fact]
    public void AViewOfOneTickOrNone_TakesAutoLocatorsTicks_ThatAreNotMajors()
    {
        // set_ylim(2.5, 3.5): AutoLocator's 2.6, 2.8, 3.0, 3.2, 3.4, every one of them named.
        double[] majors = new LogLocator().Locate(2.5, 3.5);

        Assert.Equal(5, majors.Length);
        Assert.Empty(Minor.MinorsOver(new Range1D(2.5, 3.5), majors, 13, Theme.MatplotlibV2));
        (new double[] { 2.6, 2.8, 3.0, 3.2, 3.4 }).AssertCloseTo(Minor.MinorsOver(new Range1D(2.5, 3.5), [], 13, Theme.MatplotlibV2), relative: 1e-12, floor: 0);
    }

    [Fact]
    public void AViewThatHasNoPlaceOnALogScale_HasNoMinorTicks()
    {
        Assert.Empty(Minor.MinorsOver(new Range1D(-5, 0), [], 13, Theme.MatplotlibV2));
        Assert.Empty(Minor.MinorsOver(new Range1D(double.NaN, 10), [], 13, Theme.MatplotlibV2));
        Assert.Empty(Minor.MinorsOver(new Range1D(1, double.PositiveInfinity), [], 13, Theme.MatplotlibV2));
    }

    [Fact]
    public void ATickSpaceThatIsNotANumber_IsTakenAsNine_AsALocatorWithoutAnAxisIs()
    {
        // numticks 'auto' with no axis: n_request 9. Over nine powers that keeps one tick a decade: the multiples stand.
        var view = new Range1D(1, 1e8);

        Minor.MinorsOver(view, [], 9, Theme.MatplotlibV2).AssertCloseTo(Minor.MinorsOver(view, [], double.NaN, Theme.MatplotlibV2), relative: 1e-12, floor: 0);
        Minor.MinorsOver(view, [], 9, Theme.MatplotlibV2).AssertCloseTo(Minor.MinorsOver(view, [], double.PositiveInfinity, Theme.MatplotlibV2), relative: 1e-12, floor: 0);
        Assert.Equal(64, Minor.MinorsOver(view, [], double.NaN, Theme.MatplotlibV2).Length);
    }

    /// <summary>A placement that draws its ticks on a log scale but shows no view (a link that cannot place the primary's range).</summary>
    private sealed class NoView : ITickPlacement
    {
        public double? OnPlot(double value, bool alongX) => null;
        public ScaleRule ScaleAlong(bool alongX) => ScaleRule.Log;
        public Range1D? ViewAlong(bool alongX) => null;
    }

    [Fact]
    public void ARunThatShowsNoView_HasNoScaleMinors_AndALinearRunNone()
    {
        Assert.Null(new TickRun([1.0, 10.0], new NoView(), EdgeTerms.Bottom).ScaleMinors(13, Theme.MatplotlibV2));
        Assert.Null(new TickRun([0.0, 1.0], new DataTransform(0, 1, 0, 1, new Rect(0, 0, 100, 100)), EdgeTerms.Bottom)
            .ScaleMinors(13, Theme.MatplotlibV2));
    }

    // ── on the axes ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static bool IsBlue(Color c) => c.R == Colors.Blue.R && c.G == Colors.Blue.G && c.B == Colors.Blue.B;

    private static bool IsRed(Color c) => c.R == Colors.Red.R && c.G == Colors.Red.G && c.B == Colors.Red.B;

    private static List<(Point P1, Point P2)> Lines(Figure figure, Func<Color, bool> colour)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return [.. ctx.Of<DrawLineCall>()
            .Where(a => colour(a.Color))
            .Select(a => (a.P1, a.P2))];
    }

    private static Figure LogMinorGrid(Theme theme) => Plt.Create()
        .WithSize(600, 400)
        .WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 10, 100, 1000], [1.0, 10, 100, 1000])
            .SetYScale(AxisScale.Log)
            .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, Which = GridWhich.Minor, Color = Colors.Blue }))
        .Build();

    /// <summary>matplotlib 3.11.2 draws 26 minor grid lines in view under the default style (ylim 0.708 .. 1412; the five-way split of
    /// the first major step drew 557, crowded at the top); the classic style rounds the view out to 1 .. 1000 and has 24 minor ticks in it.</summary>
    [Theory]
    [InlineData("v2", 26)]
    [InlineData("classic", 24)]
    public void TheMinorGridOfALogAxis_StandsOnItsMinorTicks(string theme, int ticks) =>
        Assert.Equal(ticks, Lines(LogMinorGrid(theme.AsTheme()), IsBlue).Count);

    [Fact]
    public void UnderTheLibrarysTheme_TheMinorGridOfALogAxis_StandsOnTheSameMultiples()
    {
        // The library's theme pads the view as v2 does: the same 26 places, not a linear split of a decade.
        var under = Lines(LogMinorGrid(Theme.MatplotlibV2), IsBlue).Select(l => l.P1.Y).Order().ToArray();
        var library = Lines(LogMinorGrid(Theme.Default), IsBlue).Select(l => l.P1.Y).Order().ToArray();

        Assert.Equal(26, library.Length);
        Assert.Equal(under.Length, library.Length);
    }

    [Fact]
    public void ALogAxisGivenAMajorLocatorOfItsOwn_KeepsTheMinorTicksOfItsScale()
    {
        // ax.yaxis.set_major_locator(FixedLocator([1, 30, 1000])) leaves the minor LogLocator in place (printed: 39 minor ticks).
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 1000], [1.0, 1000]).SetYScale(AxisScale.Log).SetYLim(1, 1000)
                .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, Which = GridWhich.Minor, Color = Colors.Blue }))
            .Build();
        figure.SubPlots[0].YAxis.TickLocator = new FixedLocator([1.0, 30, 1000]);

        // 2 .. 9, 20 .. 90 and 200 .. 900 in view, 30 a major.
        Assert.Equal(23, Lines(figure, IsBlue).Count);
    }

    [Fact]
    public void ATopAxisLinkedToALogPrimary_HasTheMinorTicksOfALogScale_InItsOwnUnit()
    {
        // secondary_xaxis over a log X axis is drawn on matplotlib's functionlog scale, whose minor locator is LogLocator(subs='auto').
        // Linked one to one, its minor marks stand where the primary's stand.
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 1000], [0.0, 1.0]).SetXScale(AxisScale.Log)
                .WithSecondaryXAxis(s => s.Link(AxisTransform.Linear(1))))
            .Build();
        var axes = figure.SubPlots[0];
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = true, Color = Colors.Blue };
        axes.SecondaryXAxis!.MinorTicks = axes.SecondaryXAxis.MinorTicks with { Visible = true, Color = Colors.Red };

        var primary = Lines(figure, IsBlue).Select(l => Math.Round(l.P1.X, 6)).Order().ToArray();
        var linked = Lines(figure, IsRed).Select(l => Math.Round(l.P1.X, 6)).Order().ToArray();

        Assert.Equal(26, primary.Length);
        Assert.Equal(primary, linked);
    }
}
