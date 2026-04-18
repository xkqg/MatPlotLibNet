// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies the visual style of the arrow drawn from an annotation text to its target point.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum ArrowStyle
{
    /// <summary>No arrow is drawn even if a target coordinate is set.</summary>
    None = 0,

    /// <summary>A simple straight line from the annotation text to the target point, no arrowhead.</summary>
    Simple = 1,

    /// <summary>A line with a solid triangular arrowhead at the target point.</summary>
    FancyArrow = 2,

    /// <summary>A wider filled wedge arrowhead at the target point.</summary>
    Wedge = 3,

    /// <summary>An open curved arrowhead at the source (text) end only.</summary>
    CurveA = 4,

    /// <summary>An open curved arrowhead at the target end only.</summary>
    CurveB = 5,

    /// <summary>Open curved arrowheads at both the source and target ends.</summary>
    CurveAB = 6,

    /// <summary>A perpendicular bracket line at the source (text) end only.</summary>
    BracketA = 7,

    /// <summary>A perpendicular bracket line at the target end only.</summary>
    BracketB = 8,

    /// <summary>Perpendicular bracket lines at both the source and target ends.</summary>
    BracketAB = 9,
}
