// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Defines a stateful animation that produces figures from typed state.</summary>
/// <typeparam name="TState">The animation state type passed between frames.</typeparam>
public interface IAnimation<TState>
{
    /// <summary>Gets the total number of frames.</summary>
    int FrameCount { get; }

    /// <summary>Gets or sets the delay between frames.</summary>
    TimeSpan Interval { get; set; }

    /// <summary>Gets or sets whether the animation loops continuously.</summary>
    bool Loop { get; set; }

    /// <summary>Creates the initial animation state.</summary>
    TState CreateInitialState();

    /// <summary>Advances the state for the next frame.</summary>
    TState Advance(TState currentState, int frameIndex);

    /// <summary>Generates a figure from the current state and frame index.</summary>
    Figure GenerateFrame(TState state, int frameIndex);
}
