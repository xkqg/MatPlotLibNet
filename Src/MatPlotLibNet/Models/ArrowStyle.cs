// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies the visual style of the arrow drawn from an annotation text to its target point.</summary>
public enum ArrowStyle
{
    /// <summary>No arrow is drawn even if a target coordinate is set.</summary>
    None,

    /// <summary>A simple straight line from the annotation text to the target point, no arrowhead.</summary>
    Simple,

    /// <summary>A line with a solid triangular arrowhead at the target point.</summary>
    FancyArrow,

    /// <summary>A wider filled wedge arrowhead at the target point.</summary>
    Wedge,

    /// <summary>An open curved arrowhead at the source (text) end only.</summary>
    CurveA,

    /// <summary>An open curved arrowhead at the target end only.</summary>
    CurveB,

    /// <summary>Open curved arrowheads at both the source and target ends.</summary>
    CurveAB,

    /// <summary>A perpendicular bracket line at the source (text) end only.</summary>
    BracketA,

    /// <summary>A perpendicular bracket line at the target end only.</summary>
    BracketB,

    /// <summary>Perpendicular bracket lines at both the source and target ends.</summary>
    BracketAB
}
