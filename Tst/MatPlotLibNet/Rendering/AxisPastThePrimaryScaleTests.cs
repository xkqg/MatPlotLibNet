// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axis past the primary (a Y axis slot, the top X axis) ranges its series under its own scale and puts its range on that
/// scale by the rule the primary axes follow (the smallest positive value its series reach is a logit axis' <c>minpos</c>), as
/// matplotlib's twin axes, which are axes of their own, range theirs. The numbers are matplotlib 3.11.2's, read with python
/// (<c>axes.ymargin</c> and <c>axes.xmargin</c> 0.05):
/// <code>
/// ax2 = ax.twinx(); ax2.plot([0, 1, 2, 3], [0, 5, 50, 500]); ax2.set_yscale('log')      -> ylim (3.9716411736214083, 629.4627058970839)
/// ax2 = ax.twiny(); ax2.plot([0, 0.1, 10, 1000], [1, 2, 3, 4]); ax2.set_xscale('log')   -> xlim (0.06309573444801932, 1584.8931924611156)
/// ax2 = ax.twinx(); ax2.plot([0, 1, 2, 3], [0, 0.3, 0.6, 1]); ax2.set_yscale('logit')   -> ylim (0.28251353013018654, 0.7174864698698133)
/// </code>
/// The series were asked under the primary's linear scale, so a 0 on a log slot was folded in and lifted to three decades under the
/// ceiling (0.5), and a logit slot took the standard smallest value.</summary>
public class AxisPastThePrimaryScaleTests
{
    private static readonly Rect Plot = new(80, 60, 700, 400);

    private static CartesianAxisPlacement PlacementOf(Axes axes) => new(axes, Plot, new SvgRenderContext(), Theme.MatplotlibV2);

    [Theory]
    [InlineData(AxisScale.Log, new[] { 0.0, 5, 50, 500 }, 3.9716411736214083, 629.4627058970839)]
    [InlineData(AxisScale.Logit, new[] { 0.0, 0.3, 0.6, 1 }, 0.28251353013018654, 0.7174864698698133)]
    public void AYSlot_RangesItsSeriesUnderItsOwnScale(AxisScale scale, double[] y, double lo, double hi)
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2, 3], [1.0, 2, 3, 4])
            .WithAddRightYAxis(s => s.Plot([0.0, 1, 2, 3], y))).Build().SubPlots[0];
        var slot = axes.YAxes.Right[0];
        slot.Axis.Scale = scale;

        var range = PlacementOf(axes).ComputeSecondaryDataRanges(0, 3, slot.Axis, slot.Series);

        Assert.Equal(lo, range.YMin, Math.Abs(lo) * 1e-12);
        Assert.Equal(hi, range.YMax, Math.Abs(hi) * 1e-12);
    }

    [Fact]
    public void TheTopAxis_RangesItsSeriesUnderItsOwnScale()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2, 3], [1.0, 2, 3, 4])
            .WithSecondaryXAxis(top => top.PlotXSecondary([0.0, 0.1, 10, 1000], [1.0, 2, 3, 4]))).Build().SubPlots[0];
        axes.SecondaryXAxis!.Scale = AxisScale.Log;

        var range = PlacementOf(axes).ComputeSecondaryXDataRanges(1, 4);

        Assert.Equal(0.06309573444801932, range.XMin, 1e-14);
        Assert.Equal(1584.8931924611156, range.XMax, 1584.8931924611156 * 1e-12);
    }

    /// <summary>The context an axis past the primary asks its series under names that axis' scale along its own direction and the
    /// primary's along the other.</summary>
    [Fact]
    public void TheContextOfAnAxisPastThePrimary_NamesItsScale()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithSecondaryXAxis(top => top.PlotXSecondary([1.0, 2], [1.0, 2])))
            .Build().SubPlots[0];
        axes.XAxis.Scale = AxisScale.Date;
        axes.YAxis.Scale = AxisScale.SymLog;
        axes.SecondaryXAxis!.Scale = AxisScale.Log;
        var slot = new Axis { Scale = AxisScale.Log };

        var alongY = AxesContextAdapter.PastPrimary(axes, slot, [], isX: false);
        var alongX = AxesContextAdapter.PastPrimary(axes, axes.SecondaryXAxis, axes.XSecondarySeries, isX: true);

        Assert.Equal(AxisScale.Date, ((MatPlotLibNet.Models.Series.IAxesContext)alongY).XScale);
        Assert.Equal(AxisScale.Log, ((MatPlotLibNet.Models.Series.IAxesContext)alongY).YScale);
        Assert.Equal(AxisScale.Log, ((MatPlotLibNet.Models.Series.IAxesContext)alongX).XScale);
        Assert.Equal(AxisScale.SymLog, ((MatPlotLibNet.Models.Series.IAxesContext)alongX).YScale);
    }
}
