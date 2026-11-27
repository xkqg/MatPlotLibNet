// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Tests.Models.Streaming;

/// <summary>The worker of a <see cref="StreamingFigure"/>: the clock that asks for a render, what it does when a render
/// fails, and that it never runs two renders at once. Joins the diagnostics collection because a failing render is
/// reported on the process-wide <see cref="ChartDiagnostics.Emitted"/>.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public sealed class StreamingFigureWorkerTests
{
    [Fact]
    public void ApplyAxisScaling_OverASeriesThatReadsItsAxes_ScalesInsteadOfThrowing()
    {
        // A candlestick's range reads the axes it is on (its x extent starts at the axes' lower limit). The scale mode
        // asked every series for its range with no axes at all, so a streaming figure that also held one threw a
        // NullReferenceException on every frame.
        using var sf = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Candlestick([10.0, 11.0, 12.0], [12.0, 13.0, 14.0], [9.0, 10.0, 11.0], [11.0, 12.0, 13.0]))
            .BuildStreaming();
        sf.DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.AutoScale(), new AxisScaleMode.AutoScale());

        sf.ApplyAxisScaling();

        var axes = sf.Figure.SubPlots[0];
        Assert.Equal(0.0, axes.XAxis.Min);
        Assert.Equal(3.0, axes.XAxis.Max);
        Assert.True(axes.YAxis.Min < 9.0);
        Assert.True(axes.YAxis.Max > 14.0);
    }

    [Fact]
    public async Task ARenderThatThrows_IsReportedAndTheClockGoesOn()
    {
        // The clock runs on a pool thread, where an exception nobody catches ends the process. A subscriber that fails
        // costs that frame: the failure is reported as a diagnostic, and the next change of the data is rendered as usual.
        var figure = new Figure();
        var series = figure.AddSubPlot().AddSeries(new StreamingLineSeries(100));
        using var sf = new StreamingFigure(figure) { MinRenderInterval = TimeSpan.FromMilliseconds(5) };

        int calls = 0;
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sf.RenderRequested += () =>
        {
            if (Interlocked.Increment(ref calls) == 1) throw new InvalidOperationException("render failed");
            second.TrySetResult();
        };

        using var capture = new DiagnosticsCapture();
        series.AppendPoint(1, 1);
        var diagnostic = await capture.WhenAnnounced(nameof(StreamingFigure)).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.IsType<InvalidOperationException>(diagnostic.Exception);

        series.AppendPoint(2, 2);
        await second.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ARenderLongerThanTheInterval_NeverRunsBesideTheNextTick()
    {
        // A periodic timer starts its next callback while the last one still runs. A render slower than the interval then
        // ran beside the next one, and on the server one more pool thread waited on the session's gate every interval.
        var figure = new Figure();
        var series = figure.AddSubPlot().AddSeries(new StreamingLineSeries(10_000));
        using var sf = new StreamingFigure(figure) { MinRenderInterval = TimeSpan.FromMilliseconds(5) };

        int running = 0, most = 0, renders = 0;
        sf.RenderRequested += () =>
        {
            int now = Interlocked.Increment(ref running);
            int seen;
            while ((seen = Volatile.Read(ref most)) < now && Interlocked.CompareExchange(ref most, now, seen) != seen) { }
            Thread.Sleep(50);
            Interlocked.Decrement(ref running);
            Interlocked.Increment(ref renders);
        };

        var until = DateTime.UtcNow.AddMilliseconds(500);
        for (int i = 0; DateTime.UtcNow < until; i++)
        {
            series.AppendPoint(i, i);
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }

        Assert.True(Volatile.Read(ref renders) > 0);
        Assert.Equal(1, Volatile.Read(ref most));
    }

    [Fact]
    public async Task AfterDispose_NoTickStartsARender()
    {
        var figure = new Figure();
        var series = figure.AddSubPlot().AddSeries(new StreamingLineSeries(100));
        var sf = new StreamingFigure(figure) { MinRenderInterval = TimeSpan.FromMilliseconds(5) };
        int fired = 0;
        sf.RenderRequested += () => Interlocked.Increment(ref fired);

        sf.Dispose();
        series.AppendPoint(1, 1);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Equal(0, Volatile.Read(ref fired));
    }
}
