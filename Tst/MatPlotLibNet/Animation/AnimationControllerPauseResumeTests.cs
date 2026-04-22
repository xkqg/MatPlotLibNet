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
        var anim = new CountingAnimation(frameCount) { IntervalMs = intervalMs, Loop = loop };
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

    [Fact]
    public void Pause_WhenStopped_NoThrow()
    {
        var ctrl = MakeController();
        var ex = Record.Exception(() => ctrl.Pause());
        Assert.Null(ex);
    }

    [Fact]
    public void Resume_WhenStopped_NoThrow()
    {
        var ctrl = MakeController();
        var ex = Record.Exception(() => ctrl.Resume());
        Assert.Null(ex);
    }

    [Fact]
    public async Task PauseResume_FramesDeliveredBeforeAndAfter()
    {
        int total = 0;
        var ctrl = MakeController(frameCount: 6, intervalMs: 20);
        ctrl.FrameReady += (_, _) => Interlocked.Increment(ref total);

        var playTask = ctrl.PlayAsync(TestContext.Current.CancellationToken);
        await Task.Delay(35, TestContext.Current.CancellationToken); // ~1 frame at 20ms
        ctrl.Pause();
        int atPause = total;
        await Task.Delay(80, TestContext.Current.CancellationToken); // paused, no new frames
        Assert.Equal(atPause, total);

        ctrl.Resume();
        await playTask;
        Assert.True(total >= 6);
    }

    // ── Helper animation ──────────────────────────────────────────────────────

    private sealed class CountingAnimation : IAnimation<int>
    {
        public CountingAnimation(int frameCount) => FrameCount = frameCount;
        public int FrameCount { get; }
        public TimeSpan Interval => TimeSpan.FromMilliseconds(IntervalMs);
        TimeSpan IAnimation<int>.Interval { get => Interval; set { } }
        public int IntervalMs { get; set; } = 5;
        public bool Loop { get; set; }
        public int CreateInitialState() => 0;
        public int Advance(int s, int i) => i;
        public Figure GenerateFrame(int s, int i) => new() { Title = $"F{i}" };
    }
}
