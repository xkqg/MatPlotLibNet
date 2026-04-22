// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>Verifies <see cref="IAnimationTimer"/> contract and
/// <see cref="SystemThreadingAnimationTimer"/> behavior.</summary>
public class AnimationTimerTests
{
    // ── IAnimationTimer contract ──────────────────────────────────────────────

    [Fact]
    public void SystemTimer_ImplementsInterface()
    {
        IAnimationTimer timer = new SystemThreadingAnimationTimer();
        Assert.NotNull(timer);
    }

    [Fact]
    public void SystemTimer_DefaultInterval_Is16ms()
    {
        var timer = new SystemThreadingAnimationTimer();
        Assert.Equal(TimeSpan.FromMilliseconds(16), timer.Interval);
    }

    [Fact]
    public void SystemTimer_IntervalCanBeChanged()
    {
        var timer = new SystemThreadingAnimationTimer();
        timer.Interval = TimeSpan.FromMilliseconds(50);
        Assert.Equal(TimeSpan.FromMilliseconds(50), timer.Interval);
    }

    [Fact]
    public async Task SystemTimer_FiresTick_AfterStart()
    {
        using var timer = new SystemThreadingAnimationTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        int ticks = 0;
        timer.Tick += (_, _) => Interlocked.Increment(ref ticks);
        timer.Start();
        await Task.Delay(100, TestContext.Current.CancellationToken);
        timer.Stop();
        Assert.True(ticks >= 2, $"Expected at least 2 ticks, got {ticks}");
    }

    [Fact]
    public async Task SystemTimer_StopPreventsMoreTicks()
    {
        using var timer = new SystemThreadingAnimationTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        int ticks = 0;
        timer.Tick += (_, _) => Interlocked.Increment(ref ticks);
        timer.Start();
        await Task.Delay(60, TestContext.Current.CancellationToken);
        timer.Stop();
        int snapshot = ticks;
        await Task.Delay(60, TestContext.Current.CancellationToken);
        // After stop, tick count must not grow (allow ±1 for in-flight tick)
        Assert.True(ticks <= snapshot + 1, $"Ticks grew after Stop: {snapshot} → {ticks}");
    }

    [Fact]
    public void SystemTimer_StartThenStart_DoesNotThrow()
    {
        using var timer = new SystemThreadingAnimationTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        timer.Start();
        var ex = Record.Exception(() => timer.Start());
        timer.Stop();
        Assert.Null(ex);
    }

    [Fact]
    public void SystemTimer_StopWithoutStart_DoesNotThrow()
    {
        using var timer = new SystemThreadingAnimationTimer();
        var ex = Record.Exception(() => timer.Stop());
        Assert.Null(ex);
    }

    [Fact]
    public void SystemTimer_Dispose_DoesNotThrow()
    {
        var timer = new SystemThreadingAnimationTimer();
        timer.Start();
        var ex = Record.Exception(() => timer.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void SystemTimer_IntervalChangedWhileRunning_UpdatesLivePeriod()
    {
        // Drives the `if (_running) _timer?.Change(...)` true-arm in the Interval setter.
        using var timer = new SystemThreadingAnimationTimer
        {
            Interval = TimeSpan.FromMilliseconds(30)
        };
        timer.Start();
        timer.Interval = TimeSpan.FromMilliseconds(15); // while running
        Assert.Equal(TimeSpan.FromMilliseconds(15), timer.Interval);
        timer.Stop();
    }

    [Fact]
    public void SystemTimer_StopBeforeStart_ThenStart_DoesNotThrow()
    {
        // Stop-then-Start path — ensures the `_running` guard resets correctly.
        using var timer = new SystemThreadingAnimationTimer
        {
            Interval = TimeSpan.FromMilliseconds(30)
        };
        timer.Stop();
        var ex = Record.Exception(() => timer.Start());
        Assert.Null(ex);
        timer.Stop();
    }

    [Fact]
    public async Task SystemTimer_FireWithoutTickSubscriber_DoesNotThrow()
    {
        // Drives the `Tick?.Invoke(...)` null-conditional false arm — the timer fires,
        // the lambda runs, and `Tick` is null (no subscriber), so the `?.` short-circuits.
        using var timer = new SystemThreadingAnimationTimer
        {
            Interval = TimeSpan.FromMilliseconds(5)
        };
        timer.Start();
        await Task.Delay(40, TestContext.Current.CancellationToken);
        timer.Stop();
        // No exception expected — the null-conditional path is covered.
    }
}
