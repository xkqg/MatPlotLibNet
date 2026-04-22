// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>Verifies <see cref="MauiAnimationTimer"/> and
/// <see cref="MplChartView"/> animation source wiring.</summary>
public class MauiAnimationTimerTests
{
    [Fact]
    public void ImplementsIAnimationTimer()
    {
        IAnimationTimer timer = new MauiAnimationTimer();
        Assert.NotNull(timer);
    }

    [Fact]
    public void DefaultInterval_Is16ms()
    {
        var timer = new MauiAnimationTimer();
        Assert.Equal(TimeSpan.FromMilliseconds(16), timer.Interval);
    }

    [Fact]
    public void IntervalCanBeChanged()
    {
        var timer = new MauiAnimationTimer();
        timer.Interval = TimeSpan.FromMilliseconds(33);
        Assert.Equal(TimeSpan.FromMilliseconds(33), timer.Interval);
    }

    [Fact]
    public void StopWithoutStart_DoesNotThrow()
    {
        using var timer = new MauiAnimationTimer();
        var ex = Record.Exception(timer.Stop);
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var timer = new MauiAnimationTimer();
        var ex = Record.Exception(timer.Dispose);
        Assert.Null(ex);
    }
}

public class MplChartViewAnimationTests
{
    [Fact]
    public void AnimationSource_DefaultsToNull()
    {
        var view = new MplChartView();
        Assert.Null(view.AnimationSource);
    }

    [Fact]
    public void AnimationSource_CanBeSet()
    {
        var view = new MplChartView();
        var fake = new FakeAnimationSource();
        view.AnimationSource = fake;
        Assert.Same(fake, view.AnimationSource);
    }

    [Fact]
    public void AnimationSource_Set_SubscribesToFrameReady()
    {
        var view = new MplChartView();
        var fake = new FakeAnimationSource();
        view.AnimationSource = fake;
        Assert.Equal(1, fake.SubscriberCount);
    }

    [Fact]
    public void AnimationSource_Replaced_UnsubscribesOld()
    {
        var view = new MplChartView();
        var fake1 = new FakeAnimationSource();
        var fake2 = new FakeAnimationSource();
        view.AnimationSource = fake1;
        view.AnimationSource = fake2;
        Assert.Equal(0, fake1.SubscriberCount);
        Assert.Equal(1, fake2.SubscriberCount);
    }

    [Fact]
    public void AnimationSource_SetToNull_UnsubscribesOld()
    {
        var view = new MplChartView();
        var fake = new FakeAnimationSource();
        view.AnimationSource = fake;
        view.AnimationSource = null;
        Assert.Equal(0, fake.SubscriberCount);
    }

    private sealed class FakeAnimationSource : IAnimationSource
    {
        public event EventHandler<Figure>? FrameReady;
        public int SubscriberCount => FrameReady?.GetInvocationList().Length ?? 0;
    }
}
