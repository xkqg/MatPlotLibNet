// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Which side of the plot an outward-stacked secondary Y axis stands on.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
public enum AxisSide
{
    /// <summary>Stacked outward to the left of the primary Y axis.</summary>
    Left = 0,

    /// <summary>Stacked outward to the right of the primary Y axis (the side <see cref="Axes.SecondaryYAxis"/> uses).</summary>
    Right = 1,
}
