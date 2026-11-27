// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests;

/// <summary>The animation a test hands a controller or a play: <paramref name="frameCount"/> frames whose state is the frame index, each
/// made by <paramref name="frame"/>, and a count of the frames it made.</summary>
/// <param name="frameCount">How many frames the animation has.</param>
/// <param name="frame">The figure of each frame index; left out, an empty figure titled <c>F</c> and the index.</param>
internal sealed class TestAnimation(int frameCount, Func<int, Figure>? frame = null) : IAnimation<int>
{
    public int FrameCount => frameCount;
    public TimeSpan Interval { get; set; }
    public bool Loop { get; set; }

    /// <summary>How many frames the animation made.</summary>
    public int FramesMade { get; private set; }

    public int CreateInitialState() => 0;
    public int Advance(int currentState, int frameIndex) => frameIndex;

    public Figure GenerateFrame(int state, int frameIndex)
    {
        FramesMade++;
        return frame?.Invoke(frameIndex) ?? new Figure { Title = $"F{frameIndex}" };
    }
}
