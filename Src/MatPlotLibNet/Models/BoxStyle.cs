// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies the visual style of the background box drawn around an annotation's text.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum BoxStyle
{
    /// <summary>No background box is drawn. The annotation text is rendered without a surrounding box.</summary>
    None = 0,

    /// <summary>A plain rectangle with sharp corners.</summary>
    Square = 1,

    /// <summary>A rectangle with rounded Bezier-curved corners.</summary>
    Round = 2,

    /// <summary>A rounded rectangle with a zigzag (saw-tooth) bottom edge.</summary>
    RoundTooth = 3,

    /// <summary>A rectangle with sawtooth edges on all four sides.</summary>
    Sawtooth = 4,
}
