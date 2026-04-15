// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

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
