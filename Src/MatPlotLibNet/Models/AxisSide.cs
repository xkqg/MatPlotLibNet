// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Which side of the plot a stacked axis stands on (<see cref="YAxisContainer"/>): a Y axis on the left or the right, a
/// linked X axis below.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
public enum AxisSide
{
    /// <summary>The left side: its slot 0 is the primary Y axis, every further left axis stacks outward past it.</summary>
    Left = 0,

    /// <summary>The right side: it starts empty, its first axis is slot 0 and every further right axis stacks outward
    /// past it.</summary>
    Right = 1,

    /// <summary>Below the plot: a linked X axis (matplotlib's <c>secondary_xaxis('bottom')</c> or at a location below it). The side
    /// starts empty; its first axis stands below everything the primary X axis draws, and every further one below the one before.</summary>
    Bottom = 2,
}
