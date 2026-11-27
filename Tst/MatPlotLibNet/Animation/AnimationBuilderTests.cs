// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>Verifies <see cref="AnimationBuilder"/> behavior.</summary>
public class AnimationBuilderTests
{
    /// <summary>Verifies that FrameCount is stored from constructor.</summary>
    [Fact]
    public void FrameCount_StoredFromConstructor()
    {
        var anim = new AnimationBuilder(10, i => Plt.Create().Plot([1.0], [(double)i]).Build());
        Assert.Equal(10, anim.FrameCount);
    }

    /// <summary>Verifies that Interval defaults to 50ms.</summary>
    [Fact]
    public void Interval_DefaultsTo50ms()
    {
        var anim = new AnimationBuilder(5, i => new Figure());
        Assert.Equal(TimeSpan.FromMilliseconds(50), anim.Interval);
    }

    /// <summary>Verifies that GenerateFrames produces the correct number of frames.</summary>
    [Fact]
    public void GenerateFrames_ProducesCorrectCount()
    {
        var anim = new AnimationBuilder(7, i => new Figure { Title = $"Frame {i}" });
        var frames = anim.GenerateFrames().ToList();
        Assert.Equal(7, frames.Count);
    }

    /// <summary>Verifies that each frame receives the correct index.</summary>
    [Fact]
    public void GenerateFrames_PassesCorrectIndex()
    {
        var anim = new AnimationBuilder(3, i => new Figure { Title = $"F{i}" });
        var frames = anim.GenerateFrames().ToList();
        Assert.Equal("F0", frames[0].Title);
        Assert.Equal("F1", frames[1].Title);
        Assert.Equal("F2", frames[2].Title);
    }

    /// <summary>Verifies that Interval can be customized.</summary>
    [Fact]
    public void Interval_CanBeSet()
    {
        var anim = new AnimationBuilder(1, i => new Figure()) { Interval = TimeSpan.FromMilliseconds(100) };
        Assert.Equal(TimeSpan.FromMilliseconds(100), anim.Interval);
    }

    /// <summary>Verifies that Loop can be set to false.</summary>
    [Fact]
    public void Loop_CanBeSetToFalse()
    {
        var anim = new AnimationBuilder(1, i => new Figure()) { Loop = false };
        Assert.False(anim.Loop);
    }

    /// <summary>A builder is an animation of frame indexes itself: a controller, an interactive figure and a host play it as it is,
    /// with no adapter between them.</summary>
    [Fact]
    public void ABuilder_IsAnAnimationOfFrameIndexes() =>
        Assert.True(typeof(IAnimation<int>).IsAssignableFrom(typeof(AnimationBuilder)));

    /// <summary>As an animation, a builder's state is the frame index: it starts at 0, a step is the index asked for whatever the
    /// state was, and a frame is the figure its generator makes for the index.</summary>
    [Fact]
    public void AsAnAnimation_TheStateIsTheFrameIndex()
    {
        var figures = new[] { new Figure { Title = "F0" }, new Figure { Title = "F1" }, new Figure { Title = "F2" } };
        var animation = (IAnimation<int>)(object)new AnimationBuilder(3, i => figures[i]);

        Assert.Equal(0, animation.CreateInitialState());
        Assert.Equal(7, animation.Advance(999, 7));
        Assert.Same(figures[1], animation.GenerateFrame(0, 1));
        Assert.Same(figures[2], animation.GenerateFrame(999, 2));
    }

    /// <summary>The pace and the loop an animation is played by are the builder's own: one value, read and written through either
    /// face.</summary>
    [Fact]
    public void AsAnAnimation_ThePaceAndTheLoopAreTheBuilders()
    {
        var builder = new AnimationBuilder(4, _ => new Figure());
        var animation = (IAnimation<int>)(object)builder;

        animation.Interval = TimeSpan.FromSeconds(1);
        animation.Loop = false;

        Assert.Equal(4, animation.FrameCount);
        Assert.Equal(TimeSpan.FromSeconds(1), builder.Interval);
        Assert.False(builder.Loop);
    }

    /// <summary>A controller plays a builder itself, every frame of it.</summary>
    [Fact]
    public async Task AController_PlaysEveryFrameOfTheBuilder()
    {
        int frames = 0;
        var builder = new AnimationBuilder(3, _ => new Figure()) { Interval = TimeSpan.FromMilliseconds(1), Loop = false };
        var controller = new AnimationController<int>((IAnimation<int>)(object)builder, (_, _) => { frames++; return Task.CompletedTask; });

        await controller.PlayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, frames);
    }
}
