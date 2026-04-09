// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Defines a subplot grid with optional unequal row/column ratios.</summary>
public sealed record GridSpec
{
    /// <summary>Gets the number of rows in the grid.</summary>
    public int Rows { get; init; }

    /// <summary>Gets the number of columns in the grid.</summary>
    public int Cols { get; init; }

    /// <summary>Gets the relative height ratios for each row, or null for equal heights. Length must equal <see cref="Rows"/>.</summary>
    public double[]? HeightRatios { get; init; }

    /// <summary>Gets the relative width ratios for each column, or null for equal widths. Length must equal <see cref="Cols"/>.</summary>
    public double[]? WidthRatios { get; init; }
}

