// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Animation;

/// <summary>State machine states for animation playback.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum AnimationPlaybackState
{
    /// <summary>Animation is stopped.</summary>
    Stopped = 0,

    /// <summary>Animation is actively playing frames.</summary>
    Playing = 1,

    /// <summary>Animation is paused and can be resumed.</summary>
    Paused = 2,
}
