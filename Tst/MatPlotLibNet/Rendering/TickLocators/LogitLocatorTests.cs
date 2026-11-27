// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The ticks and labels of a logit axis, read from matplotlib 3.11.2's <c>LogitLocator</c> and <c>LogitFormatter</c>
/// (<c>inspect.getsource</c>) and printed with <c>ax.set_yscale('logit'); ax.set_ylim(lo, hi)</c> in a default figure, whose Y
/// axis has room for 13 labels (<c>get_tick_space</c>; 14 under classic). The major locator lays the ideal ticks
/// (…, 10⁻², 10⁻¹, ½, 1−10⁻¹, 1−10⁻², …) from <c>binf</c> to <c>bsup</c>, every <c>ceil(numideal / nbins)</c>-th when there are more
/// than <c>nbins</c>, and <c>MaxNLocator(steps=[1, 2, 5, 10])</c>'s ticks when fewer than two are in view; the minor locator lays
/// the multiples between them (or the skipped ideals); the minor formatter names six of fewer than 25 minor ticks, the ones with
/// the most room, and the library draws a named minor tick among the majors.</summary>
public sealed class LogitLocatorTests
{
    public static TheoryData<double, double, double[]> DefaultStyleViews { get; } = new()
    {
        // The plot of 0.09 .. 0.9: majors 10^-2 .. 1-10^-2, minors named 0.2, 0.3, 0.4, 0.6, 0.7, 0.8.
        { 0.07315689602168207, 0.918547048311118, [0.01, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 0.99] },
        // Majors 10^-3 .. 1/2; of 19 minors 2x10^-3, 2x10^-2, 3x10^-2, 0.2, 0.3, 0.4 are named.
        { 0.005, 0.2, [0.001, 0.002, 0.01, 0.02, 0.03, 0.1, 0.2, 0.3, 0.4, 0.5] },
        // Twelve ideal ticks, 78 minors: none named.
        { 1e-5, 1 - 1e-5, [1e-5, 1e-4, 1e-3, 0.01, 0.1, 0.5, 0.9, 0.99, 0.999, 0.9999, 0.99999, 0.999999] },
        // Nineteen ideal ticks for 13 labels: every other one.
        { 1e-9, 1 - 1e-9, [1e-8, 1e-6, 1e-4, 0.01, 0.5, 0.99, 0.9999, 0.999999, 0.99999999, 0.9999999999] },
        // Zoomed: MaxNLocator's ticks, a step past either end.
        { 0.1860501741024006, 0.621296084688531, [0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65] },
        { 0.02, 0.08, [0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08] },
        { 0.92, 0.98, [0.92, 0.93, 0.94, 0.95, 0.96, 0.97, 0.98] },
        { 0.1, 0.5, [0.1, 0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5] },
    };

    [Theory]
    [MemberData(nameof(DefaultStyleViews))]
    public void ALogitLocator_LaysTheTicksMatplotlibLabels(double lo, double hi, double[] expected) =>
        expected.AssertCloseTo(new LogitLocator().LocateWithSpace(lo, hi, 13, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);

    [Fact]
    public void UnderTheClassicStyle_AZoomedLogitAxis_TakesAStepWhoseBinsReachTheEnd()
    {
        // Classic, set_ylim(0.186.., 0.621..): 10^-1, 0.2, .., 0.7 (round_numbers asks for a step from which nine bins reach the top).
        (new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7 }).AssertCloseTo(new LogitLocator().LocateWithSpace(0.1860501741024006, 0.621296084688531, 14, Theme.MatplotlibClassic), relative: 1e-12, floor: 1e-9);
    }

