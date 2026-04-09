// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies the visual style of the arrow drawn from an annotation text to its target point.</summary>
public enum ArrowStyle
{
    /// <summary>No arrow is drawn even if a target coordinate is set.</summary>
    None,

    /// <summary>A simple straight line from the annotation text to the target point.</summary>
    Simple,

    /// <summary>A line with a solid triangular arrowhead at the target point.</summary>
    FancyArrow
}
