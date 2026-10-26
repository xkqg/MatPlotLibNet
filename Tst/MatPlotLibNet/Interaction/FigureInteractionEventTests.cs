// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

public class FigureInteractionEventTests
{
    [Fact]
    public void ZoomEvent_ApplyTo_SetsAxisLimits()
    {
        var figure = TestFigures.SingleLine();
        var evt = new ZoomEvent("chart-1", AxesIndex: 0, XMin: 0.5, XMax: 2.5, YMin: 0, YMax: 10);

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(0.5, axes.XAxis.Min);
        Assert.Equal(2.5, axes.XAxis.Max);
        Assert.Equal(0.0, axes.YAxis.Min);
        Assert.Equal(10.0, axes.YAxis.Max);
    }

    [Fact]
    public void ResetEvent_ApplyTo_SetsAxisLimits()
    {
        var figure = TestFigures.SingleLine();
        var evt = new ResetEvent("chart-1", AxesIndex: 0, XMin: 1, XMax: 3, YMin: 1, YMax: 9);

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(1.0, axes.XAxis.Min);
        Assert.Equal(3.0, axes.XAxis.Max);
        Assert.Equal(1.0, axes.YAxis.Min);
        Assert.Equal(9.0, axes.YAxis.Max);
    }

    [Fact]
    public void PanEvent_ApplyTo_TranslatesAxisLimits()
    {
        var figure = TestFigures.SingleLine();
        var axes = figure.SubPlots[0];
        axes.XAxis.Min = 0; axes.XAxis.Max = 10;
        axes.YAxis.Min = 0; axes.YAxis.Max = 20;

        var evt = new PanEvent("chart-1", AxesIndex: 0, DxData: 2.0, DyData: -5.0);
        evt.ApplyTo(figure);

        Assert.Equal(2.0, axes.XAxis.Min);
        Assert.Equal(12.0, axes.XAxis.Max);
        Assert.Equal(-5.0, axes.YAxis.Min);
        Assert.Equal(15.0, axes.YAxis.Max);
    }

