// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Defines a subplot grid with optional unequal row/column ratios.</summary>
public sealed record GridSpec
{
    /// <summary>How many rows the grid has. The rows share the height left after the margins and the gaps between them,
    /// in the proportions <see cref="HeightRatios"/> gives, or equally when it gives none.</summary>
    public int Rows { get; init; }

    /// <summary>How many columns the grid has. The columns share the width left after the margins and the gaps between
    /// them, in the proportions <see cref="WidthRatios"/> gives, or equally when it gives none.</summary>
    public int Cols { get; init; }

    /// <summary>The relative height of each row, top to bottom: one number per row, taken as proportions of the height the
    /// rows share (<c>{ 3, 1 }</c> makes the first row three times as tall as the second). <see langword="null"/> (the
    /// default) makes the rows equally tall; a list whose length is not <see cref="Rows"/> is ignored the same way.</summary>
    public double[]? HeightRatios { get; init; }

    /// <summary>The relative width of each column, left to right: one number per column, taken as proportions of the width
    /// the columns share (<c>{ 2, 1 }</c> makes the first column twice as wide as the second). <see langword="null"/> (the
    /// default) makes the columns equally wide; a list whose length is not <see cref="Cols"/> is ignored the same way.</summary>
    public double[]? WidthRatios { get; init; }
}

