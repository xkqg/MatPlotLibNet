// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Threading;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>What the chart control says of itself is where its figure comes from: the <see cref="MplChartControl.Figure"/> it is
/// handed, or each frame of its animation source. An application that swaps the chart for a fresh one (a replay that rebuilds its
/// timeline on every tick, discussion #22) must see pan and zoom act on the figure the reader is looking at.</summary>
public class MplChartControlTests
{
    private static Figure LineFigure() =>
        Plt.Create().Plot([0.0, 1, 2, 3, 4], [0.0, 1, 4, 9, 16]).Build();

    [Fact]
    public void ZoomOnTheFigureTheControlDrew_ActsOnIt_AndAfterTheFigureIsReplaced_OnTheNewOne() => new Action(() =>
    {
        var first = LineFigure();
        var second = LineFigure();
        var control = new MplChartControl { Figure = first, IsInteractive = true };
        var wheel = new ScrollInputArgs(400, 300, 0, 3.0, ModifierKeys.None);

        control.PaintedOnce().HandleScroll(wheel);
        Assert.NotNull(first.SubPlots[0].XAxis.Min);
        first.SubPlots[0].XAxis.Min = null;

        control.Figure = second;
        control.PaintedOnce().HandleScroll(wheel);

        Assert.NotNull(second.SubPlots[0].XAxis.Min);
        Assert.Null(first.SubPlots[0].XAxis.Min);
    }).OnStaThread();

    [Fact]
    public void AFrameOfTheAnimationSource_IsShownOnTheDispatcher_AndALetGoSourceShowsNone() => new Action(() =>
    {
        var control = new MplChartControl();
        var source = new FakeAnimationSource();
        control.AnimationSource = source;
        var first = new Figure();

        source.Raise(first);
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

        Assert.Same(source, control.AnimationSource);
        Assert.Same(first, control.Figure);

        control.AnimationSource = null;
        source.Raise(new Figure());
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

        Assert.Equal(0, source.SubscriberCount);
        Assert.Same(first, control.Figure);
    }).OnStaThread();
}
