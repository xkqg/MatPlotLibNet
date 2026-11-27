// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axis past the primary (a Y axis slot, the top X axis) is an axes of its own to matplotlib (<c>twinx</c>, <c>twiny</c>):
/// its series are ranged under its own limits along its direction and stacked among themselves, never the primary's. matplotlib 3.11.2
/// (printed): <c>ax.set_ylim(0, 1); ax2 = ax.twinx(); ax2.bar([0, 1], [3, 4]); ax2.bar([0, 1], [5, 6], bottom=[3, 4])</c> gives the twin
/// <c>ylim (0.0, 10.5)</c> and leaves the primary at (0, 1).</summary>
public class AxisPastThePrimaryContextTests
{
    private static readonly Rect Plot = new(80, 60, 700, 400);

    [Fact]
    public void AYSlotsContext_NamesItsOwnLimitsAlongY_AndItsOwnSeries()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.2, 0.4]).SetXLim(-1, 3).SetYLim(0, 1)
            .WithAddRightYAxis(s => s.Plot([0.0, 1], [10.0, 20]))).Build().SubPlots[0];
        var slot = axes.YAxes.Right[0];
        slot.Axis.Min = 5;
        slot.Axis.Max = 50;

        IAxesContext context = AxesContextAdapter.PastPrimary(axes, slot.Axis, slot.Series, isX: false);

        Assert.Equal(5, context.YAxisMin);
        Assert.Equal(50, context.YAxisMax);
        Assert.Equal(-1, context.XAxisMin);
        Assert.Equal(3, context.XAxisMax);
        Assert.Same(slot.Series, context.AllSeries);
    }

    [Fact]
    public void TheTopAxisContext_NamesItsOwnLimitsAlongX_AndItsOwnSeries()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.2, 0.4]).SetXLim(-1, 3).SetYLim(0, 1)
            .WithSecondaryXAxis(top => top.PlotXSecondary([1.0, 2], [1.0, 2]))).Build().SubPlots[0];
        axes.SecondaryXAxis!.Min = 100;
        axes.SecondaryXAxis.Max = 200;

        IAxesContext context = AxesContextAdapter.PastPrimary(axes, axes.SecondaryXAxis, axes.XSecondarySeries, isX: true);

        Assert.Equal(100, context.XAxisMin);
        Assert.Equal(200, context.XAxisMax);
        Assert.Equal(0, context.YAxisMin);
        Assert.Equal(1, context.YAxisMax);
        Assert.Same(axes.XSecondarySeries, context.AllSeries);
    }

    [Fact]
    public void StackedBarsOnAYSlot_AreStackedAmongThemselves_AndRangeTheSlotToTheirTop()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.2, 0.4]).SetYLim(0, 1).SetBarMode(BarMode.Stacked)
            .WithAddRightYAxis(s => s.SetYLabel("stacked"))).Build().SubPlots[0];
        var slot = axes.YAxes.Right[0];
        slot.AddSeries(new BarSeries(["a", "b"], [3.0, 4]));
        slot.AddSeries(new BarSeries(["a", "b"], [5.0, 6]));

        var range = new CartesianAxisPlacement(axes, Plot, new SvgRenderContext(), Theme.MatplotlibV2)
            .ComputeSecondaryDataRanges(-0.5, 1.5, slot.Axis, slot.Series);

        Assert.Equal(0, range.YMin, 12);
        Assert.Equal(10.5, range.YMax, 12);
    }
}
