// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one rule that folds what series contribute into the range an axis draws, read at the three places that
/// fold it besides the main pipeline: the 3-D axes, a Y slot past the primary, and the top X axis. The rule is
/// <see cref="Range1D"/>'s (<c>Merge</c>, then <c>Normalized</c>): an empty range is 0 to 1, a range of no width is widened
/// by a half either side, and a range with only its low end (a series that reports a minimum and no maximum) is one unit
/// wide. The first two held at every place before; the third held only on the main pipeline.</summary>
public sealed class RangeFoldRuleTests
{
    private static readonly Rect Plot = new(80, 60, 700, 400);

    /// <summary>A series that draws nothing and reports exactly the contribution it is given.</summary>
    private sealed class ReportingSeries(DataRangeContribution reported) : ChartSeries
    {
        public override void Accept(ISeriesVisitor visitor, RenderArea area) { }

        public override DataRangeContribution ComputeDataRange(IAxesContext context) => reported;

        public override SeriesDto ToSeriesDto() => new() { Type = "reporting" };
    }

    /// <summary>The six ends of the 3-D box the axes draw.</summary>
    private readonly record struct Cube(double XMin, double XMax, double YMin, double YMax, double ZMin, double ZMax);

    private static Cube Range3D(Axes axes)
    {
        var box = axes.ComputeCubeRange(Theme.Default);
        return new(box.X.Lo, box.X.Hi, box.Y.Lo, box.Y.Hi, box.Z.Lo, box.Z.Hi);
    }

    private static CartesianAxisPlacement Cartesian(Axes axes) =>
        new(axes, Plot, new SvgRenderContext(), Theme.Default);

