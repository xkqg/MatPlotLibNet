// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
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

    /// <summary>A sampled signal is a streaming kind too: its appends must tell the clock a frame is due, or a figure that holds one
    /// is drawn once and never again.</summary>
    [Fact]
    public void DataVersion_CountsAStreamingSignal()
    {
        var figure = new Figure();
        var signal = figure.AddSubPlot().AddSeries(new StreamingSignalSeries(100));
        using var sf = new StreamingFigure(figure);

        long before = sf.DataVersion;
        signal.AppendSample(1);

        Assert.Equal(before + 1, sf.DataVersion);
    }

    /// <summary>Every seat a series can stand on is counted: the Y axes on the right, the ones stacked on the left past the primary,
    /// the top X axis and the insets. A stream on any of them that the clock does not count is never drawn again.</summary>
    public static TheoryData<string> Seats { get; } = ["right Y axis", "left Y axis past the primary", "top X axis", "inset", "inset of an inset"];

    [Theory]
    [MemberData(nameof(Seats))]
    public void DataVersion_CountsAStreamOnEverySeat(string seat)
    {
        var figure = new Figure();
        var stream = Seat(figure.AddSubPlot(), seat, new StreamingLineSeries(100));
        using var sf = new StreamingFigure(figure);

        long before = sf.DataVersion;
        stream.AppendPoint(1, 2);

        Assert.Equal(before + 1, sf.DataVersion);
    }

    /// <summary>The clock's question is the sum of the versions of every series the subplots and their insets show: the walk agrees
    /// with <see cref="Axes.AllSeries"/>, the owner of "every series of a subplot".</summary>
    [Fact]
    public void DataVersion_IsTheSumOfTheVersionsOfEverySeriesShown()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        foreach (var seat in Seats) Seat(axes, seat, new StreamingLineSeries(10)).AppendPoints([1.0, 2.0], [3.0, 4.0]);
        axes.AddSeries(new StreamingSignalSeries(10)).AppendSample(1);
        axes.AddSeries(new StreamingCandlestickSeries(10)).AppendBar(1, 2, 0, 1);
        axes.AddSeries(new StreamingScatterSeries(10)).AppendPoint(1, 1);
        axes.AddSeries(new LineSeries([1.0, 2.0], [1.0, 2.0]));
        using var sf = new StreamingFigure(figure);

        long expected = Shown(figure.SubPlots).Sum(series => series switch
        {
            IStreamingSeries s => s.Version,
            IStreamingOhlcSeries o => o.Version,
            StreamingSignalSeries g => g.Version,
            _ => 0,
        });

        Assert.Equal(8, expected);
        Assert.Equal(expected, sf.DataVersion);
    }

    /// <summary>The clock asks at every tick, thirty times a second per figure: asking allocates nothing.</summary>
    [Fact]
    public void DataVersion_AllocatesNothing()
    {
        var figure = new Figure();
        for (int p = 0; p < 3; p++)
        {
            var axes = figure.AddSubPlot();
            foreach (var seat in Seats) Seat(axes, seat, new StreamingLineSeries(10)).AppendPoint(1, 1);
            axes.AddSeries(new StreamingSignalSeries(10)).AppendSample(1);
        }

        using var sf = new StreamingFigure(figure) { MinRenderInterval = TimeSpan.FromHours(1) };
        _ = sf.DataVersion;

        long before = GC.GetAllocatedBytesForCurrentThread();
        long version = sf.DataVersion;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(version > 0);
        Assert.Equal(0, allocated);
    }

    /// <summary>The red team's repro: a streaming figure that holds a sampled signal asks for a frame when a sample comes in.</summary>
    [Fact]
    public async Task RenderRequested_FiresForAStreamingSignal()
    {
        var figure = new Figure();
        var signal = figure.AddSubPlot().AddSeries(new StreamingSignalSeries(100));
        using var sf = new StreamingFigure(figure) { MinRenderInterval = TimeSpan.FromMilliseconds(20) };
        var fired = new TaskCompletionSource();
        sf.RenderRequested += () => fired.TrySetResult();

        signal.AppendSample(1);

        var completed = await Task.WhenAny(fired.Task, Task.Delay(5000, TestContext.Current.CancellationToken));
        Assert.Equal(fired.Task, completed);
    }

    private static T Seat<T>(Axes axes, string seat, T series) where T : ISeries => seat switch
    {
        "right Y axis" => axes.YAxes.Right.Add().AddSeries(series),
        "left Y axis past the primary" => axes.YAxes.Left.Add().AddSeries(series),
        "top X axis" => axes.AddXSecondarySeries(series),
        "inset" => axes.AddInset(0.1, 0.1, 0.3, 0.3).AddSeries(series),
        "inset of an inset" => axes.AddInset(0.5, 0.5, 0.3, 0.3).AddInset(0.1, 0.1, 0.5, 0.5).AddSeries(series),
        _ => throw new ArgumentOutOfRangeException(nameof(seat), seat, null),
    };

    private static IEnumerable<ISeries> Shown(IEnumerable<Axes> axes) =>
        axes.SelectMany(a => a.AllSeries.Concat(Shown(a.Insets)));

    [Fact]
    public async Task RenderRequested_FiresAfterThrottle()
    {
        var (sf, series) = CreateSimple();
        sf.MinRenderInterval = TimeSpan.FromMilliseconds(50);

        var tcs = new TaskCompletionSource();
        sf.RenderRequested += () => tcs.TrySetResult();

        series.AppendPoint(1, 2);

        // Should fire within 200ms (throttle is 50ms)
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000, TestContext.Current.CancellationToken));
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
        await Task.Delay(200, TestContext.Current.CancellationToken);
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
    public void ANewStreamingFigure_SlidesItsXWindow_AndScalesItsYToTheData()
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
    public void ANewStreamingFigure_DrawsAtMostOnceEvery33Milliseconds()
    {
        var (sf, _) = CreateSimple();
        Assert.Equal(TimeSpan.FromMilliseconds(33), sf.MinRenderInterval);
        sf.Dispose();
    }

    [Fact]
    public void MinRenderInterval_SetAfterConstruction_SlowsTheRenderRequests()
    {
        // The property is the documented minimum interval between renders, and the only way to ask for a
        // slower chart. The timer was armed once in the constructor and never re-armed, so a caller who set
        // it kept getting 33 ms and could not find out: the property returned the value it was given while
        // the cadence ignored it. `BuildStreaming(renderInterval)` sets it the same way, so every caller of
        // that overload was affected too.
        var (sf, series) = CreateSimple();
        using var _ = sf;

        sf.MinRenderInterval = TimeSpan.FromSeconds(10);

        int fired = 0;
        sf.RenderRequested += () => Interlocked.Increment(ref fired);

        for (int i = 0; i < 10; i++)
        {
            series.AppendPoint(i, i);
        }

        // At the default 33 ms this window holds a dozen ticks; at ten seconds it holds none.
        Thread.Sleep(400);

        Assert.Equal(0, Volatile.Read(ref fired));
    }

    [Fact]
    public async Task MinRenderInterval_LoweredAgain_LetsTheRenderRequestsResume()
    {
        // The other direction, so the fix cannot be "stop the timer and never restart it".
        var (sf, series) = CreateSimple();
        using var _ = sf;

        sf.MinRenderInterval = TimeSpan.FromSeconds(10);
        sf.MinRenderInterval = TimeSpan.FromMilliseconds(20);

        var fired = new TaskCompletionSource();
        sf.RenderRequested += () => fired.TrySetResult();
        series.AppendPoint(1, 1);

        // Wait for the event instead of sleeping a fixed time: a runner that is busy measuring coverage can starve the
        // 20 ms timer for longer than any short window, while an interval still held at ten seconds delivers nothing
        // within five.
        var completed = await Task.WhenAny(fired.Task, Task.Delay(5000, TestContext.Current.CancellationToken));

        Assert.Equal(fired.Task, completed);
    }
}
