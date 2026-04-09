// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Animation;

/// <summary>State machine states for animation playback.</summary>
public enum AnimationPlaybackState
{
    /// <summary>Animation is stopped.</summary>
    Stopped,

    /// <summary>Animation is actively playing frames.</summary>
    Playing,

    /// <summary>Animation is paused and can be resumed.</summary>
    Paused
}
