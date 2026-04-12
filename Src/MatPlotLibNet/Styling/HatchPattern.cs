// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the fill hatch pattern drawn inside a filled region.</summary>
public enum HatchPattern
{
    /// <summary>No hatch — solid fill only.</summary>
    None,

    /// <summary>Forward diagonal lines ( / ).</summary>
    ForwardDiagonal,

    /// <summary>Back diagonal lines ( \ ).</summary>
    BackDiagonal,

    /// <summary>Horizontal lines ( ─ ).</summary>
    Horizontal,

    /// <summary>Vertical lines ( │ ).</summary>
    Vertical,

    /// <summary>Horizontal + vertical cross ( + ).</summary>
    Cross,

    /// <summary>Forward + back diagonal cross ( × ).</summary>
    DiagonalCross,

    /// <summary>Dot grid ( · ).</summary>
    Dots,

    /// <summary>Star grid ( * ) — dots combined with diagonal crosses.</summary>
    Stars,
}
