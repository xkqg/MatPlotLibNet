// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Defines a stateful animation that produces figures from typed state.</summary>
/// <typeparam name="TState">The animation state type passed between frames.</typeparam>
public interface IAnimation<TState>
{
    /// <summary>The number of frames in one pass of the animation; frame indexes run from 0 to one less than this.</summary>
    int FrameCount { get; }

    /// <summary>The pause between one frame and the next during playback.</summary>
    TimeSpan Interval { get; set; }

    /// <summary>Whether playback starts over, from a fresh initial state, after the last frame instead of stopping.</summary>
    bool Loop { get; set; }

    /// <summary>Creates the initial animation state.</summary>
    TState CreateInitialState();

    /// <summary>Advances the state for the next frame.</summary>
    TState Advance(TState currentState, int frameIndex);

    /// <summary>Generates a figure from the current state and frame index.</summary>
    Figure GenerateFrame(TState state, int frameIndex);
}
