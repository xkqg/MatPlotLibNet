// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Where the label of a <see cref="QuiverKeySeries"/> stands against the arrow it names. They are matplotlib's
/// <c>labelpos</c> values <c>N</c>, <c>S</c>, <c>E</c> and <c>W</c>.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
public enum QuiverKeyLabelPosition
{
    /// <summary>The label stands above the arrow, centred on it (matplotlib's <c>N</c>). The key's point is the middle of the
    /// arrow.</summary>
    Above = 0,

    /// <summary>The label stands below the arrow, centred on it (matplotlib's <c>S</c>). The key's point is the middle of the
    /// arrow.</summary>
    Below = 1,

    /// <summary>The label stands to the right of the arrow, level with it (matplotlib's <c>E</c>). The key's point is the
    /// arrow's head.</summary>
    Right = 2,

    /// <summary>The label stands to the left of the arrow, level with it (matplotlib's <c>W</c>). The key's point is the
    /// arrow's tail.</summary>
    Left = 3,
}
