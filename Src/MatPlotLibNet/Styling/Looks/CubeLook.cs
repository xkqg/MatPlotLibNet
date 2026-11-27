// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>The box of a 3-D axes and the stride of its surfaces.</summary>
internal sealed record CubeLook : IThemeKeys<CubeLook>
{
    /// <summary>The library's own box.</summary>
    internal static CubeLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.WallGrid = WallGrid is { } wall ? GridDto.From(wall) : null;
        keys.SurfaceStride = SurfaceStride;
        keys.CubeAxisLineWidth = AxisLineWidth;
    }

    /// <inheritdoc />
    public static CubeLook ReadKeys(ThemeDto keys) =>
        new() { WallGrid = keys.WallGrid?.ToGridStyle(), SurfaceStride = keys.SurfaceStride, AxisLineWidth = keys.CubeAxisLineWidth };

    /// <summary>The grid the three back walls wear when the theme names one of their own, as matplotlib's classic style does (light
    /// grey, 1 pt, solid, where its 2-D grid is black and dotted). Unset: the theme's grid. An author's change applies over it.</summary>
    public GridStyle? WallGrid { get; init; }

    /// <summary>The stride a 3-D surface is drawn with when its author sets none: every tenth row and column under the classic style
    /// (<c>plot_surface</c> in classic mode). Unset: at most <see cref="Rendering.SurfaceFaces.Strides.MostPatchesASide"/> patches along
    /// each side of the grid, as matplotlib's default style draws.</summary>
    public int? SurfaceStride { get; init; }

    /// <summary>How wide the three axis lines of a 3-D box are drawn, in pixels, under a theme that bounds the box with them as
    /// matplotlib does (its 3-D axis draws an <c>axisline</c> along the edge its ticks stand on: <c>axes.linewidth</c> under the
    /// default style, 0.75 pt in classic mode), in the theme's text colour, and no other edge of the box. Unset: the back panes are
    /// bounded with nine grey half-pixel edges.</summary>
    public double? AxisLineWidth { get; init; }

    /// <summary>The grid on the back walls of a 3-D box: this look's, else <paramref name="resting"/>.</summary>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal GridStyle WallGridOr(GridStyle resting) => WallGrid ?? resting;
}
