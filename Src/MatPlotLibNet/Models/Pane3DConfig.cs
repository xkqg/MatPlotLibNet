// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Configures the three back-facing cube panes of a 3D axes (floor, left wall, right wall).
/// When <c>null</c> colors are used, the renderer falls back to the theme's <see cref="Theme.Pane3DColor"/>
/// or the default <c>#F5F5F5</c>.</summary>
public sealed record Pane3DConfig
{
    /// <summary>Color of the bottom floor pane (z = zMin). Default: theme color.</summary>
    public Color? FloorColor { get; init; }

    /// <summary>Color of the back-left wall pane (x = xMin). Default: theme color.</summary>
    public Color? LeftWallColor { get; init; }

    /// <summary>Color of the back-right wall pane (y = yMax). Default: theme color.</summary>
    public Color? RightWallColor { get; init; }

    /// <summary>Opacity of all pane surfaces. Range [0, 1], default 0.8.</summary>
    public double Alpha { get; init; } = 0.8;

    /// <summary>Whether panes are visible. Set <c>false</c> for a transparent 3D background.</summary>
    public bool Visible { get; init; } = true;
}