    [Fact]
    public void WithRoomForFourLabels_TheIdealTicksAreSubsampled()
    {
        // LogitLocator(nbins=4).tick_values(1e-5, 1 - 1e-5): 10^-3, 1/2, 1-10^-3, 1-10^-6.
        (new double[] { 0.001, 0.5, 0.999, 0.999999 }).AssertCloseTo(new LogitLocator().LocateWithSpace(1e-5, 1 - 1e-5, 4, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);
    }

    [Fact]
    public void WithoutAnAxis_TheLocatorTakesNineLabels_AndAReversedViewIsItsInterval()
    {
        ITickLocator locator = new LogitLocator();

        (new LogitLocator().LocateWithSpace(1e-5, 1 - 1e-5, 9, Theme.Default)).AssertCloseTo(locator.Locate(1 - 1e-5, 1e-5), relative: 1e-12, floor: 1e-9);
    }

    [Fact]
    public void AViewAtOrPastZeroAndOne_OrWithNoWidth_IsTakenToWhereTheScaleHasRoom()
    {
        var locator = new LogitLocator();

        locator.LocateWithSpace(1e-7, 1 - 1e-7, 13, Theme.MatplotlibV2).AssertCloseTo(locator.LocateWithSpace(-1, 2, 13, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);
        locator.LocateWithSpace(1e-7, 1 - 1e-7, 13, Theme.MatplotlibV2).AssertCloseTo(locator.LocateWithSpace(2, 3, 13, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);
        locator.LocateWithSpace(1e-7, 1 - 1e-7, 13, Theme.MatplotlibV2).AssertCloseTo(locator.LocateWithSpace(double.NaN, 0.5, 13, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);
        // nonsingular: vmin == vmax opens to 0.1 * vmin .. 1 - 0.1 * vmin.
        locator.LocateWithSpace(0.03, 0.97, 13, Theme.MatplotlibV2).AssertCloseTo(locator.LocateWithSpace(0.3, 0.3, 13, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);
        locator.LocateWithSpace(1e-7, 1 - 1e-7, double.PositiveInfinity, Theme.MatplotlibV2)[..3].AssertCloseTo([1e-7, 1e-6, 1e-5], relative: 1e-12, floor: 1e-9);
    }

    // ── minor ticks ─────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheMinorTicks_AreTheMultiplesBetweenTheIdealTicks_LessTheNamedOnes()
    {
        // 22 minors over the plot of 0.09 .. 0.9, of which 0.2 .. 0.8 are named and stand among the majors.
        var locator = new LogitLocator();
        var view = new Range1D(0.07315689602168207, 0.918547048311118);
        var majors = locator.LocateWithSpace(view.Lo, view.Hi, 13, Theme.MatplotlibV2);

        (new double[] { 0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08, 0.09, 0.91, 0.92, 0.93, 0.94, 0.95, 0.96, 0.97, 0.98 }).AssertCloseTo(locator.MinorsOver(view, majors, 13, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);
    }

    [Fact]
    public void WhenTheIdealTicksAreSubsampled_TheMinorTicksAreTheSkippedOnes()
    {
        // LogitLocator(minor=True) over 1e-9 .. 1-1e-9 with 13 labels.
        var locator = new LogitLocator();
        var view = new Range1D(1e-9, 1 - 1e-9);

        (new double[] { 1e-9, 1e-7, 1e-5, 0.001, 0.1, 0.9, 0.999, 0.99999, 0.9999999, 0.999999999 }).AssertCloseTo(locator.MinorsOver(view, locator.LocateWithSpace(view.Lo, view.Hi, 13, Theme.MatplotlibV2), 13, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);
        // nbins = 4 over 1e-5 .. 1-1e-5: 1e-5, 1e-4, 0.01, 0.1, 0.9, 0.99, 0.9999, 0.99999.
        var narrow = new Range1D(1e-5, 1 - 1e-5);
        (new double[] { 1e-5, 1e-4, 0.01, 0.1, 0.9, 0.99, 0.9999, 0.99999 }).AssertCloseTo(locator.MinorsOver(narrow, locator.LocateWithSpace(narrow.Lo, narrow.Hi, 4, Theme.MatplotlibV2), 4, Theme.MatplotlibV2), relative: 1e-12, floor: 1e-9);
    }

    [Fact]
    public void AZoomedView_HasNoMinorTicks()
    {
        var locator = new LogitLocator();

        Assert.Empty(locator.MinorsOver(new Range1D(0.2, 0.6), locator.LocateWithSpace(0.2, 0.6, 13, Theme.MatplotlibV2), 13, Theme.MatplotlibV2));
    }

    [Fact]
    public void WithNothingBoundingTheLabels_EveryIdealTickIsAMajor_AndTheMultiplesAreTheMinors()
    {
        // An infinite tick space takes every ideal tick (nbins unbounded): no ideal is skipped, so the minors are the multiples.
        var locator = new LogitLocator();
        var view = new Range1D(1e-9, 1 - 1e-9);
        double[] majors = locator.LocateWithSpace(view.Lo, view.Hi, double.PositiveInfinity, Theme.MatplotlibV2);

        double[] minors = locator.MinorsOver(view, majors, double.PositiveInfinity, Theme.MatplotlibV2);

        Assert.Contains(0.5, majors);
        Assert.Contains(minors, m => Math.Abs(m - 2e-9) < 1e-20);
        Assert.DoesNotContain(minors, majors.Contains);
    }

    // ── labels ──────────────────────────────────────────────────────────────────────────────────────────────────────

    public static TheoryData<double, double, string[]> Labels { get; } = new()
    {
        { 0.07315689602168207, 0.918547048311118, ["10⁻²", "10⁻¹", "0.2", "0.3", "0.4", "½", "0.6", "0.7", "0.8", "1−10⁻¹", "1−10⁻²"] },
        { 0.1860501741024006, 0.621296084688531, ["0.15", "0.20", "0.25", "0.30", "0.35", "0.40", "0.45", "½", "0.55", "0.60", "0.65"] },
        { 0.02, 0.08, ["2⋅10⁻²", "3⋅10⁻²", "4⋅10⁻²", "5⋅10⁻²", "6⋅10⁻²", "7⋅10⁻²", "8⋅10⁻²"] },
        // 1 - 0.97 and 1 - 0.98 of MaxNLocator's ticks carry a rounding error that asks for a digit more (printed so).
        { 0.92, 0.98, ["1−8⋅10⁻²", "1−7⋅10⁻²", "1−6⋅10⁻²", "1−5⋅10⁻²", "1−4⋅10⁻²", "1−3.0⋅10⁻²", "1−2.0⋅10⁻²"] },
        { 0.005, 0.2, ["10⁻³", "2⋅10⁻³", "10⁻²", "2⋅10⁻²", "3⋅10⁻²", "10⁻¹", "0.2", "0.3", "0.4", "½"] },
        { 0.1, 0.5, ["10⁻¹", "0.15", "0.20", "0.25", "0.30", "0.35", "0.40", "0.45", "½"] },
    };

    [Theory]
    [MemberData(nameof(Labels))]
    public void TheLogitFormatter_WritesWhatMatplotlibsLogitFormatterWrites(double lo, double hi, string[] expected)
    {
        double[] ticks = new LogitLocator().LocateWithSpace(lo, hi, 13, Theme.MatplotlibV2);
        var formatter = new LogitFormatter().ForTicks(ticks, null, null, Theme.MatplotlibV2);

        Assert.Equal(expected, ticks.Select(formatter.Format).ToArray());
    }

    [Fact]
    public void AValueAtOrPastZeroAndOne_HasNoLabel_AndAValueAloneTakesTheFewestDigits()
    {
        var formatter = new LogitFormatter();

        Assert.Equal("", formatter.Format(0));
        Assert.Equal("", formatter.Format(1));
        Assert.Equal("", formatter.Format(double.NaN));
        Assert.Equal("0.3", formatter.Format(0.3));
        Assert.Equal("3⋅10⁻²", formatter.Format(0.03));
        Assert.Equal("1−3⋅10⁻²", formatter.Format(0.97));
        // A neighbour far off asks for fewer digits than none; matplotlib keeps none (set_locs([0.02, 0.5]): 2\cdot10^{-2}).
        Assert.Equal("2⋅10⁻²", formatter.ForTicks([0.02, 0.5], null, null, Theme.MatplotlibV2).Format(0.02));
    }

    // ── the axis ────────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALogitAxis_TakesTheLogitLocator_TellingItHowManyLabelsFit()
    {
        // A default figure's Y axis: 369.6 pixels, labels 10 pt (13.89 px), two label heights a tick: 13 fit.
        var axis = new Axis { Scale = AxisScale.Logit };
        var source = axis.OwnTickSource(isX: false, Theme.Default);

        Assert.IsType<LogitLocator>(source.Locator);
        Assert.IsType<LogitFormatter>(source.Formatter);
        Assert.True(axis.Scale.Rule.BringsLocator(isX: false));
        (new double[] { 1e-8, 1e-6, 1e-4, 0.01, 0.5, 0.99, 0.9999, 0.999999, 0.99999999, 0.9999999999 }).AssertCloseTo(axis.TickValuesBetween(source, new Range1D(1 - 1e-9, 1e-9), 369.6, Theme.MatplotlibV2, isX: false), relative: 1e-12, floor: 1e-9);
    }

    [Fact]
    public void UnderAMatplotlibTheme_ALogitAxisDrawsItsMinorGrid_OnItsMinorTicksInView()
    {
        // Minor ticks in view over 0.073 .. 0.919: 0.08, 0.09 and 0.91 (0.2 .. 0.8 are named and stand among the majors).
        var axes = new Axes();
        axes.YAxis.Scale = AxisScale.Logit;
        var range = new DataRange(0, 1, 0.07315689602168207, 0.918547048311118);
        var transform = DataTransform.Of(range, new Rect(0, 0, 400, 300), new AxisMapping(ScaleRule.Linear, []),
            new AxisMapping(ScaleRule.Logit, []));
        double[] majors = new LogitLocator().LocateWithSpace(range.YMin, range.YMax, 13, Theme.MatplotlibV2);
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(new GridStyle { Visible = true, Axis = GridAxis.Y, Which = GridWhich.Minor }, axes, [], majors, transform, Theme.MatplotlibV2);

        Assert.Equal(3, ctx.Count<DrawLineCall>());
    }

    [Fact]
    public void AMinorSpacingOfTheAxis_WinsOverTheMinorTicksOfItsScale()
    {
        var spaced = new TickConfig { Spacing = 0.25 };
        var walked = new List<double>();
        foreach (double v in Theme.MatplotlibV2.Looks.Ticks.MinorValuesBetween(spaced, [0.0, 1.0], [0.3])) walked.Add(v);
        var split = new List<double>();
        foreach (double v in Theme.MatplotlibV2.Looks.Ticks.MinorValuesBetween(spaced, [0.0, 1.0])) split.Add(v);

        Assert.Equal(split, walked);
        Assert.DoesNotContain(0.3, walked);
        var given = new List<double>();
        foreach (double v in Theme.MatplotlibV2.Looks.Ticks.MinorValuesBetween(new TickConfig(), [0.0, 1.0], [0.5])) given.Add(v);
        Assert.Equal([0.5], given);
    }

    [Fact]
    public void ARangeTakenInsideZeroAndOne_KeepsItsOrientation_AndOpensWhenItHasNoWidth()
    {
        Assert.Equal(new Range1D(0.91, 0.09), ScaleRule.Logit.Limited(new Range1D(1, 0), 0.09));
        Assert.Equal(new Range1D(0.03, 0.97), ScaleRule.Logit.Limited(new Range1D(0.3, 0.3), 1e-7));
        Assert.Equal(new Range1D(0.2, 0.7), ScaleRule.Logit.Limited(new Range1D(0.2, 0.7), 1e-7));
        var logit = new Axis { Scale = AxisScale.Logit };
        Assert.Equal(new Range1D(1e-7, 1 - 1e-7), new AxisAutoscale(logit, isX: false, Theme.Default).OnScale(new Range1D(0, 1), null));
        Assert.Equal(new Range1D(0, 1), new AxisAutoscale(new Axis(), isX: false, Theme.Default).OnScale(new Range1D(0, 1), null));
    }
}