    private static Axes ThreeD(params ISeries[] series)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        foreach (var s in series) axes.AddSeries(s);
        return axes;
    }

    /// <summary>What the 3-D axes draw over a unit-padded interval: the stage-1 margin of five per cent, then the view margin
    /// of a forty-eighth of the width that results, each side. The z margin is nil: the projection is a perspective one.</summary>
    private static Interval Drawn(double lo, double hi, double margin)
    {
        double pad1 = (hi - lo) * margin;
        double lo1 = lo - pad1, hi1 = hi + pad1;
        double pad2 = (hi1 - lo1) / 48.0;
        return new(lo1 - pad2, hi1 + pad2);
    }

    /// <summary>The two ends of an expected range.</summary>
    private readonly record struct Interval(double Lo, double Hi);

    private static void AssertRange(Interval expected, double lo, double hi)
    {
        Assert.Equal(expected.Lo, lo, 9);
        Assert.Equal(expected.Hi, hi, 9);
    }

    // ── 3-D axes ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThreeD_NoSeries_RangesEveryAxisZeroToOne()
    {
        var r = Range3D(ThreeD());

        AssertRange(Drawn(0, 1, 0.05), r.XMin, r.XMax);
        AssertRange(Drawn(0, 1, 0.05), r.YMin, r.YMax);
        AssertRange(Drawn(0, 1, 0.0), r.ZMin, r.ZMax);
    }

    [Fact]
    public void ThreeD_SeriesReportingASinglePoint_WidensEachAxisByAHalfEitherSide()
    {
        var r = Range3D(ThreeD(new ReportingSeries(new(5, 5, 7, 7, ZMin: 2, ZMax: 2))));

        AssertRange(Drawn(4.5, 5.5, 0.05), r.XMin, r.XMax);
        AssertRange(Drawn(6.5, 7.5, 0.05), r.YMin, r.YMax);
        AssertRange(Drawn(1.5, 2.5, 0.0), r.ZMin, r.ZMax);
    }

    [Fact]
    public void ThreeD_SeveralSeries_FoldToTheirUnion()
    {
        var r = Range3D(ThreeD(
            new ReportingSeries(new(0, 4, 1, 2, ZMin: 0, ZMax: 1)),
            new ReportingSeries(new(2, 10, -3, 0, ZMin: -1, ZMax: 5)),
            new ReportingSeries(new(null, null, null, null))));

        AssertRange(Drawn(0, 10, 0.05), r.XMin, r.XMax);
        AssertRange(Drawn(-3, 2, 0.05), r.YMin, r.YMax);
        AssertRange(Drawn(-1, 5, 0.0), r.ZMin, r.ZMax);
    }

    [Fact]
    public void ThreeD_SetLimitsBypassTheFold()
    {
        var axes = ThreeD(new ReportingSeries(new(0, 10, 0, 10, ZMin: 0, ZMax: 10)));
        axes.XAxis.Min = 2;
        axes.XAxis.Max = 3;

        var r = Range3D(axes);

        // A side the author set is left as set by every margin.
        Assert.Equal(2.0, r.XMin);
        Assert.Equal(3.0, r.XMax);
    }

    [Fact]
    public void ThreeD_GridSeries_FoldsItsDrawableCoordinatesAndSkipsWhatCannotBePlaced()
    {
        double[] x = [0, 1, double.NaN, double.PositiveInfinity, 4];
        double[] y = [10, 20];
        double[,] z = new double[2, 5];
        z[0, 0] = -1; z[1, 4] = 3; z[0, 2] = double.NaN;
        var r = Range3D(ThreeD(new SurfaceSeries(x, y, z)));

        AssertRange(Drawn(0, 4, 0.05), r.XMin, r.XMax);
        AssertRange(Drawn(10, 20, 0.05), r.YMin, r.YMax);
        AssertRange(Drawn(-1, 3, 0.0), r.ZMin, r.ZMax);
    }

    /// <summary>Named fix: a series that reports a minimum and no maximum left the 3-D range at the maximum's seed, the
    /// lowest double, so the axis was drawn from the minimum to minus infinity and the box with it. The range is now one unit
    /// wide there, as it is on the 2-D axes.</summary>
    [Fact]
    public void ThreeD_SeriesReportingOnlyAMinimum_IsOneUnitWideFromIt()
    {
        var r = Range3D(ThreeD(new ReportingSeries(new(3, null, 6, null, ZMin: 1))));

        AssertRange(Drawn(3, 4, 0.05), r.XMin, r.XMax);
        AssertRange(Drawn(6, 7, 0.05), r.YMin, r.YMax);
        AssertRange(Drawn(1, 2, 0.0), r.ZMin, r.ZMax);
    }

    /// <summary>Named fix: one set limit with all the data beyond it drew the 3-D box inverted (the set side above the free
    /// side). The free side is put one unit past the set one, as on the 2-D axes (<c>Range1D.WithExplicitLimits</c>).</summary>
    [Fact]
    public void ThreeD_OneSetLimitWithAllTheDataBeyondIt_IsNotDrawnInverted()
    {
        var axes = ThreeD(new ReportingSeries(new(0, 10, 0, 10, ZMin: 0, ZMax: 10)));
        axes.XAxis.Min = 20;

        var r = Range3D(axes);

        // The set side stays at 20; the free side is 21, then takes the stage-1 margin (a twentieth of the 1 unit) and the
        // view margin (a forty-eighth of the 1.05 that results).
        Assert.Equal(20.0, r.XMin);
        Assert.Equal(21.0 + 0.05 + 1.05 / 48.0, r.XMax, 9);
    }

    // ── a Y slot past the primary ──────────────────────────────────────────────────────────────────

    private static DataRange Slot(Axes axes, YAxisSlot slot, double xMin = 0, double xMax = 1) =>
        Cartesian(axes).ComputeSecondaryDataRanges(xMin, xMax, slot.Axis, slot.Series);

    [Fact]
    public void YSlot_NoSeries_RangesZeroToOne()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.FirstOrAdd();

        var r = Slot(axes, slot);

        AssertRange(new(-0.05, 1.05), r.YMin, r.YMax);
    }

    [Fact]
    public void YSlot_SeriesOfNoWidth_IsWidenedByAHalfEitherSide()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.FirstOrAdd();
        slot.AddSeries(new ReportingSeries(new(0, 1, 5, 5)));

        var r = Slot(axes, slot);

        AssertRange(new(4.5 - 0.05, 5.5 + 0.05), r.YMin, r.YMax);
    }

    [Fact]
    public void YSlot_SeriesFoldToTheirUnion()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.FirstOrAdd();
        slot.AddSeries(new ReportingSeries(new(0, 1, 2, 4)));
        slot.AddSeries(new ReportingSeries(new(0, 1, -2, 3)));

        var r = Slot(axes, slot);

        AssertRange(new(-2 - 0.3, 4 + 0.3), r.YMin, r.YMax);
    }

    /// <summary>Named fix: the same as the 3-D one. A slot's series that reports a minimum and no maximum left the range at
    /// its seed and the axis was drawn to minus infinity; it is one unit wide from the minimum, as on the primary axes.</summary>
    [Fact]
    public void YSlot_SeriesReportingOnlyAMinimum_IsOneUnitWideFromIt()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.FirstOrAdd();
        slot.AddSeries(new ReportingSeries(new(0, 1, 2, null)));

        var r = Slot(axes, slot);

        AssertRange(new(2 - 0.05, 3 + 0.05), r.YMin, r.YMax);
    }

    // ── the top X axis ─────────────────────────────────────────────────────────────────────────────

    private static DataRange Top(Axes axes, double yMin = 0, double yMax = 1) =>
        Cartesian(axes).ComputeSecondaryXDataRanges(yMin, yMax);

    [Fact]
    public void TopAxis_NoSeries_RangesZeroToOne()
    {
        var axes = new Axes();
        axes.TwinY();

        var r = Top(axes);

        AssertRange(new(-0.05, 1.05), r.XMin, r.XMax);
    }

    [Fact]
    public void TopAxis_SeriesOfNoWidth_IsWidenedByAHalfEitherSide()
    {
        var axes = new Axes();
        axes.AddXSecondarySeries(new ReportingSeries(new(5, 5, 0, 1)));

        var r = Top(axes);

        AssertRange(new(4.5 - 0.05, 5.5 + 0.05), r.XMin, r.XMax);
    }

    [Fact]
    public void TopAxis_SeriesFoldToTheirUnion()
    {
        var axes = new Axes();
        axes.AddXSecondarySeries(new ReportingSeries(new(0, 4, 0, 1)));
        axes.AddXSecondarySeries(new ReportingSeries(new(-2, 3, 0, 1)));

        var r = Top(axes);

        AssertRange(new(-2 - 0.3, 4 + 0.3), r.XMin, r.XMax);
    }

    /// <summary>Named fix: the top axis asked only a series that carries the <see cref="IHasDataRange"/> marker for its range,
    /// while a Y slot asks every <see cref="ISeries"/>; a series written against the interface alone was drawn off the plot on
    /// the top axis and in place on a Y slot. Both ask the interface now.</summary>
    [Fact]
    public void TopAxis_SeriesWrittenAgainstTheInterface_IsRangedLikeAnyOther()
    {
        var axes = new Axes();
        axes.AddXSecondarySeries(new ForeignSeries(new(100, 200, 0, 1)));

        var r = Top(axes);

        AssertRange(new(100 - 5.0, 200 + 5.0), r.XMin, r.XMax);
    }

    [Fact]
    public void TopAxis_SeriesReportingOnlyAMinimum_IsOneUnitWideFromIt()
    {
        var axes = new Axes();
        axes.AddXSecondarySeries(new ReportingSeries(new(2, null, 0, 1)));

        var r = Top(axes);

        AssertRange(new(2 - 0.05, 3 + 0.05), r.XMin, r.XMax);
    }

    // ── one rule, whatever the axis ────────────────────────────────────────────────────────────────

    /// <summary>A set limit is exact on a slot and on the top axis: the side the author set is the side drawn, whatever the
    /// series reach and whatever the margin.</summary>
    [Fact]
    public void SetLimits_AreExact_OnASlotAndOnTheTopAxis()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.FirstOrAdd();
        slot.Axis.Min = 0;
        slot.Axis.Max = 10;
        slot.AddSeries(new ReportingSeries(new(0, 1, -50, 50)));
        axes.AddXSecondarySeries(new ReportingSeries(new(-50, 50, 0, 1)));
        axes.SecondaryXAxis!.Min = 1;
        axes.SecondaryXAxis!.Max = 2;

        var y = Slot(axes, slot);
        var x = Top(axes);

        Assert.Equal(0.0, y.YMin);
        Assert.Equal(10.0, y.YMax);
        Assert.Equal(1.0, x.XMin);
        Assert.Equal(2.0, x.XMax);
    }

    /// <summary>Bars rise from 0, and the cube's Z range takes 0 in, but the view margin pads below it as below any value:
    /// matplotlib 3.11's Axes3D._autoscale_axis knows no sticky edge, so its bar3d of the fidelity scene (heights 1 to 16)
    /// draws Z from -0.3333 to 16.3333 in both styles.</summary>
    [Fact]
    public void ThreeD_TheFloorOfBars_IsPaddedLikeAnyValue()
    {
        var axes = ThreeD(new Bar3DSeries(new double[] { 1, 2, 3 }, new double[] { 0.5, 1, 1.5 }, new double[] { 2, 4, 3 }));

        var z = axes.ComputeCubeRange(Theme.Default).Z;

        AssertRange(Drawn(0, 4, 0), z.Lo, z.Hi);
    }

    /// <summary>The Z axis of a 3-D box has no stage-1 margin under a perspective projection, which ours always is (focal
    /// length 1 by default; a camera distance d draws as focal length d / 10). matplotlib 3.11, axes3d.py:
    /// <code>
    /// def clear(self):
    ///     # docstring inherited.
    ///     super().clear()
    ///     if self._focal_length == np.inf:
    ///         self._zmargin = mpl.rcParams['axes.zmargin']
    ///     else:
    ///         self._zmargin = 0.
    /// </code>
    /// The 5 % margin went to a figure with a camera distance, the wrong way round: a distance keeps the projection a
    /// perspective one, so it pads Z by the view margin alone, as without one.</summary>
    [Fact]
    public void ThreeD_ACameraDistance_PadsZByTheViewMarginAlone()
    {
        var withDistance = ThreeD(new Line3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 2, 6 }));
        withDistance.CameraDistance = 9;

        AssertRange(Drawn(2, 6, 0), withDistance.ComputeCubeRange(Theme.Default).Z.Lo, withDistance.ComputeCubeRange(Theme.Default).Z.Hi);
    }

    /// <summary>X and Y of a 3-D box take the theme's stage-1 margin, as matplotlib's Axes3D pads them by axes.xmargin and
    /// axes.ymargin: 0 under the classic style, where <c>ax.plot([0, 1], [0, 1], [0, 1])</c> draws X from -0.0208 to 1.0208
    /// (matplotlib 3.11.2), the view margin alone. Z keeps none under the perspective projection, whatever the theme.</summary>
    [Fact]
    public void ThreeD_XAndY_TakeTheThemesMargin()
    {
        var axes = ThreeD(new Line3DSeries(new double[] { 0, 1 }, new double[] { 0, 2 }, new double[] { 0, 1 }));

        var classic = axes.ComputeCubeRange(Theme.MatplotlibClassic);
        var v2 = axes.ComputeCubeRange(Theme.MatplotlibV2);

        AssertRange(Drawn(0, 1, 0), classic.X.Lo, classic.X.Hi);
        AssertRange(Drawn(0, 2, 0), classic.Y.Lo, classic.Y.Hi);
        AssertRange(Drawn(0, 1, 0), classic.Z.Lo, classic.Z.Hi);
        AssertRange(Drawn(0, 1, 0.05), v2.X.Lo, v2.X.Hi);
        AssertRange(Drawn(0, 2, 0.05), v2.Y.Lo, v2.Y.Hi);
    }

    /// <summary>Under the classic theme each axis of a 3-D box rounds out to its outermost ticks before the view margin, as
    /// matplotlib's classic style does (axes.autolimit_mode 'round_numbers': Axes3D._autoscale_axis takes the major locator's
    /// view_limits). matplotlib 3.11.2, classic, bar3d of the fidelity scene: X from 0 to 3.8 draws (-0.0833, 4.0833), Z from 0
    /// to 16 draws (-0.3333, 16.3333); a stem over 0..2π on Z draws (-0.1458, 7.1458).</summary>
    [Fact]
    public void ThreeD_UnderTheClassicTheme_TheLimitsRoundOutToTheOutermostTicks()
    {
        var axes = ThreeD(new Line3DSeries(new double[] { 0, 3.8 }, new double[] { -0.986, 1 }, new double[] { 0, 2 * Math.PI }));

        var classic = axes.ComputeCubeRange(Theme.MatplotlibClassic);

        AssertRange(Drawn(0, 4, 0), classic.X.Lo, classic.X.Hi);
        AssertRange(Drawn(-1, 1, 0), classic.Y.Lo, classic.Y.Hi);
        AssertRange(Drawn(0, 7, 0), classic.Z.Lo, classic.Z.Hi);
    }

    /// <summary>The ends of the ticks matplotlib's classic 3-D locator lays over a range (matplotlib 3.11.2, classic,
    /// <c>ax.xaxis.get_major_locator()._raw_ticks(lo, hi)</c> of a 3-D axes). The classic locator steps by 1, 2, 5 and 10
    /// (<c>_extended_steps</c> 0.1 to 20), never by 2.5: 0 to 22 ends at 25, 0 to 2.2 at 2.5.</summary>
    [Theory]
    [InlineData(0, 3.8, 0, 4)]
    [InlineData(-0.986, 1, -1, 1)]
    [InlineData(0, 6.283185307179586, 0, 7)]
    [InlineData(0, 16, 0, 16)]
    [InlineData(-3, 3, -3, 3)]
    [InlineData(-2.16, 2.35, -3, 3)]
    [InlineData(-2.38, 3.16, -3, 4)]
    [InlineData(1000.2, 1003.7, 1000, 1004)]
    [InlineData(0, 1, 0, 1)]
    [InlineData(-0.001, 0.002, -0.001, 0.002)]
    [InlineData(0.3, 0.9, 0.3, 0.9)]
    [InlineData(0.95, 1.05, 0.94, 1.06)]
    [InlineData(0.7, 0.71, 0.7, 0.71)]
    [InlineData(1.01, 1.09, 1.01, 1.09)]
    [InlineData(0.1, 0.3, 0.1, 0.3)]
    [InlineData(-0.3, 0.3, -0.3, 0.3)]
    [InlineData(0, 22, 0, 25)]
    [InlineData(0, 2.2, 0, 2.5)]
    [InlineData(0, 0.22, 0, 0.25)]
    [InlineData(-1.1, 1.1, -1.5, 1.5)]
    public void ThreeD_RoundsOutToTheEndsOfMatplotlibsClassicTicks(double lo, double hi, double expectedLo, double expectedHi)
    {
        var rounded = new AxisAutoscale(new Axis(), isX: false, Theme.Default).RoundedOutToTicks(new Range1D(lo, hi), TickLadder.Classic);

        AssertRange(new Interval(expectedLo, expectedHi), rounded.Lo, rounded.Hi);
    }

    /// <summary>An axis with a spacing and an axis with a <c>MultipleLocator</c> of its own round to the multiples by one rule
    /// (matplotlib's MultipleLocator.view_limits); one with a locator that rounds nothing (a <c>FixedLocator</c>) and a degenerate
    /// range keep what they have.</summary>
    [Fact]
    public void ThreeD_ASpacingAndAMultipleLocatorRoundToTheMultiples_AFixedLocatorAndAPointDoNot()
    {
        var spaced = new Axis { MajorTicks = new TickConfig { Spacing = 3 } };
        var located = new Axis { TickLocator = new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(3) };
        var fixedTicks = new Axis { TickLocator = new MatPlotLibNet.Rendering.TickLocators.FixedLocator([1.0]) };

        AssertRange(new Interval(-3, 9), new AxisAutoscale(spaced, isX: false, Theme.Default).RoundedOutToTicks(new Range1D(-0.5, 7.2), TickLadder.Classic).Lo, new AxisAutoscale(spaced, isX: false, Theme.Default).RoundedOutToTicks(new Range1D(-0.5, 7.2), TickLadder.Classic).Hi);
        Assert.Equal(new AxisAutoscale(spaced, isX: false, Theme.Default).RoundedOutToTicks(new Range1D(-0.5, 7.2), TickLadder.Classic), new AxisAutoscale(located, isX: false, Theme.Default).RoundedOutToTicks(new Range1D(-0.5, 7.2), TickLadder.Classic));
        Assert.Equal(new Range1D(-0.5, 7.2), new AxisAutoscale(fixedTicks, isX: false, Theme.Default).RoundedOutToTicks(new Range1D(-0.5, 7.2), TickLadder.Classic));
        Assert.Equal(new Range1D(2, 2), new AxisAutoscale(new Axis(), isX: false, Theme.Default).RoundedOutToTicks(new Range1D(2, 2), TickLadder.Classic));
    }

    /// <summary>Only the classic theme rounds (matplotlib's default style has axes.autolimit_mode 'data'), and a limit the
    /// author sets is kept as set.</summary>
    [Fact]
    public void ThreeD_TheModernThemeAndASetLimit_DoNotRound()
    {
        var axes = ThreeD(new Line3DSeries(new double[] { 0, 3.8 }, new double[] { 0, 1 }, new double[] { 0, 2 * Math.PI }));
        axes.ZAxis.Min = 0;
        axes.ZAxis.Max = 6.5;

        var v2 = axes.ComputeCubeRange(Theme.MatplotlibV2);
        var classic = axes.ComputeCubeRange(Theme.MatplotlibClassic);

        AssertRange(Drawn(0, 3.8, 0.05), v2.X.Lo, v2.X.Hi);
        AssertRange(new Interval(0, 6.5), classic.Z.Lo, classic.Z.Hi);
    }

    /// <summary>A 3-D scatter keeps a margin of at least 5 % on Z, as matplotlib's Axes3D.scatter does in every style
    /// (<c>if self._zmargin &lt; 0.05 and xs.size &gt; 0: self.set_zmargin(0.05)</c>): matplotlib 3.11.2 draws the Z of the
    /// default-style scatter of the fidelity scene from -2.5 for data from -2.13. A line keeps none; a margin set on the axis
    /// wins.</summary>
    [Fact]
    public void ThreeD_AScatter_KeepsAFivePercentMarginOnZ()
    {
        var scatter = ThreeD(new Scatter3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 }));
        var line = ThreeD(new Line3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 }));
        var set = ThreeD(new Scatter3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 }));
        set.ZAxis.Margin = 0;

        var z = scatter.ComputeCubeRange(Theme.MatplotlibV2).Z;
        var lineZ = line.ComputeCubeRange(Theme.MatplotlibV2).Z;
        var setZ = set.ComputeCubeRange(Theme.MatplotlibV2).Z;

        AssertRange(Drawn(0, 1, 0.05), z.Lo, z.Hi);
        AssertRange(Drawn(0, 1, 0), lineZ.Lo, lineZ.Hi);
        AssertRange(Drawn(0, 1, 0), setZ.Lo, setZ.Hi);
    }

    /// <summary>A margin set on the axis wins over the theme's.</summary>
    [Fact]
    public void ThreeD_AnAxisMargin_WinsOverTheThemes()
    {
        var axes = ThreeD(new Line3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 }));
        axes.XAxis.Margin = 0.1;
        var onY = ThreeD(new Line3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 }));
        onY.YAxis.Margin = 0.2;

        var v2 = axes.ComputeCubeRange(Theme.MatplotlibV2);
        var v2OnY = onY.ComputeCubeRange(Theme.MatplotlibV2);

        AssertRange(Drawn(0, 1, 0.1), v2.X.Lo, v2.X.Hi);
        AssertRange(Drawn(0, 1, 0.05), v2.Y.Lo, v2.Y.Hi);
        AssertRange(Drawn(0, 1, 0.2), v2OnY.Y.Lo, v2OnY.Y.Hi);
    }
}
