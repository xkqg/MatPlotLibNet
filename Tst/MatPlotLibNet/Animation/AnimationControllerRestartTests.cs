// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>A play started while an earlier one is still publishing a frame: the earlier play ends, and what it leaves
/// behind must not touch the play that replaced it.</summary>
public class AnimationControllerRestartTests
{
    [Fact]
    public async Task ARestartWhileAFrameIsPublishing_LeavesTheNewPlayPlaying()
    {
        // The earlier play woke up after the restart, saw its token cancelled and set the state to Stopped over the new
        // play's Playing, after which Pause did nothing.
        var firstPublishEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstPublish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int publishes = 0;

        Task Publish(Figure frame, CancellationToken ct)
        {
            if (Interlocked.Increment(ref publishes) == 1)
            {
                firstPublishEntered.TrySetResult();
                return releaseFirstPublish.Task; // a publisher that does not listen to the token
            }

            return Task.Delay(Timeout.Infinite, ct);
        }

        var controller = new AnimationController<int>(new TestAnimation(1_000) { Interval = TimeSpan.FromMilliseconds(1), Loop = true }, Publish);
        var first = controller.PlayAsync(TestContext.Current.CancellationToken);
        await firstPublishEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var second = controller.PlayAsync(TestContext.Current.CancellationToken);
        releaseFirstPublish.TrySetResult();
        await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(AnimationPlaybackState.Playing, controller.State);
        controller.Pause();
        Assert.Equal(AnimationPlaybackState.Paused, controller.State);

        controller.Stop();
        await second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
    }

    [Fact]
    public async Task APlayThatRunsToItsEnd_StopsTheController()
    {
        var controller = new AnimationController<int>(new TestAnimation(2) { Interval = TimeSpan.FromMilliseconds(1) }, static (_, _) => Task.CompletedTask);

        await controller.PlayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
        controller.Stop(); // nothing left to stop
        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);
    }
}
