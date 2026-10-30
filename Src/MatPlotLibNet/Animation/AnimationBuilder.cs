// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Builds an animation as a sequence of figure frames for playback via SignalR.</summary>
public sealed class AnimationBuilder
{
    private readonly Func<int, Figure> _frameGenerator;

    /// <summary>The number of frames in one pass of the animation; frame indexes run from 0 to one less than this.</summary>
    public int FrameCount { get; }

    /// <summary>The pause between one frame and the next during playback. Default 50 ms.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>Whether playback starts over after the last frame instead of stopping. Default <see langword="true"/>.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Creates an animation builder with the specified frame count and generator function.</summary>
    /// <param name="frameCount">Total number of frames.</param>
    /// <param name="frameGenerator">Function that produces a Figure for each frame index (0-based).</param>
    public AnimationBuilder(int frameCount, Func<int, Figure> frameGenerator)
    {
        FrameCount = frameCount;
        _frameGenerator = frameGenerator;
    }

    /// <summary>Generates all frames as an enumerable sequence of figures.</summary>
    public IEnumerable<Figure> GenerateFrames()
    {
        for (int i = 0; i < FrameCount; i++)
            yield return _frameGenerator(i);
    }

    /// <summary>Generates a single frame at the specified index.</summary>
    public Figure GenerateFrame(int index) => _frameGenerator(index);

    /// <summary>Wraps this builder in an <see cref="AnimationController{TState}"/> ready to play.</summary>
    public AnimationController<int> ToController() =>
        new(new LegacyAnimationAdapter(this), static (_, _) => Task.CompletedTask);
}
