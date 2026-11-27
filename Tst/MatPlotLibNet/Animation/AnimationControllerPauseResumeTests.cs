// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>Verifies the new Pause/Resume state machine and <see cref="AnimationController{TState}.FrameReady"/>
/// event added to <see cref="AnimationController{TState}"/>.</summary>
public class AnimationControllerPauseResumeTests
{
    private static AnimationController<int> MakeController(
        int frameCount = 10,
        bool loop = false,
        int intervalMs = 5,
        Action<Figure>? onFrame = null)
    {
        var anim = new TestAnimation(frameCount) { Interval = TimeSpan.FromMilliseconds(intervalMs), Loop = loop };
        Action<Figure, CancellationToken> noop = (_, _) => { };
        var ctrl = new AnimationController<int>(anim,
            async (fig, ct) =>
            {
                onFrame?.Invoke(fig);
                await Task.CompletedTask;
            });
        return ctrl;
    }

    // ── FrameReady event ──────────────────────────────────────────────────────

    [Fact]
    public async Task FrameReady_FiredOnEachPublishedFrame()
    {
        var frames = new List<Figure>();
        var ctrl = MakeController(frameCount: 3, intervalMs: 1);
        ctrl.FrameReady += (_, fig) => frames.Add(fig);

        await ctrl.PlayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, frames.Count);
    }

    [Fact]
    public async Task FrameReady_FigureTitleMatchesFrameIndex()
    {
        var frames = new List<Figure>();
        var ctrl = MakeController(frameCount: 3, intervalMs: 1);
        ctrl.FrameReady += (_, fig) => frames.Add(fig);

        await ctrl.PlayAsync(TestContext.Current.CancellationToken);

        Assert.Equal("F0", frames[0].Title);
        Assert.Equal("F1", frames[1].Title);
        Assert.Equal("F2", frames[2].Title);
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    [Fact]
    public async Task Pause_TransitionsStateToPaused()
    {
        var ctrl = MakeController(frameCount: 100, intervalMs: 50);
        var playTask = ctrl.PlayAsync(TestContext.Current.CancellationToken);
        await Task.Delay(20, TestContext.Current.CancellationToken);

        ctrl.Pause();

        Assert.Equal(AnimationPlaybackState.Paused, ctrl.State);
        ctrl.Stop();
        await playTask;
    }

    [Fact]
    public async Task Resume_TransitionsStateToPlaying()
    {
        var ctrl = MakeController(frameCount: 100, intervalMs: 50);
        var playTask = ctrl.PlayAsync(TestContext.Current.CancellationToken);
        await Task.Delay(20, TestContext.Current.CancellationToken);

        ctrl.Pause();
        Assert.Equal(AnimationPlaybackState.Paused, ctrl.State);

        ctrl.Resume();
        await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.Equal(AnimationPlaybackState.Playing, ctrl.State);

        ctrl.Stop();
        await playTask;
    }

    [Fact]
    public async Task Pause_WhilePaused_NoThrow()
    {
        var ctrl = MakeController(frameCount: 100, intervalMs: 50);
        var playTask = ctrl.PlayAsync(TestContext.Current.CancellationToken);
        await Task.Delay(20, TestContext.Current.CancellationToken);
        ctrl.Pause();

        var ex = Record.Exception(() => ctrl.Pause());
        Assert.Null(ex);

        ctrl.Stop();
        await playTask;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PauseAndResume_WhenStopped_NoThrow(bool pause)
    {
        var ctrl = MakeController();
        var ex = Record.Exception(() => { if (pause) ctrl.Pause(); else ctrl.Resume(); });
        Assert.Null(ex);
    }

    /// <summary>The pause is taken from inside the handler of the first frame, so no later frame can be anywhere between its
    /// generation and its delivery: the frames that follow wait for the resume whatever the box is doing, and the test
    /// reads nothing from a clock to say which frame the pause landed on.</summary>
    [Fact]
    public async Task PauseResume_FramesDeliveredBeforeAndAfter()
    {
        var delivered = new List<string>();
        var paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ctrl = MakeController(frameCount: 6, intervalMs: 1);
        ctrl.FrameReady += (_, fig) =>
        {
            lock (delivered) delivered.Add(fig.Title!);
            if (paused.Task.IsCompleted) return;
            ctrl.Pause();
            paused.SetResult();
        };

        var playTask = ctrl.PlayAsync(TestContext.Current.CancellationToken);
        await paused.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Task.Delay(40, TestContext.Current.CancellationToken); // only a regression is caught by this wait; a slow box cannot fail it
        lock (delivered) Assert.Equal(["F0"], delivered);

        ctrl.Resume();
        await playTask;
        lock (delivered) Assert.Equal(["F0", "F1", "F2", "F3", "F4", "F5"], delivered);
    }

    /// <summary>A pause that lands while a frame is being published holds that frame back: once <c>Pause</c> has returned no
    /// <c>FrameReady</c> is raised, and the frame is delivered when the playback resumes. The publish is held on a gate the
    /// test opens after the pause, and the gate is a source whose continuation runs on the opening thread, so a handler that
    /// ran on opening it would have run before the next line of the test.</summary>
    [Fact]
    public async Task Pause_WhileAFrameIsBeingPublished_RaisesNoFrameReadyUntilTheResume()
    {
        var gate = new TaskCompletionSource();
        var publishing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var anim = new TestAnimation(2) { Interval = TimeSpan.FromMilliseconds(1) };
        var ctrl = new AnimationController<int>(anim, async (_, _) =>
        {
            publishing.TrySetResult();
            await gate.Task;
        });
        var delivered = new List<string>();
        ctrl.FrameReady += (_, fig) => { lock (delivered) delivered.Add(fig.Title!); };

        var playTask = ctrl.PlayAsync(TestContext.Current.CancellationToken);
        await publishing.Task.WaitAsync(TestContext.Current.CancellationToken);
        ctrl.Pause();
        gate.SetResult();

        lock (delivered) Assert.Empty(delivered);
        Assert.Equal(AnimationPlaybackState.Paused, ctrl.State);

        ctrl.Resume();
        await playTask;
        lock (delivered) Assert.Equal(["F0", "F1"], delivered);
    }

    /// <summary>A stop that lands while a frame is being published ends the run without raising the frame it held.</summary>
    [Fact]
    public async Task Stop_WhileAFrameIsBeingPublishedAndPaused_RaisesNoFrameReady()
    {
        var gate = new TaskCompletionSource();
        var publishing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ctrl = new AnimationController<int>(new TestAnimation(2) { Interval = TimeSpan.FromMilliseconds(1) }, async (_, _) =>
        {
            publishing.TrySetResult();
            await gate.Task;
        });
        int delivered = 0;
        ctrl.FrameReady += (_, _) => Interlocked.Increment(ref delivered);

        var playTask = ctrl.PlayAsync(TestContext.Current.CancellationToken);
        await publishing.Task.WaitAsync(TestContext.Current.CancellationToken);
        ctrl.Pause();
        gate.SetResult();
        ctrl.Stop();
        await playTask;

        Assert.Equal(0, delivered);
    }

    // ── Helper animation ──────────────────────────────────────────────────────
}
