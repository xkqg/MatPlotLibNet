// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the fill hatch pattern drawn inside a filled region.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum HatchPattern
{
    /// <summary>No hatch — solid fill only.</summary>
    None = 0,

    /// <summary>Forward diagonal lines ( / ).</summary>
    ForwardDiagonal = 1,

    /// <summary>Back diagonal lines ( \ ).</summary>
    BackDiagonal = 2,

    /// <summary>Horizontal lines ( ─ ).</summary>
    Horizontal = 3,

    /// <summary>Vertical lines ( │ ).</summary>
    Vertical = 4,

    /// <summary>Horizontal + vertical cross ( + ).</summary>
    Cross = 5,

    /// <summary>Forward + back diagonal cross ( × ).</summary>
    DiagonalCross = 6,

    /// <summary>Dot grid ( · ).</summary>
    Dots = 7,

    /// <summary>Star grid ( * ) — dots combined with diagonal crosses.</summary>
    Stars = 8,
}
