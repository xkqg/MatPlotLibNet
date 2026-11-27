// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for a <see cref="Models.GridPosition"/> span within a grid layout.</summary>
internal sealed record GridPositionDto
{
    public int RowStart { get; init; }
    public int RowEnd { get; init; }
    public int ColStart { get; init; }
    public int ColEnd { get; init; }

    /// <summary>The JSON's form of <paramref name="position"/>.</summary>
    internal static GridPositionDto Of(GridPosition position) => new()
    {
        RowStart = position.RowStart, RowEnd = position.RowEnd,
        ColStart = position.ColStart, ColEnd = position.ColEnd
    };

    /// <summary>The grid position this record describes.</summary>
    internal GridPosition ToGridPosition() => new(RowStart, RowEnd, ColStart, ColEnd);
}
