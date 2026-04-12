// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies the path style used to connect an annotation text to its arrow target.</summary>
public enum ConnectionStyle
{
    /// <summary>A straight line directly from the annotation text position to the target point.</summary>
    Straight,

    /// <summary>A cubic Bezier arc. The curvature is controlled by <see cref="Annotation.ConnectionRad"/>.</summary>
    Arc3,

    /// <summary>A right-angle path: horizontal segment first, then vertical to the target.</summary>
    Angle,

    /// <summary>A smoothed right-angle path using Bezier curves at the corner for a rounded elbow.</summary>
    Angle3
}
