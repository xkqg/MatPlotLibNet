// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The WPF, Avalonia, Uno and MAUI streaming controls each kept their own copy of the same wiring: listen to the
/// figure they are given, let go of the one they are taken off of, set the limits from the data before the first draw, and,
/// when the figure asks to be drawn (on a pool thread), move to the control's thread, set the limits and draw. They differed
/// only in how they reach their thread and how they draw. The follower is that wiring once; a control hands it the two.</summary>
public class StreamingFigureFollowerTests
{
    private static readonly Rect Area = new(10, 10, 200, 100);

    private static int Listeners(StreamingFigure figure) =>
        (typeof(StreamingFigure).GetField(nameof(StreamingFigure.RenderRequested), BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(figure) as Delegate)?.GetInvocationList().Length ?? 0;

    private static (StreamingFigure Stream, StreamingLineSeries Series) Streaming()
    {
        var figure = new Figure { ChartId = "c" };
        var series = figure.AddSubPlot().AddSeries(new StreamingLineSeries(100));
        for (int i = 0; i < 10; i++) series.AppendPoint(i, i * 2.0);
        var stream = new StreamingFigure(figure)
        {
            DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.AutoScale(), new AxisScaleMode.AutoScale()),
        };
        return (stream, series);
    }

    [Fact]
    public void AFigureFollowed_IsListenedTo_AndItsLimitsFollowTheDataBeforeTheFirstDraw()
    {
        var (stream, _) = Streaming();
        using var _ = stream;
        var follower = new StreamingFigureFollower(new PostedWork().Post, () => { });

        follower.Follow(stream);

        Assert.Same(stream, follower.Figure);
        Assert.Equal(1, Listeners(stream));
        Assert.Equal(9.0, stream.Figure.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public void AnotherFigure_LetsGoOfTheFirst_AndNoneLetsGoOfAny()
    {
        var (first, _) = Streaming();
        var (second, _) = Streaming();
        using var a = first;
        using var b = second;
        var follower = new StreamingFigureFollower(new PostedWork().Post, () => { });
        follower.Follow(first);

        follower.Follow(second);
        Assert.Equal(0, Listeners(first));
        Assert.Equal(1, Listeners(second));

        follower.Follow(null);
        Assert.Equal(0, Listeners(second));
        Assert.Null(follower.Figure);
        Assert.Null(Record.Exception(() => follower.Follow(null)));   // letting go twice is harmless
    }

    [Fact]
    public void ARenderRequest_IsPostedToTheControlsThread_WhereTheLimitsFollowTheDataBeforeTheDraw()
    {
        var (stream, series) = Streaming();
        using var _ = stream;
        var thread = new PostedWork();
        var limitAtDraw = new List<double?>();
        var follower = new StreamingFigureFollower(thread.Post,
            () => limitAtDraw.Add(stream.Figure.SubPlots[0].XAxis.Max));
        follower.Follow(stream);
        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);

        stream.RequestRender();
        Assert.Empty(limitAtDraw);                                  // nothing ran on the pool thread
        Assert.Equal(9.0, stream.Figure.SubPlots[0].XAxis.Max);

        thread.RunAll();
        Assert.Equal([19.0], limitAtDraw);                          // the limits were set, then the control drew
    }

    [Fact]
    public void ARequestStillWaiting_WhenTheFigureIsLetGo_DrawsWithoutTouchingIt()
    {
        var (stream, series) = Streaming();
        using var _ = stream;
        var thread = new PostedWork();
        int draws = 0;
        var follower = new StreamingFigureFollower(thread.Post, () => draws++);
        follower.Follow(stream);
        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        stream.RequestRender();

        follower.Follow(null);
        thread.RunAll();

        Assert.Equal(1, draws);
        Assert.Equal(9.0, stream.Figure.SubPlots[0].XAxis.Max);    // a figure let go of is not written to
    }

    [Fact]
    public void AGesture_SetsTheLimitsFromTheData_OnlyWhileAFigureIsFollowed()
    {
        var (stream, series) = Streaming();
        using var _ = stream;
        int draws = 0;
        var follower = new StreamingFigureFollower(new PostedWork().Post, () => draws++);
        // A streaming control gives its host the follower's redraw as its repaint: a gesture that changed the figure sets the limits
        // from the data, then draws.
        var host = new InteractionHost(follower.Redraw);
        follower.Follow(stream);
        host.Update(stream.Figure, ChartLayout.Create(stream.Figure, [Area]));

        // A zoom moves the axes, and Home hands it back to its scale mode at once: the limits follow the data that is there.
        host.HandleScroll(new ScrollInputArgs(110, 60, 0, -3.0));
        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        host.HandleKeyDown(new KeyInputArgs("Home"));
        Assert.Equal(19.0, stream.Figure.SubPlots[0].XAxis.Max);
        Assert.Equal(2, draws);                                      // the zoom and Home each drew once

        follower.Follow(null);
        for (int i = 20; i < 30; i++) series.AppendPoint(i, i * 2.0);
        host.HandleScroll(new ScrollInputArgs(110, 60, 0, -3.0));
        host.HandleKeyDown(new KeyInputArgs("Home"));
        Assert.Equal(19.0, stream.Figure.SubPlots[0].XAxis.Max);    // a figure let go of no longer follows the gestures
    }
}
