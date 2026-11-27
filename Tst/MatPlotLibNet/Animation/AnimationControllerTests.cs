// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>Verifies <see cref="AnimationController{TState}"/> playback behavior.</summary>
public class AnimationControllerTests
{
    /// <summary>Verifies that AnimationController can be constructed.</summary>
    [Fact]
    public void Controller_CanBeConstructed()
    {
        var anim = new TestAnimation(3) { Interval = TimeSpan.FromMilliseconds(10) };
        var controller = new AnimationController<int>(anim, (fig, ct) => Task.CompletedTask);

        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
        Assert.Equal(0, controller.CurrentFrame);
    }

    /// <summary>
    /// Covers the null-CTS branch of <see cref="AnimationController{T}.Stop"/>
    /// when called before any <c>PlayAsync</c> invocation. The two <c>?.</c>
    /// short-circuits must take their null path.
    /// </summary>
    [Fact]
    public void Stop_BeforePlay_DoesNotThrow()
    {
        var controller = new AnimationController<int>(new TestAnimation(3) { Interval = TimeSpan.FromMilliseconds(10) }, (_, _) => Task.CompletedTask);
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
        var anim = new TestAnimation(3) { Interval = TimeSpan.FromMilliseconds(50) };
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
        var controller = new AnimationController<int>(new TestAnimation(3) { Interval = TimeSpan.FromMilliseconds(10) }, (_, _) => Task.CompletedTask);
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
        var anim = new TestAnimation(3) { Interval = TimeSpan.FromMilliseconds(100) };
        var controller = new AnimationController<int>(anim, (_, _) => Task.CompletedTask);
        // Fire-and-forget Play, then immediately Stop on the same thread
        var playTask = controller.PlayAsync(TestContext.Current.CancellationToken);
        // Give the controller a tick to set _cts
        await Task.Delay(20, TestContext.Current.CancellationToken);
        controller.Stop();
        await playTask; // cancellation propagates and PlayAsync returns
        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
    }

    [Fact]
    public async Task Controller_StopBeforePlay_IsNoOp()
    {
        var fig = new Figure();
        var ctrl = new AnimationController<int>(
            new AnimationBuilder(3, _ => fig),
            (_, _) => Task.CompletedTask);

        // Stop before any Play call must not throw.
        ctrl.Stop();
        Assert.Equal(AnimationPlaybackState.Stopped, ctrl.State);
    }

    [Fact]
    public async Task Controller_PlayThenComplete_NormalPath()
    {
        var fig = new Figure();
        int frames = 0;
        var ctrl = new AnimationController<int>(
            new AnimationBuilder(3, _ => fig)
            { Interval = TimeSpan.FromMilliseconds(1), Loop = false },
            (_, _) => { frames++; return Task.CompletedTask; });

        // Use TestContext.Current.CancellationToken (xUnit v3) so the test cancels
        // promptly if the runner aborts -- avoids xUnit1051 warning.
        await ctrl.PlayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, frames);
        Assert.Equal(AnimationPlaybackState.Stopped, ctrl.State);
    }

    [Fact]
    public async Task Controller_ExternalCancellation_StopsPlayback()
    {
        var fig = new Figure();
        var ctrl = new AnimationController<int>(
            new AnimationBuilder(1000, _ => fig)
            { Interval = TimeSpan.FromMilliseconds(100), Loop = true },
            (_, _) => Task.CompletedTask);

        using var externalCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await ctrl.PlayAsync(externalCts.Token);

        Assert.Equal(AnimationPlaybackState.Stopped, ctrl.State);
    }

    [Fact]
    public async Task Controller_DisposeAsync_StopsAndIsIdempotent()
    {
        var fig = new Figure();
        var ctrl = new AnimationController<int>(
            new AnimationBuilder(3, _ => fig),
            (_, _) => Task.CompletedTask);

        await ctrl.DisposeAsync();
        await ctrl.DisposeAsync();   // second dispose must not throw
    }
}
