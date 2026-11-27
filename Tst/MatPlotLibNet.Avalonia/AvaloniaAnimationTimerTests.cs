// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>The Avalonia timer hands the base's start, stop and interval to its dispatcher timer; what every timer does is held once
/// by the core's <c>AnimationTimerTests</c>.</summary>
public class AvaloniaAnimationTimerTests
{
    [Fact]
    public void TheDispatcherTimer_IsRetimedStartedAndStopped_AndTheTimerKeepsTheIntervalItWasTold()
    {
        using var timer = new AvaloniaAnimationTimer { Interval = TimeSpan.FromSeconds(10) };

        timer.Start();
        timer.Stop();

        Assert.Equal(TimeSpan.FromSeconds(10), timer.Interval);
    }
}
