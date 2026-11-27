// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>What every animation timer does, held once by <see cref="AnimationTimer"/>: it ticks every 16 ms until it is told another
/// interval, a second start is the same timer, a stop of a timer that never ran is harmless, a tick is said to whoever listens with the
/// timer itself as the sender, and the ticks come from the thread pool unless the platform's timer gives them.</summary>
public class AnimationTimerTests
{
    /// <summary>A timer whose platform tick is a count of what the base asked of it.</summary>
    private sealed class RecordingTimer : AnimationTimer
    {
        public int Begun;
        public int Ended;
        public TimeSpan? IntervalAtBegin;
        public readonly List<TimeSpan> Retimed = [];

        private protected override void Begin()
        {
            Begun++;
            IntervalAtBegin = Interval;
        }

        private protected override void End() => Ended++;

        private protected override void Retime(TimeSpan interval) => Retimed.Add(interval);

        public void PlatformTicks() => RaiseTick(this, EventArgs.Empty);
    }

    [Fact]
    public void ATimerTicksEverySixteenMilliseconds_UntilToldOtherwise()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(16), new RecordingTimer().Interval);
        Assert.Equal(TimeSpan.FromMilliseconds(16), new SystemThreadingAnimationTimer().Interval);
    }

    [Fact]
    public void AnIntervalSet_IsKept_AndThePlatformIsToldAtOnce()
    {
        var timer = new RecordingTimer { Interval = TimeSpan.FromMilliseconds(33) };

        Assert.Equal(TimeSpan.FromMilliseconds(33), timer.Interval);
        Assert.Equal([TimeSpan.FromMilliseconds(33)], timer.Retimed);
    }

    [Fact]
    public void ASecondStart_IsTheSameTimer_AndAStartAfterAStopBeginsAgain()
    {
        var timer = new RecordingTimer();

        timer.Start();
        timer.Start();
        Assert.Equal(1, timer.Begun);

        timer.Stop();
        timer.Start();
        Assert.Equal(2, timer.Begun);
        Assert.Equal(1, timer.Ended);
    }

    [Fact]
    public void AStopBeforeAnyStart_IsHarmless_AndADisposeIsAStop()
    {
        var timer = new RecordingTimer();

        Assert.Null(Record.Exception(timer.Stop));
        timer.Dispose();

        Assert.Equal(0, timer.Begun);
        Assert.Equal(2, timer.Ended);
    }

    [Fact]
    public void ThePlatformIsBegun_AtTheIntervalSetBeforeTheStart()
    {
        var timer = new RecordingTimer { Interval = TimeSpan.FromMilliseconds(50) };

        timer.Start();

        Assert.Equal(TimeSpan.FromMilliseconds(50), timer.IntervalAtBegin);
    }

    [Fact]
    public void ATick_IsSaidToWhoeverListens_WithTheTimerAsTheSender_AndToNobodyWhenNobodyDoes()
    {
        var timer = new RecordingTimer();
        timer.PlatformTicks();   // nobody listens: nothing happens
        int ticks = 0;
        EventHandler listener = (sender, _) =>
        {
            Assert.Same(timer, sender);
            ticks++;
        };
        timer.Tick += listener;

        timer.PlatformTicks();
        timer.Tick -= listener;
        timer.PlatformTicks();

        Assert.Equal(1, ticks);
    }

    [Fact]
    public async Task OnThePool_AStartedTimerTicks_UntilItIsStopped_AndAnIntervalChangedWhileRunningIsTheOneThatFollows()
    {
        using var timer = new SystemThreadingAnimationTimer { Interval = TimeSpan.FromMinutes(5) };
        var ticked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        timer.Tick += (sender, _) =>
        {
            Assert.Same(timer, sender);
            ticked.TrySetResult();
        };
        timer.Start();

        timer.Interval = TimeSpan.FromMilliseconds(5);
        await ticked.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timer.Stop();

        Assert.Equal(TimeSpan.FromMilliseconds(5), timer.Interval);
    }

    [Fact]
    public void OnThePool_ATimerThatWasNeverStarted_CanBeRetimedAndStopped()
    {
        var timer = new SystemThreadingAnimationTimer { Interval = TimeSpan.FromMilliseconds(20) };

        timer.Stop();

        Assert.Equal(TimeSpan.FromMilliseconds(20), timer.Interval);
    }
}
