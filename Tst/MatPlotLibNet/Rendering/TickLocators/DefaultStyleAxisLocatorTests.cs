// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The ticks the default style (the matplotlib v2 theme) places on an axis that has no locator or spacing of its own are
/// matplotlib's <c>AutoLocator</c>'s. matplotlib 3.11.2, read with inspect: <c>AutoLocator.__init__</c> outside classic mode sets
/// <c>nbins = 'auto'</c> and <c>steps = [1, 2, 2.5, 5, 10]</c>; <c>MaxNLocator._raw_ticks</c> then takes
/// <c>nbins = np.clip(self.axis.get_tick_space(), max(1, self._min_n_ticks - 1), 9)</c> and the smallest step of the staircase at or
/// above the range over the bins ("Start at smallest of the steps greater than the raw step"). <c>XAxis.get_tick_space</c>:
/// <c>length = ends.width * 72; size = self._get_tick_label_size('x') * 3; return int(np.floor(length / size))</c>;
/// <c>YAxis.get_tick_space</c> the same over the height with <c>* 2</c>; <c>_get_tick_label_size</c> reads the axis' own
/// <c>labelsize</c>, else <c>xtick.labelsize</c> (10 pt in the default style). <c>axes.autolimit_mode</c> is <c>'data'</c>, so no
/// step is lengthened to reach the end of the range. Every expected list was printed by matplotlib under
/// <c>plt.style.use('default')</c> for the same view: a default figure (6.4 x 4.8 in at 100 dpi) has an axes 496 px wide
/// (tick space 11) and 369.6 px high (13), so nine bins. The library placed eight intervals by the nearest step: 0 to 9 over
/// 0 to 9.3, a step of 0.05 over 0 to 0.46.</summary>
public sealed class DefaultStyleAxisLocatorTests
{
    private static readonly TickSource NoSource = new(null, null, false);

    // matplotlib's default axes in a 6.4 x 4.8 in figure at 100 dpi: 0.775 x 0.77 of it.
    private const double DefaultWidth = 496, DefaultHeight = 369.6;

    // xtick.labelsize 'medium' = font.size 10 pt, in pixels.
    private static readonly double V2LabelPx = 10.0 * 100 / 72;

    private static double[] Ticks(Range1D view, Theme theme, double length, bool isX, Axis? axis = null) =>
        (axis ?? new Axis()).TickValuesBetween(NoSource, view, length, theme, isX);

    // ── the ticks in view at the default size ─────────────────────────────────────────────────────────────────────

    public static TheoryData<double, double, double[]> Views { get; } = new()
    {
        { 0, 9.3, [0, 2, 4, 6, 8] },
        { 0, 0.46, [0, 0.1, 0.2, 0.3, 0.4] },
        { -1, 1, [-1, -0.75, -0.5, -0.25, 0, 0.25, 0.5, 0.75, 1] },
        { 0, 1, [0, 0.2, 0.4, 0.6, 0.8, 1] },
        { 0, 18, [0, 2, 4, 6, 8, 10, 12, 14, 16, 18] },
        { 1, 3, [1, 1.25, 1.5, 1.75, 2, 2.25, 2.5, 2.75, 3] },
        { 1000, 1010, [1000, 1002, 1004, 1006, 1008, 1010] },
        { 2, 97, [20, 40, 60, 80] },
    };

    [Theory]
    [MemberData(nameof(Views))]
    public void V2_TheXTicksInView_AreMatplotlibsAutoLocators(double lo, double hi, double[] expected) =>
        expected.AssertCloseTo(Ticks(new Range1D(lo, hi), Theme.MatplotlibV2, DefaultWidth, isX: true), relative: 1e-9, floor: 1);

    [Theory]
    [MemberData(nameof(Views))]
    public void V2_TheYTicksInView_AreMatplotlibsAutoLocators(double lo, double hi, double[] expected) =>
        expected.AssertCloseTo(Ticks(new Range1D(lo, hi), Theme.MatplotlibV2, DefaultHeight, isX: false), relative: 1e-9, floor: 1);

    // ── the bins follow the axis' length and its label size ──────────────────────────────────────────────────────

    public static TheoryData<double, bool, double[]> Lengths { get; } = new()
    {
        // figsize (3, 2): X space 5, Y space 5.
        { 232.5, true, [0, 2, 4, 6, 8] },
        { 154, false, [0, 2, 4, 6, 8] },
        // figsize (2, 1.5): X space 3, Y space 4.
        { 155, true, [0, 5] },
        { 115.5, false, [0, 2.5, 5, 7.5] },
        // figsize (1.6, 1.2): X space 2, Y space 3.
        { 124, true, [0, 5] },
        { 92.4, false, [0, 5] },
    };

