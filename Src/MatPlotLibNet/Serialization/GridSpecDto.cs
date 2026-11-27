// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for a <see cref="Models.GridSpec"/> layout descriptor.</summary>
internal sealed record GridSpecDto
{
    public int Rows { get; init; }
    public int Cols { get; init; }
    public double[]? HeightRatios { get; init; }
    public double[]? WidthRatios { get; init; }

    /// <summary>The JSON's form of <paramref name="spec"/>.</summary>
    internal static GridSpecDto Of(GridSpec spec) => new()
    {
        Rows = spec.Rows, Cols = spec.Cols,
        HeightRatios = spec.HeightRatios, WidthRatios = spec.WidthRatios
    };

    /// <summary>The grid specification this record describes.</summary>
    internal GridSpec ToGridSpec() => new()
    {
        Rows = Rows, Cols = Cols,
        HeightRatios = HeightRatios, WidthRatios = WidthRatios
    };
}
