// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Configures the three back-facing cube panes of a 3D axes — the floor (Z pane) and the
/// two walls (X and Y panes). Which SIDE of each axis the camera puts at the back follows the view
/// (see <see cref="CubeFaceSelection"/>); each colour is bound to its AXIS, so a wall keeps its
/// colour when the camera swings it to the opposite side.
/// When <c>null</c> colors are used, the renderer falls back to the theme's <see cref="Theme.Pane3DColor"/>
/// or the default <c>#F5F5F5</c>.</summary>
public sealed record Pane3DConfig
{
    /// <summary>Color of the horizontal floor pane — the Z-axis back plane the camera selects
    /// (<c>z = zMin</c> seen from above, <c>z = zMax</c> seen from below). Default: theme color.</summary>
    public Color? FloorColor { get; init; }

    /// <summary>Color of the X-axis wall pane — whichever of <c>x = xMin</c> / <c>x = xMax</c> faces
    /// away from the viewer. At the default camera that is the left-hand wall. Default: theme color.</summary>
    public Color? LeftWallColor { get; init; }

    /// <summary>Color of the Y-axis wall pane — whichever of <c>y = yMin</c> / <c>y = yMax</c> faces
    /// away from the viewer. At the default camera that is the right-hand wall. Default: theme color.</summary>
    public Color? RightWallColor { get; init; }

    /// <summary>Opacity applied to the pane surfaces, scaling each colour's own alpha.
    /// Range [0, 1]; 1 (the default) leaves every pane colour exactly as supplied.</summary>
    public double Alpha { get; init; } = 1.0;

    /// <summary>Whether panes are visible. Set <c>false</c> for a transparent 3D background.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>The configured colour for one axis' pane, or <c>null</c> to fall back to the theme.</summary>
    /// <param name="axis">The axis whose pane is being drawn.</param>
    public Color? ColorFor(CubeAxis axis) => axis switch
    {
        CubeAxis.X => LeftWallColor,
        CubeAxis.Y => RightWallColor,
        _ => FloorColor,
    };
}
