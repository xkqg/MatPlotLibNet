// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Maui;
using NSubstitute;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>B3 (council main@a5fe781) — <see cref="MplStreamingChartView"/> subscribed to
/// <see cref="StreamingFigure.RenderRequested"/> on property change but never detached when the
/// view itself was torn down (only on a subsequent StreamingFigure property swap). That leaked
/// the view via the figure's event (figure outlives view) and kept invalidation callbacks firing
/// into a dead view. Fixed by unsubscribing in <c>OnHandlerChanging</c> when the platform handler
/// is being removed (MAUI's Loaded/Unloaded equivalent for detecting teardown without a running
/// native platform), mirroring the Avalonia exemplar's <c>OnDetachedFromVisualTree</c>.</summary>
public class MplStreamingChartViewTests
{
    private static int SubscriberCount(StreamingFigure sf)
    {
        var field = typeof(StreamingFigure).GetField(nameof(StreamingFigure.RenderRequested),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (field.GetValue(sf) as Delegate)?.GetInvocationList().Length ?? 0;
    }

    private static StreamingFigure NewStreamingFigure()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        return new StreamingFigure(figure);
    }

    [Fact]
    public void StreamingFigureProperty_Set_SubscribesToRenderRequested()
    {
        using var sf = NewStreamingFigure();
        var view = new MplStreamingChartView { StreamingFigure = sf };

        Assert.Equal(1, SubscriberCount(sf));
    }

    [Fact]
    public void StreamingFigureChange_UnsubscribesOldFigure()
    {
        using var sf1 = NewStreamingFigure();
        using var sf2 = NewStreamingFigure();
        var view = new MplStreamingChartView { StreamingFigure = sf1 };

        view.StreamingFigure = sf2;

        Assert.Equal(0, SubscriberCount(sf1));
        Assert.Equal(1, SubscriberCount(sf2));
    }

    [Fact]
    public void StreamingFigureCleared_LetsGoOfTheFigure_AndLeavesTheLastPicture()
    {
        using var sf = NewStreamingFigure();
        var view = new MplStreamingChartView { StreamingFigure = sf };
        Assert.Same(sf.Figure, view.Figure);

        view.StreamingFigure = null;

        Assert.Equal(0, SubscriberCount(sf));
        Assert.Same(sf.Figure, view.Figure);
    }

    /// <summary>The streaming view repaints after a gesture through its follower: the limits follow the data that arrived since the
    /// last draw before the view is drawn, so Home hands the axes back to the scale mode that follows the data at once.</summary>
    [Fact]
    public void AGesture_SetsTheLimitsFromTheData_BeforeTheViewIsDrawnAgain()
    {
        var figure = new Figure { ChartId = "c" };
        var series = figure.AddSubPlot().AddSeries(new MatPlotLibNet.Models.Series.Streaming.StreamingLineSeries(100));
        for (int i = 0; i < 10; i++) series.AppendPoint(i, i * 2.0);
        using var sf = new StreamingFigure(figure)
        {
            DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.AutoScale(), new AxisScaleMode.AutoScale()),
        };
        var view = new MplStreamingChartView { IsInteractive = true, StreamingFigure = sf };
        view.Host.Update(figure, ChartLayout.Create(figure, [new MatPlotLibNet.Rendering.Rect(10, 10, 200, 100)]));

        // A zoom moves the axes; Home hands it back to its scale mode, and the limits follow the data that is there then.
        view.Host.HandleScroll(new ScrollInputArgs(110, 60, 0, -3.0));
        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        view.Host.HandleKeyDown(new KeyInputArgs("Home"));

        Assert.Equal(19.0, figure.SubPlots[0].XAxis.Max);
    }

    /// <summary>The repro for B3: simulate the view being attached to a platform handler and
    /// then torn down (handler removed) WITHOUT the StreamingFigure property ever changing.
    /// Pre-fix, nothing unsubscribed in this path — the figure kept a live reference to the
    /// dead view's handler forever.</summary>
    [Fact]
    public void Maui_OnUnload_UnsubscribesRenderRequested()
    {
        using var sf = NewStreamingFigure();
        var view = new MplStreamingChartView { StreamingFigure = sf };
        Assert.Equal(1, SubscriberCount(sf));

        // Simulate attach then detach from the native platform handler.
        view.Handler = Substitute.For<IViewHandler>();
        view.Handler = null;

        Assert.Equal(0, SubscriberCount(sf));
    }
}
