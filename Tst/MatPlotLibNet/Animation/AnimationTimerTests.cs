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

    /// <summary>The contract is that a started timer fires REPEATEDLY — not that it manages a given number of
    /// ticks inside a given number of milliseconds.
    /// <para>Asserting the second is how this test failed on CI: 20 ms interval, a fixed 100 ms sleep, and a
    /// shared runner that delivered one tick in that window. Widening the sleep would only move the boundary;
    /// waiting for the CONDITION removes it — a fast box finishes in ~40 ms, a loaded one takes as long as it
    /// takes, and the assertion is the same either way.</para></summary>
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

        var budget = TimeSpan.FromSeconds(5);
        var deadline = DateTime.UtcNow + budget;
        while (Volatile.Read(ref ticks) < 2 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        timer.Stop();
        Assert.True(Volatile.Read(ref ticks) >= 2,
            $"a started timer must fire repeatedly; got {ticks} tick(s) in {budget.TotalSeconds:0} s at a 20 ms interval");
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
        // Drives the `Tick?.Invoke(...)` null-conditional false arm — the timer fires, the lambda runs, and
        // `Tick` is null (no subscriber), so the `?.` short-circuits.
        //
        // A fixed 40 ms sleep used to decide whether that arm was ever reached, which made this class's BRANCH
        // coverage a function of machine load: under the coverage collector the thread-pool callback sometimes
        // did not run inside the window and the gate read 87.5 % (measured 2026-08-31). So: first PROVE this
        // process is scheduling timer callbacks at all — with a subscribed probe that signals — and only then
        // hold an unsubscribed timer open for a window that is two orders of magnitude wider than its interval.
        using var fired = new ManualResetEventSlim(false);
        using (var probe = new SystemThreadingAnimationTimer { Interval = TimeSpan.FromMilliseconds(5) })
        {
            probe.Tick += (_, _) => fired.Set();
            probe.Start();
            Assert.True(fired.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
                "the process is not scheduling timer callbacks at all — nothing about the timer can be asserted");
            probe.Stop();
        }

        using var timer = new SystemThreadingAnimationTimer
        {
            Interval = TimeSpan.FromMilliseconds(5)
        };
        timer.Start();
        await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
        timer.Stop();
        // No exception expected — the null-conditional path is covered.
    }
}