    /// <summary>ROADMAP.md row 100: zoom/reset must move every outward-stacked secondary axis' own range, not
    /// only the primary. Each change is a slot's OWN new range — DataRange.XMin/XMax are unused here (a secondary
    /// Y axis shares the primary X, already carried by ZoomEvent's own XMin/XMax).</summary>
    [Fact]
    public void ZoomEvent_ApplyTo_WithSecondaryRanges_SetsEachSlotsOwnRange()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(y2 => y2.Plot([1.0, 2], [50.0, 60]))
                .WithOutwardYAxis(AxisSide.Right, outer => outer.Plot([1.0, 2], [500.0, 600]))
                .WithOutwardYAxis(AxisSide.Left, left => left.Plot([1.0, 2], [5.0, 6])))
            .Build();

        var evt = new ZoomEvent("chart-1", 0, 0.5, 2.5, 0, 10)
        {
            SecondaryRanges =
            [
                new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 1, 10, 20)),
                new SecondaryAxisRange(AxisSide.Right, 1, new DataRange(0, 1, 100, 200)),
                new SecondaryAxisRange(AxisSide.Left, 0, new DataRange(0, 1, 1, 2)),
            ],
        };

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(10.0, axes.RightYAxes[0].Axis.Min);
        Assert.Equal(20.0, axes.RightYAxes[0].Axis.Max);
        Assert.Equal(100.0, axes.RightYAxes[1].Axis.Min);
        Assert.Equal(200.0, axes.RightYAxes[1].Axis.Max);
        Assert.Equal(1.0, axes.LeftYAxes[0].Axis.Min);
        Assert.Equal(2.0, axes.LeftYAxes[0].Axis.Max);
    }

    /// <summary>A pan carries the ALREADY-computed final range per secondary slot (PanModifier works out each
    /// slot's own proportional delta before sending, since the primary's DxData/DyData are in the PRIMARY's data
    /// units) — ApplyTo assigns it directly, it does not re-derive a shift from DyData for a secondary slot.</summary>
    [Fact]
    public void PanEvent_ApplyTo_WithSecondaryRanges_AssignsEachSlotsOwnRangeDirectly()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(y2 => y2.Plot([1.0, 2], [50.0, 60])))
            .Build();

        var evt = new PanEvent("chart-1", 0, 2.0, -5.0)
        {
            SecondaryRanges = [new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 1, 30, 70))],
        };

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(30.0, axes.RightYAxes[0].Axis.Min);
        Assert.Equal(70.0, axes.RightYAxes[0].Axis.Max);
    }

    [Fact]
    public void PanEvent_ApplyTo_IsNoOpWhenAxisLimitsNull()
    {
        var figure = TestFigures.SingleLine();
        var axes = figure.SubPlots[0];
        axes.XAxis.Min = null; axes.XAxis.Max = null;
        axes.YAxis.Min = null; axes.YAxis.Max = null;

        var evt = new PanEvent("chart-1", 0, 1.0, 1.0);
        evt.ApplyTo(figure);

        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
    }

    [Fact]
    public void LegendToggleEvent_ApplyTo_FlipsVisible()
    {
        var figure = TestFigures.SingleLine();
        var series = (ChartSeries)figure.SubPlots[0].Series[0];
        Assert.True(series.Visible);

        var evt = new LegendToggleEvent("chart-1", AxesIndex: 0, SeriesIndex: 0);
        evt.ApplyTo(figure);
        Assert.False(series.Visible);

        evt.ApplyTo(figure);
        Assert.True(series.Visible);
    }

    /// <summary>The index a legend entry sends is the series' slot: the primary series, then the secondary-Y series,
    /// then the secondary-X series. The toggle flips the series at that slot and no other; a slot past the last one
    /// changes nothing.</summary>
    [Theory]
    [InlineData(0, "primary")]
    [InlineData(1, "secondaryY")]
    [InlineData(2, "secondaryX")]
    [InlineData(3, null)]
    public void LegendToggleEvent_ApplyTo_FlipsTheSeriesAtTheSlot_OnEitherAxis(int slot, string? hidden)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "primary")
            .WithSecondaryYAxis(y2 => y2.Plot([1.0, 2.0], [10.0, 20.0], s => s.Label = "secondaryY"))
            .WithSecondaryXAxis(x2 => x2.PlotXSecondary([100.0, 200.0], [1.0, 2.0], s => s.Label = "secondaryX"))).Build();
        var axes = figure.SubPlots[0];

        new LegendToggleEvent("c", 0, slot).ApplyTo(figure);

        var all = axes.Series.Concat(axes.SecondarySeries).Concat(axes.XSecondarySeries).Cast<ChartSeries>();
        Assert.All(all, s => Assert.Equal(s.Label != hidden, s.Visible));
    }

    [Fact]
    public void FigureInteractionEvent_IsAbstract()
    {
        Assert.True(typeof(FigureInteractionEvent).IsAbstract);
    }

    [Fact]
    public void AxisRangeEvent_IsAbstract()
    {
        Assert.True(typeof(AxisRangeEvent).IsAbstract);
    }

    [Fact]
    public void ZoomEvent_Inherits_AxisRangeEvent()
    {
        Assert.True(typeof(AxisRangeEvent).IsAssignableFrom(typeof(ZoomEvent)));
    }

    [Fact]
    public void ResetEvent_Inherits_AxisRangeEvent()
    {
        Assert.True(typeof(AxisRangeEvent).IsAssignableFrom(typeof(ResetEvent)));
    }

    [Fact]
    public void PanEvent_Inherits_FigureInteractionEvent_Directly()
    {
        Assert.Equal(typeof(FigureInteractionEvent), typeof(PanEvent).BaseType);
    }

    [Fact]
    public void LegendToggleEvent_Inherits_FigureInteractionEvent_Directly()
    {
        Assert.Equal(typeof(FigureInteractionEvent), typeof(LegendToggleEvent).BaseType);
    }

    [Fact]
    public void ZoomEvent_CarriesChartIdAndAxesIndex()
    {
        var evt = new ZoomEvent("live-1", 2, 0, 1, 0, 1);
        Assert.Equal("live-1", evt.ChartId);
        Assert.Equal(2, evt.AxesIndex);
    }

    [Fact]
    public void ZoomEvent_IsRecord_SupportsValueEquality()
    {
        var a = new ZoomEvent("c", 0, 1, 2, 3, 4);
        var b = new ZoomEvent("c", 0, 1, 2, 3, 4);
        Assert.Equal(a, b);
    }
}
