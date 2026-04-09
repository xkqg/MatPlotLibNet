// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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

    /// <summary>Verifies that Loop defaults to true.</summary>
    [Fact]
    public void Loop_DefaultsToTrue()
    {
        var anim = new AnimationBuilder(5, i => new Figure());
        Assert.True(anim.Loop);
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
}
