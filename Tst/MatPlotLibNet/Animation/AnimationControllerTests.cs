// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>Verifies <see cref="AnimationController{TState}"/> playback behavior.</summary>
public class AnimationControllerTests
{
    /// <summary>Verifies that IAnimation interface can be implemented.</summary>
    [Fact]
    public void IAnimation_CanBeImplemented()
    {
        IAnimation<int> anim = new SimpleAnimation();
        Assert.Equal(3, anim.FrameCount);
        Assert.Equal(0, anim.CreateInitialState());
        Assert.Equal(1, anim.Advance(0, 1));
        Assert.NotNull(anim.GenerateFrame(0, 0));
    }

    /// <summary>Verifies that LegacyAnimationAdapter adapts AnimationBuilder to IAnimation.</summary>
    [Fact]
    public void LegacyAdapter_Adapts()
    {
        var builder = new AnimationBuilder(3, i => new Figure { Title = $"F{i}" });
        var adapter = new LegacyAnimationAdapter(builder);

        Assert.Equal(3, adapter.FrameCount);
        Assert.Equal("F0", adapter.GenerateFrame(0, 0).Title);
        Assert.Equal("F2", adapter.GenerateFrame(2, 2).Title);
    }

    /// <summary>Verifies that AnimationController can be constructed.</summary>
    [Fact]
    public void Controller_CanBeConstructed()
    {
        var anim = new SimpleAnimation();
        var controller = new AnimationController<int>(anim, (fig, ct) => Task.CompletedTask);

        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
        Assert.Equal(0, controller.CurrentFrame);
    }

    /// <summary>Verifies that AnimationPlaybackState enum has expected values.</summary>
    [Fact]
    public void PlaybackState_HasExpectedValues()
    {
        Assert.Equal(0, (int)AnimationPlaybackState.Stopped);
        Assert.Equal(1, (int)AnimationPlaybackState.Playing);
        Assert.Equal(2, (int)AnimationPlaybackState.Paused);
    }

    /// <summary>
    /// Covers the null-CTS branch of <see cref="AnimationController{T}.Stop"/>
    /// when called before any <c>PlayAsync</c> invocation. The two <c>?.</c>
    /// short-circuits must take their null path.
    /// </summary>
    [Fact]
    public void Stop_BeforePlay_DoesNotThrow()
    {
        var controller = new AnimationController<int>(new SimpleAnimation(), (_, _) => Task.CompletedTask);
        var ex = Record.Exception(() => controller.Stop());
        Assert.Null(ex);
        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
    }

    /// <summary>
    /// Covers the post-play <see cref="AnimationController{T}.Stop"/> path
    /// where _cts is non-null and gets cancelled + disposed. Also covers
    /// the OperationCanceledException catch in PlayAsync via external cancellation.
    /// </summary>
    [Fact]
    public async Task PlayAsync_CancelledExternally_StopsCleanly()
    {
        var anim = new SimpleAnimation { Interval = TimeSpan.FromMilliseconds(50) };
        var controller = new AnimationController<int>(anim, (_, _) => Task.CompletedTask);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(20);

        await controller.PlayAsync(cts.Token);

        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
    }

    /// <summary>
    /// Verifies <see cref="AnimationController{T}.DisposeAsync"/> calls Stop
    /// and returns a completed ValueTask.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_StopsAndCompletes()
    {
        var controller = new AnimationController<int>(new SimpleAnimation(), (_, _) => Task.CompletedTask);
        await controller.DisposeAsync();
        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
    }

    /// <summary>
    /// Covers the non-null _cts branches of <see cref="AnimationController{T}.Stop"/>:
    /// after PlayAsync starts, _cts is non-null, so calling Stop() exercises the
    /// _cts?.Cancel() and _cts?.Dispose() non-null short-circuits.
    /// </summary>
    [Fact]
    public async Task Stop_AfterPlayStarted_CancelsActiveCts()
    {
        var anim = new SimpleAnimation { Interval = TimeSpan.FromMilliseconds(100) };
        var controller = new AnimationController<int>(anim, (_, _) => Task.CompletedTask);
        // Fire-and-forget Play, then immediately Stop on the same thread
        var playTask = controller.PlayAsync(TestContext.Current.CancellationToken);
        // Give the controller a tick to set _cts
        await Task.Delay(20, TestContext.Current.CancellationToken);
        controller.Stop();
        await playTask; // cancellation propagates and PlayAsync returns
        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
    }

    private sealed class SimpleAnimation : IAnimation<int>
    {
        public int FrameCount => 3;
        public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(10);
        public bool Loop { get; set; }
        public int CreateInitialState() => 0;
        public int Advance(int currentState, int frameIndex) => frameIndex;
        public Figure GenerateFrame(int state, int frameIndex) => new() { Title = $"F{frameIndex}" };
    }
}
