// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the step interpolation style used when drawing a line series.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum DrawStyle
{
    /// <summary>Straight segments joining consecutive points, with no stepping.</summary>
    Default  = 0,
    /// <summary>Step line whose vertical jump falls at the earlier point: it rises to the next point's value first and then runs flat
    /// to that point's x (matplotlib's <c>steps-pre</c>).</summary>
    StepsPre = 1,
    /// <summary>Step line whose vertical jump falls halfway between two consecutive points (matplotlib's <c>steps-mid</c>).</summary>
    StepsMid = 2,
    /// <summary>Step line that holds each point's value to the right, up to the next point's x, so the vertical jump falls at the later point (matplotlib's <c>steps-post</c>).</summary>
    StepsPost= 3,
}
