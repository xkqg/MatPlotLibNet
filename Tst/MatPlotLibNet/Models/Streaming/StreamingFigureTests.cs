// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Tests.Models.Streaming;

public sealed class StreamingFigureTests
{
    private static (StreamingFigure sf, StreamingLineSeries series) CreateSimple()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var series = axes.AddSeries(new StreamingLineSeries(100));
        var sf = new StreamingFigure(figure);
        return (sf, series);
    }

    [Fact]
    public void Construction_WrappsFigure()
    {
        var (sf, _) = CreateSimple();
        Assert.NotNull(sf.Figure);
        sf.Dispose();
    }

    [Fact]
    public void DataVersion_ChangesWhenSeriesAppends()
    {
        var (sf, series) = CreateSimple();
        long v0 = sf.DataVersion;
        series.AppendPoint(1, 2);
        Assert.True(sf.DataVersion > v0);
        sf.Dispose();
    }

    [Fact]
    public void DataVersion_AggregatesMultipleSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var s1 = axes.AddSeries(new StreamingLineSeries(100));
        var s2 = axes.AddSeries(new StreamingScatterSeries(100));
        var sf = new StreamingFigure(figure);

        s1.AppendPoint(1, 2);
        long v1 = sf.DataVersion;
        s2.AppendPoint(3, 4);
        Assert.True(sf.DataVersion > v1);
        sf.Dispose();
    }

    [Fact]
    public async Task RenderRequested_FiresAfterThrottle()
    {
        var (sf, series) = CreateSimple();
        sf.MinRenderInterval = TimeSpan.FromMilliseconds(50);

        var tcs = new TaskCompletionSource();
        sf.RenderRequested += () => tcs.TrySetResult();

        series.AppendPoint(1, 2);

        // Should fire within 200ms (throttle is 50ms)
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        Assert.Equal(tcs.Task, completed);
        sf.Dispose();
    }

    [Fact]
    public async Task RenderRequested_DoesNotFireWhenNoDataChange()
    {
        var (sf, _) = CreateSimple();
        sf.MinRenderInterval = TimeSpan.FromMilliseconds(50);

        bool fired = false;
        sf.RenderRequested += () => fired = true;

        // Wait longer than throttle without appending data
        await Task.Delay(200);
        Assert.False(fired);
        sf.Dispose();
    }

    [Fact]
    public void ApplyAxisScaling_AutoScale_SetsLimitsToDataRange()
    {
        var (sf, series) = CreateSimple();
        sf.DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.AutoScale(), new AxisScaleMode.AutoScale());

        series.AppendPoints([1.0, 5.0, 10.0], [2.0, 8.0, 4.0]);
        sf.ApplyAxisScaling();

        Assert.Equal(1.0, sf.Figure.SubPlots[0].XAxis.Min);
        Assert.Equal(10.0, sf.Figure.SubPlots[0].XAxis.Max);
        sf.Dispose();
    }

    [Fact]
    public void ApplyAxisScaling_SlidingWindow_SetsXToLastWindowSize()
    {
        var (sf, series) = CreateSimple();
        sf.DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.SlidingWindow(5.0), new AxisScaleMode.AutoScale());

        series.AppendPoints([1.0, 3.0, 7.0, 10.0], [1.0, 1.0, 1.0, 1.0]);
        sf.ApplyAxisScaling();

        Assert.Equal(5.0, sf.Figure.SubPlots[0].XAxis.Min);  // 10 - 5
        Assert.Equal(10.0, sf.Figure.SubPlots[0].XAxis.Max);
        sf.Dispose();
    }

    [Fact]
    public void ApplyAxisScaling_Fixed_DoesNotChangeLimits()
    {
        var (sf, series) = CreateSimple();
        sf.DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.Fixed(), new AxisScaleMode.Fixed());
        sf.Figure.SubPlots[0].XAxis.Min = 0;
        sf.Figure.SubPlots[0].XAxis.Max = 100;
        sf.Figure.SubPlots[0].YAxis.Min = 0;
        sf.Figure.SubPlots[0].YAxis.Max = 50;

        series.AppendPoints([200.0, 300.0], [100.0, 200.0]);
        sf.ApplyAxisScaling();

        Assert.Equal(0.0, sf.Figure.SubPlots[0].XAxis.Min);
        Assert.Equal(100.0, sf.Figure.SubPlots[0].XAxis.Max);
        sf.Dispose();
    }

    [Fact]
    public void ApplyAxisScaling_StickyRight_ScrollsWhenAtEdge()
    {
        var (sf, series) = CreateSimple();
        sf.DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.StickyRight(5.0), new AxisScaleMode.AutoScale());

        series.AppendPoints([1.0, 3.0, 5.0], [1.0, 1.0, 1.0]);
        sf.Figure.SubPlots[0].XAxis.Max = 5.0; // simulate being at the edge
        sf.ApplyAxisScaling();

        Assert.Equal(0.0, sf.Figure.SubPlots[0].XAxis.Min); // 5 - 5
        Assert.Equal(5.0, sf.Figure.SubPlots[0].XAxis.Max);
        sf.Dispose();
    }

    [Fact]
    public void ApplyAxisScaling_EmptyData_DoesNotCrash()
    {
        var (sf, _) = CreateSimple();
        sf.ApplyAxisScaling(); // should not throw
        sf.Dispose();
    }

    [Fact]
    public void Dispose_StopsTimer()
    {
        var (sf, series) = CreateSimple();
        sf.Dispose();

        bool fired = false;
        sf.RenderRequested += () => fired = true;
        series.AppendPoint(1, 2);
        Thread.Sleep(200);
        Assert.False(fired);
    }

    [Fact]
    public void DefaultConfig_IsSlidingWindowAutoScale()
    {
        var (sf, _) = CreateSimple();
        Assert.IsType<AxisScaleMode.SlidingWindow>(sf.DefaultConfig.XMode);
        Assert.IsType<AxisScaleMode.AutoScale>(sf.DefaultConfig.YMode);
        sf.Dispose();
    }

    [Fact]
    public void PerAxesConfig_OverridesDefault()
    {
        var (sf, series) = CreateSimple();
        sf.AxesConfigs[0] = new StreamingAxesConfig(new AxisScaleMode.Fixed(), new AxisScaleMode.Fixed());
        sf.Figure.SubPlots[0].XAxis.Min = 0;
        sf.Figure.SubPlots[0].XAxis.Max = 10;

        series.AppendPoints([100.0, 200.0], [1.0, 2.0]);
        sf.ApplyAxisScaling();

        Assert.Equal(0.0, sf.Figure.SubPlots[0].XAxis.Min); // Fixed: unchanged
        sf.Dispose();
    }

    [Fact]
    public void RequestRender_FiresImmediately()
    {
        var (sf, _) = CreateSimple();
        bool fired = false;
        sf.RenderRequested += () => fired = true;
        sf.RequestRender();
        Assert.True(fired);
        sf.Dispose();
    }

    [Fact]
    public void MinRenderInterval_DefaultIs33ms()
    {
        var (sf, _) = CreateSimple();
        Assert.Equal(TimeSpan.FromMilliseconds(33), sf.MinRenderInterval);
        sf.Dispose();
    }
}
