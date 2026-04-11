// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Defines a subplot grid with optional unequal row/column ratios.</summary>
public sealed record GridSpec
{
    public int Rows { get; init; }

    public int Cols { get; init; }

    public double[]? HeightRatios { get; init; }

    public double[]? WidthRatios { get; init; }
}

