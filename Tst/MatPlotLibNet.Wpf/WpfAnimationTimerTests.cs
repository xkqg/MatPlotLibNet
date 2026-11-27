// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Threading;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>The WPF timer hands the base's start, stop and interval to its dispatcher timer; what every timer does is held once by
/// the core's <c>AnimationTimerTests</c>.</summary>
public class WpfAnimationTimerTests
{
    [Fact]
    public void TheTimer_RetimedAndStarted_TicksOnTheDispatcher_WithItselfAsTheSender_UntilItIsStopped() => new Action(() =>
    {
        using var timer = new WpfAnimationTimer { Interval = TimeSpan.FromMilliseconds(1) };
        object? sender = null;
        timer.Tick += (s, _) =>
        {
            sender = s;
            timer.Stop();
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        };
        timer.Start();

        var guard = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        guard.Tick += (_, _) => Dispatcher.CurrentDispatcher.InvokeShutdown();
        guard.Start();
        Dispatcher.Run();

        Assert.Same(timer, sender);
    }).OnStaThread();
}
