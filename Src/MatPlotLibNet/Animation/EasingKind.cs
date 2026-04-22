// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Animation;

/// <summary>Identifies a built-in easing curve.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum EasingKind
{
    /// <summary>Constant velocity — no easing.</summary>
    Linear = 0,

    /// <summary>Slow start, fast finish (quadratic).</summary>
    EaseIn = 1,

    /// <summary>Fast start, slow finish (quadratic).</summary>
    EaseOut = 2,

    /// <summary>Slow start, fast middle, slow finish (cubic S-curve).</summary>
    EaseInOut = 3,

    /// <summary>Bouncing deceleration at the end.</summary>
    Bounce = 4,

    /// <summary>Elastic overshoot near the end.</summary>
    Elastic = 5,
}
