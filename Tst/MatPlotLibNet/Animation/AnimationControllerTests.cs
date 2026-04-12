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