    [Theory]
    [MemberData(nameof(Lengths))]
    public void V2_AShorterAxis_AsksForFewerBins_AsMatplotlibsTickSpaceSays(double length, bool isX, double[] expected) =>
        expected.AssertCloseTo(Ticks(new Range1D(0, 9.3), Theme.MatplotlibV2, length, isX), relative: 1e-9, floor: 1);

    [Fact]
    public void V2_TheAxisOwnLabelSize_CountsAsMatplotlibsLabelSizeDoes()
    {
        // ax.tick_params(axis='x', labelsize=20): get_tick_space 5, ticks -1, -0.5, 0, 0.5, 1 over -1 .. 1.
        var axis = new Axis();
        axis.MajorTicks = axis.MajorTicks with { LabelSize = 20.0 * 100 / 72 };

        (new double[] { -1, -0.5, 0, 0.5, 1 }).AssertCloseTo(Ticks(new Range1D(-1, 1), Theme.MatplotlibV2, DefaultWidth, isX: true, axis), relative: 1e-9, floor: 1);
    }

    [Fact]
    public void TheLibrarysOwnTheme_KeepsItsNearestStepPlacement() =>
        (new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }).AssertCloseTo(Ticks(new Range1D(0, 9.3), Theme.Default, DefaultWidth, isX: true), relative: 1e-9, floor: 1);

    [Fact]
    public void TheClassicTheme_KeepsItsNineBins_WhateverTheLength() =>
        (new double[] { -1, -0.5, 0, 0.5, 1 }).AssertCloseTo(Ticks(new Range1D(-1, 1), Theme.MatplotlibClassic, 60, isX: true), relative: 1e-9, floor: 1);

    // ── one ladder, only the bin count varying ───────────────────────────────────────────────────────────────────

    [Fact]
    public void ForTheRoomOfAnAxis_OnlyTheBinsChange_AndTheStaircaseIsTheSameArray()
    {
        var ladder = Theme.MatplotlibV2.Looks.Ticks.Ladder!.Value;

        Assert.Equal(9, ladder.ForTickSpace(MaxNLocator.LabelsAlong(DefaultWidth, V2LabelPx, alongX: true)).Bins);
        Assert.Equal(3, ladder.ForTickSpace(MaxNLocator.LabelsAlong(155, V2LabelPx, alongX: true)).Bins);
        Assert.Equal(4, ladder.ForTickSpace(MaxNLocator.LabelsAlong(115.5, V2LabelPx, alongX: false)).Bins);
        // np.clip(..., max(1, min_n_ticks - 1), 9): never below one bin, never above nine.
        Assert.Equal(1, ladder.ForTickSpace(MaxNLocator.LabelsAlong(10, V2LabelPx, alongX: true)).Bins);
        Assert.Equal(9, ladder.ForTickSpace(MaxNLocator.LabelsAlong(10_000, V2LabelPx, alongX: true)).Bins);
        // get_tick_space with no label size answers 2**31 - 1.
        Assert.Equal(9, ladder.ForTickSpace(MaxNLocator.LabelsAlong(100, 0, alongX: true)).Bins);
        Assert.Same(ladder.ExtendedSteps, ladder.ForTickSpace(MaxNLocator.LabelsAlong(155, V2LabelPx, alongX: true)).ExtendedSteps);

        var classic = Theme.MatplotlibClassic.Looks.Ticks.Ladder!.Value;
        Assert.Equal(9, classic.ForTickSpace(MaxNLocator.LabelsAlong(60, V2LabelPx, alongX: true)).Bins);
    }

    [Fact]
    public void TheV2ThemeCarriesTheLadder_AndAClone_AndTheJson_KeepIt()
    {
        var v2 = Theme.MatplotlibV2.Looks.Ticks.Ladder!.Value;
        Assert.Equal([1.0, 2.0, 2.5, 5.0, 10.0], v2.Steps);
        Assert.Equal(9, v2.Bins);
        Assert.Equal(2, v2.MinTicks);
        Assert.True(v2.BinsByLength);
        Assert.False(Theme.MatplotlibClassic.Looks.Ticks.Ladder!.Value.BinsByLength);

        var cloned = Theme.MatplotlibV2.Cloned().Looks.Ticks.Ladder!.Value;
        Assert.True(cloned.BinsByLength);

        var dto = ThemeDto.From(Theme.MatplotlibV2);
        Assert.True(dto.AxisTickBinsByLength);
        Assert.Null(ThemeDto.From(Theme.MatplotlibClassic).AxisTickBinsByLength);
        var read = dto.ToTheme().Looks.Ticks.Ladder!.Value;
        Assert.Equal(v2.Steps, read.Steps);
        Assert.Equal(v2.Bins, read.Bins);
        Assert.True(read.BinsByLength);
        Assert.False(Theme.MatplotlibClassic.ThroughDto().Looks.Ticks.Ladder!.Value.BinsByLength);
    }

    // ── what the layout and the draw lay the ticks along ─────────────────────────────────────────────────────────

    [Fact]
    public void V2_ATopAxisOfItsOwnRange_LaysItsTicksAlongThePlotsWidth_AsTwinyDoes()
    {
        // ax.twiny() over 0 .. 9.3 in a default figure: get_tick_space 11, xlim (-0.465, 9.765), ticks every 2. At the 240 px the
        // library's own top axis is laid along, the bins would be 5 and the step 2.5.
        var axes = new Axes();
        axes.Plot([0.0, 9.3], [0.0, 9.3]);
        axes.PlotXSecondary([0.0, 9.3], [0.0, 1.0]);
        var placement = new CartesianAxisPlacement(axes, new Rect(80, 58, DefaultWidth, DefaultHeight), new RecordingRenderContext(),
            Theme.MatplotlibV2);

        var top = placement.PlaceTopAxis(placement.ComputeDataRanges());

        (new double[] { 0, 2, 4, 6, 8 }).AssertCloseTo([.. top.Ticks.Where(t => t >= top.Range.XMin && t <= top.Range.XMax)], relative: 1e-9, floor: 1);
    }

    [Fact]
    public void TheLayoutMeasuresAYSlot_AtTheLengthOfItsDensestTicks()
    {
        var axis = new Axis();

        // Nine bins two label heights apart need 9 * 2 * 13.89 = 250 px, past the 240 px of the library's densest placement; the
        // layout measures half a span past that.
        Assert.Equal(9.5 * MaxNLocator.LabelSpanAlong(false) * V2LabelPx, axis.DensestTickLength(Theme.MatplotlibV2, isX: false), 9);
        Assert.Equal(9, Theme.MatplotlibV2.Looks.Ticks.Ladder!.Value.ForTickSpace(MaxNLocator.LabelsAlong(axis.DensestTickLength(Theme.MatplotlibV2, isX: false), V2LabelPx, alongX: false)).Bins);
        Assert.Equal(AutoLocator.FullDensityLength, axis.DensestTickLength(Theme.Default, isX: false));
        Assert.Equal(AutoLocator.FullDensityLength, axis.DensestTickLength(Theme.MatplotlibClassic, isX: false));
    }

    [Fact]
    public void V2_ALineChart_LabelsItsXAxisEveryTwo_AsMatplotlibDoes()
    {
        // ax.plot([0, 9.3], [0, 9.3]) at the default size: xlim (-0.465, 9.765), labels 0, 2, 4, 6, 8 in view.
        var svg = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 9.3], [0.0, 9.3])).Build().ToSvg().Xml();

        var labels = svg.Each("text").Where(e => (string?)e.Attribute("text-anchor") == "middle")
            .Select(e => e.Value).ToList();
        Assert.Equal(["0", "2", "4", "6", "8"], labels);
    }

    // ── 3-D ──────────────────────────────────────────────────────────────────────────────────────────────────────

    public static TheoryData<double, double, double[]> CubeViews { get; } = new()
    {
        // ax.plot([0, 9.3], [0, 1], [0, 0.46]) in a default 3-D axes (3.696 in wide: get_tick_space 8), the 3-D raw step 23/24.
        { -0.6781250000000001, 9.978125, [-2, 0, 2, 4, 6, 8, 10] },
        { -0.07291666666666667, 1.0729166666666667, [-0.2, 0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2] },
        { -0.009583333333333333, 0.46958333333333335, [-0.1, 0, 0.1, 0.2, 0.3, 0.4, 0.5] },
    };

    [Theory]
    [MemberData(nameof(CubeViews))]
    public void V2_TheTicksOfA3DAxis_AreMatplotlibsAutoLocatorsAlongThe3DAxesWidth(double lo, double hi, double[] expected) =>
        expected.AssertCloseTo(ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(new Axis(), new Range1D(lo, hi), Theme.MatplotlibV2, axesWidth: 369.6), relative: 1e-9, floor: 1);

    [Fact]
    public void A3DAxisUnderTheLibrarysTheme_KeepsItsOwnPlacement() =>
        (new double[] { 0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 }).AssertCloseTo(ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(new Axis(), new Range1D(0, 1), Theme.Default, axesWidth: 369.6), relative: 1e-9, floor: 1);
}
