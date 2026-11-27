// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Windows;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>
/// WPF was the only one of the four native heads without a streaming control: Avalonia, MAUI and Uno each
/// ship one beside the ordinary chart, so a reader binds a <see cref="StreamingFigure"/> once and the chart
/// follows the data (discussion #20). What the streaming control says of itself is where its figure comes from: it follows the
/// figure it is given, lets go of the one it is given up and of the last one when it is unloaded (the figure outlives the control,
/// so a control that never detaches keeps being invalidated after it is gone), and is painted again through the follower, which
/// sets the limits from the data before the draw.
/// </summary>
public class MplStreamingChartControlTests
{
    /// <summary>How many handlers the figure is holding — the only way to see a leak from outside.</summary>
    private static int SubscriberCount(StreamingFigure sf)
    {
        var field = typeof(StreamingFigure).GetField(nameof(StreamingFigure.RenderRequested),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (field.GetValue(sf) as Delegate)?.GetInvocationList().Length ?? 0;
    }

    private static StreamingFigure NewStreamingFigure()
    {
        var figure = new Figure();
        figure.AddSubPlot().AddSeries(new StreamingLineSeries(100));
        return new StreamingFigure(figure);
    }

    [Fact]
    public void TheBoundFigureIsFollowed_TheOneReplacedIsLetGo_AndUnloadingLetsGoOfTheLast() => new Action(() =>
    {
        using var first = NewStreamingFigure();
        using var second = NewStreamingFigure();
        var control = new MplStreamingChartControl { StreamingFigure = first };
        Assert.Same(first, control.StreamingFigure);
        Assert.Equal(1, SubscriberCount(first));

        control.StreamingFigure = second;
        Assert.Equal(0, SubscriberCount(first));
        Assert.Equal(1, SubscriberCount(second));

        control.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Assert.Equal(0, SubscriberCount(second));
    }).OnStaThread();

    [Fact]
    public void APaint_WritesNothingIntoTheFigure_TheLimitsFollowTheDataBeforeIt() => new Action(() =>
    {
        var figure = new Figure();
        var series = figure.AddSubPlot().AddSeries(new StreamingLineSeries(100));
        for (int i = 0; i < 10; i++) series.AppendPoint(i, i * 2.0);
        using var stream = new StreamingFigure(figure)
        {
            DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.AutoScale(), new AxisScaleMode.AutoScale()),
        };
        var control = new MplStreamingChartControl { StreamingFigure = stream };
        Assert.Equal(9.0, figure.SubPlots[0].XAxis.Max);   // bound: the limits follow the data that is there

        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        var axes = figure.SubPlots[0];
        var before = (axes.XAxis.Min, axes.XAxis.Max, axes.YAxis.Min, axes.YAxis.Max);
        control.PaintedOnce();

        // The paint drew and wrote no limit: the next request to draw moves them, on the dispatcher, before that draw.
        Assert.Equal(before, (axes.XAxis.Min, axes.XAxis.Max, axes.YAxis.Min, axes.YAxis.Max));
        Assert.Equal(9.0, axes.XAxis.Max);
    }).OnStaThread();

    [Fact]
    public void AnInteractiveStreamingChart_StaysWhereTheReaderZoomedItWhileDataKeepsArriving() => new Action(() =>
    {
        var figure = new Figure();
        var series = figure.AddSubPlot().AddSeries(new StreamingLineSeries(100));
        figure.ChartId = "c";
        for (int i = 0; i < 10; i++) series.AppendPoint(i, i * 2.0);
        using var stream = new StreamingFigure(figure)
        {
            DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.AutoScale(), new AxisScaleMode.AutoScale()),
        };
        var control = new MplStreamingChartControl { StreamingFigure = stream, IsInteractive = true };
        var host = control.PaintedOnce();

        host.HandleScroll(new ScrollInputArgs(400, 300, 0, -3.0, ModifierKeys.None));
        double? zoomedMax = figure.SubPlots[0].XAxis.Max;
        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        control.PaintedOnce();

        Assert.NotNull(zoomedMax);
        Assert.Equal(zoomedMax, figure.SubPlots[0].XAxis.Max);   // the next frame did not overrule the reader

        host.HandleKeyDown(new KeyInputArgs("Home"));
        control.PaintedOnce();
        Assert.Equal(19.0, figure.SubPlots[0].XAxis.Max);        // Home handed the axes back to the scale mode
    }).OnStaThread();
}
