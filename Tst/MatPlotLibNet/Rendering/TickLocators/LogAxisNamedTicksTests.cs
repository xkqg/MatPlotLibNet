// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The ticks a log axis names, by the rule a colour bar of matplotlib's log norm names them by. matplotlib 3.11.2: a log
/// scale has a major <c>LogLocator</c> (the powers of ten), a minor <c>LogLocator(subs='auto')</c> (2 to 9 times them; when they and
/// the powers in view are one or none it returns <c>AutoLocator().tick_values</c> instead) and a <c>LogFormatterSciNotation</c> on
/// both, whose <c>set_locs</c> labels by <c>minor_thresholds</c> ((1, 0.4) in the default style, (0, 0) in classic mode): more powers of
/// ten in view than the first, only the powers; a view wider than the second (in decades), the multiples whose coefficient rounds to
/// 1, 2, 3, 4 or 6; otherwise every one. Printed with <c>ax.set_yscale('log'); ax.set_ylim(lo, hi)</c>, the labelled ticks in view:
/// default (2, 8) 2, 3, 4 and 6 x 10^0; (0.5, 3) 6 x 10^-1, 10^0, 2 and 3 x 10^0; (100, 947) 10^2 and 2, 3, 4, 6 x 10^2; (1, 5) 10^0, 2,
/// 3, 4 x 10^0; (2.1, 2.9) every 0.1 from 2.1 x 10^0; (2, 2.6) every 0.1 from 2 x 10^0; (3, 450) 10^1, 10^2; classic (0.5, 3) 10^0 alone,
/// (100, 947) 10^2 alone, (2, 8) as the default. The library named the powers alone and, with none in view, only the view's low end
/// (one tick, "2" over 2 .. 8), writing a value off a power in four significant digits.</summary>
public sealed class LogAxisNamedTicksTests
{
    public static TheoryData<double, double, double[]> DefaultStyleViews { get; } = new()
    {
        { 2, 8, [2, 3, 4, 6] },
        { 0.5, 3, [0.6, 1, 2, 3] },
        { 100, 947, [100, 200, 300, 400, 600] },
        { 1, 5, [1, 2, 3, 4] },
        { 2.1, 2.9, [2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9] },
        { 2, 2.6, [2, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6] },
        { 3, 450, [10, 100] },
        { 1, 1000, [1, 10, 100, 1000] },
    };

    [Theory]
    [MemberData(nameof(DefaultStyleViews))]
    public void ALogLocator_NamesTheTicksMatplotlibLabels(double lo, double hi, double[] expected) =>
        expected.AssertCloseTo(new LogLocator().Locate(lo, hi), relative: 1e-9, floor: 1);

    public static TheoryData<double, double, double[]> ClassicViews { get; } = new()
    {
        { 0.5, 3, [1] },
        { 100, 947, [100] },
        { 2, 8, [2, 3, 4, 6] },
        { 1, 1000, [1, 10, 100, 1000] },
        // AutoLocator's ticks, of which those whose coefficient rounds to 1, 2, 3, 4 or 6 (printed: 4.2 .. 4.5; 4.6 rounds to 5).
        { 4.2, 4.9, [4.2, 4.3, 4.4, 4.5] },
        // One power in view: the powers alone, of AutoLocator's ticks the ones whose coefficient rounds to 1 (printed: 10^0, 1.01 ..
        // 1.03 x 10^0; 0.97 .. 0.99 round to 10).
        { 0.97, 1.03, [1, 1.01, 1.02, 1.03] },
    };

    [Fact]
    public void UnderTheDefaultTheme_ANarrowViewAroundAPower_NamesEveryOneOfAutoLocatorsTicks()
    {
        // Printed: 9.7, 9.8, 9.9 x 10^-1, 10^0, 1.01, 1.02, 1.03 x 10^0.
        var axis = new Axis { Scale = AxisScale.Log };

        (new double[] { 0.97, 0.98, 0.99, 1, 1.01, 1.02, 1.03 }).AssertCloseTo(axis.TickValuesBetween(axis.OwnTickSource(isX: false, Theme.Default), new Range1D(0.97, 1.03), 400, Theme.MatplotlibV2, isX: false), relative: 1e-9, floor: 1);
    }

