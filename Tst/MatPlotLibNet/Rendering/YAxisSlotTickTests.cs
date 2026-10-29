// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The number of ticks of a Y axis slot stacked beside the plot follows the length of the plot as the primary
/// axis' does: a plot under 240 px asks for fewer, a short row of a dashboard is not crammed with labels. Worked out by hand
/// for the limits 0 to 100: a target of 8 intervals gives a step of 10 (11 ticks), a target of 5 gives a step of 20 (6).</summary>
public class YAxisSlotTickTests
{
    private static Axes AxesWithARightSlot()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1, 2], [0.0, 50, 100]);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
        var slot = axes.YAxes.Right.Add();
        slot.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 1, 2], [0.0, 50, 100]));
        slot.Axis.Min = 0;
        slot.Axis.Max = 100;
        return axes;
    }

    private static double[] SlotTicks(double plotHeight)
    {
        var axes = AxesWithARightSlot();
        var renderer = new CartesianAxesRenderer(axes, new Rect(50, 50, 300, plotHeight), new RecordingRenderContext(), Theme.Default);

        return renderer.PlaceSlot(axes.YAxes.Right[0], new MinMaxRange(0, 2)).Ticks;
    }

    [Fact]
    public void ASlotBesideAShortPlot_HasFewerTicks_AsThePrimaryAxisHas()
    {
        // 150 px of plot: round(150 / 30) = 5 intervals.
        Assert.Equal([0.0, 20, 40, 60, 80, 100], SlotTicks(150));
    }

    [Fact]
    public void ASlotBesideATallPlot_KeepsTheDefaultDensity()
    {
        Assert.Equal([0.0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100], SlotTicks(400));
    }

    [Fact]
    public void TheLayoutReservesForTheDensestTicksTheSlotCanDraw()
    {
        // The layout does not know the plot's height: it measures the slot as for a tall plot, so what it reserves holds
        // the labels of any shorter plot, whose ticks are a coarser set of the same values.
        var axes = AxesWithARightSlot();
        var bands = ConstrainedLayoutEngine.SecondaryAxisBands(axes, new RecordingRenderContext(), Theme.Default);
        var renderer = new CartesianAxesRenderer(axes, new Rect(50, 50, 300, 400), new RecordingRenderContext(), Theme.Default);

        Assert.Equal(renderer.SlotBand(renderer.PlaceSlot(axes.YAxes.Right[0], new MinMaxRange(0, 2))), Assert.Single(bands), 9);
    }
}
