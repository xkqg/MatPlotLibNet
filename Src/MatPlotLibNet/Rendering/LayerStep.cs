// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>What one step of an axes' data layer draws (<see cref="LayerStep"/>).</summary>
internal enum DataLayer
{
    /// <summary>The grid lines.</summary>
    Grid,

    /// <summary>Every span of the axes.</summary>
    Spans,

    /// <summary>One patch.</summary>
    Patch,

    /// <summary>Every reference line of the axes.</summary>
    ReferenceLines,

    /// <summary>Every trendline, price level and Fibonacci retracement of the axes.</summary>
    Tools,

    /// <summary>One primary series.</summary>
    Series,

    /// <summary>The tick marks of the primary axes, where a theme draws them among the data.</summary>
    TickMarks,
}

/// <summary>One thing an axes draws in its data layer, at the place the z-order gives it: after every step of a lower
/// <see cref="Z"/>, and after the steps of an equal one listed before it.</summary>
/// <param name="Z">Where the step stands in the z-order.</param>
/// <param name="Place">Where it was listed: steps are listed in the order they were drawn in before they had a z-order, so a tie
/// keeps that order.</param>
/// <param name="Layer">What it draws.</param>
/// <param name="Index">Which patch or series of the axes it draws; 0 for a layer that draws all of its kind.</param>
internal readonly record struct LayerStep(double Z, int Place, DataLayer Layer, int Index);