    [Fact]
    public void ARangeThatReachesZero_OrHasNoWidth_IsLocatedAsBefore()
    {
        ITickLocator log = new LogLocator();
        var spaced = (ITickSpaceLocator)log;

        Assert.Equal(log.Locate(-1, 100), spaced.LocateWithSpace(-1, 100, 13, Theme.MatplotlibV2));
        Assert.True(spaced.LocateWithSpace(-1, 100, 13, Theme.MatplotlibV2)[0] == 1e-10);
        Assert.Equal([5.0], spaced.LocateWithSpace(5, 5, 13, Theme.MatplotlibV2));
        Assert.Equal([5.0], log.Locate(5, 5));
    }

    [Theory]
    [MemberData(nameof(ClassicViews))]
    public void UnderTheClassicTheme_ALogAxisNamesTheTicksOfItsThresholds(double lo, double hi, double[] expected)
    {
        var axis = new Axis { Scale = AxisScale.Log };

        expected.AssertCloseTo(axis.TickValuesBetween(axis.OwnTickSource(isX: false, Theme.Default), new Range1D(lo, hi), 400, Theme.MatplotlibClassic, isX: false), relative: 1e-9, floor: 1);
    }

    [Fact]
    public void UnderTheDefaultTheme_ALogAxisNamesTheMultiples()
    {
        var axis = new Axis { Scale = AxisScale.Log };

        (new double[] { 0.6, 1, 2, 3 }).AssertCloseTo(axis.TickValuesBetween(axis.OwnTickSource(isX: false, Theme.Default), new Range1D(0.5, 3), 400, Theme.MatplotlibV2, isX: false), relative: 1e-9, floor: 1);
    }

    [Theory]
    [InlineData(2.0, "2×10⁰")]
    [InlineData(600.0, "6×10²")]
    [InlineData(0.6, "6×10⁻¹")]
    [InlineData(2.5, "2.5×10⁰")]
    [InlineData(1000.0, "10³")]
    public void TheLogFormatter_WritesAValueOffAPower_AsMatplotlibsLogFormatterSciNotation(double value, string expected) =>
        Assert.Equal(expected, new LogTickFormatter().Format(value));

    [Fact]
    public void ALogYAxisOverTwoToEight_LabelsTwoThreeFourAndSix()
    {
        // The repro: one Y tick text, "2", where matplotlib labels 2, 3, 4 and 6 x 10^0.
        var svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [2.0, 5.0, 8.0]).SetYScale(AxisScale.Log).SetYLim(2, 8)).Build().ToSvg().Xml();

        var labels = svg.Each("text").Where(e => (string?)e.Attribute("text-anchor") == "end")
            .Select(e => e.Value).ToList();
        Assert.Equal(["2×10⁰", "3×10⁰", "4×10⁰", "6×10⁰"], labels);
    }

    // ── one rule for the axis and the colour bar ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AClassicColourBarOfALogNorm_NamesThePowerAlone_AsMatplotlibsClassicColorbarDoes()
    {
        // fig.colorbar of LogNorm(0.5, 3) under plt.style.use('classic'): 10^0 alone (minor_thresholds (0, 0)).
        var scale = ColorBarScale.For(0.5, 3, 300, vertical: true, LogNorm.Instance, new RecordingRenderContext(),
            Theme.MatplotlibClassic.TickFont(), Theme.MatplotlibClassic);

        Assert.Equal([1.0], scale.Ticks);
        Assert.Equal(["10⁰"], scale.Labels);
    }

    [Fact]
    public void TheClassicThemeCarriesItsThresholds_AndAClone_AndTheJson_KeepThem()
    {
        Assert.Equal(new LogLabelThresholds(0, 0), Theme.MatplotlibClassic.Looks.TickLabels.LogThresholds);
        Assert.Null(Theme.MatplotlibV2.Looks.TickLabels.LogThresholds);
        Assert.Null(Theme.Default.Looks.TickLabels.LogThresholds);
        Assert.Equal(new LogLabelThresholds(0, 0), Theme.MatplotlibClassic.Cloned().Looks.TickLabels.LogThresholds);
        Assert.Equal(new LogLabelThresholds(0, 0), Theme.MatplotlibClassic.ThroughDto().Looks.TickLabels.LogThresholds);
        Assert.Null(Theme.MatplotlibV2.ThroughDto().Looks.TickLabels.LogThresholds);
    }
}
