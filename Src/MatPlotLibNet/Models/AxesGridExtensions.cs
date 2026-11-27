// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>The grid an axes draws under a theme: the one rule every renderer draws a grid by and the JSON writer resolves a change with.</summary>
internal static class AxesGridExtensions
{
    extension(Axes axes)
    {
        /// <summary>The grid this axes draws under <paramref name="theme"/>: the theme's grid with the axes' change when it asked for one
        /// (<see cref="Axes.GridChange"/>), its own <see cref="Axes.Grid"/> when that is switched on, and the theme's grid otherwise.</summary>
        /// <param name="theme">The theme the axes is drawn in.</param>
        internal GridStyle GridUnder(Theme theme) => axes.GridOver(theme.DefaultGrid);

        /// <summary>The grid of an axes whose grid is part of its shape (the rings and spokes of a polar axes, the lines of a ternary
        /// triangle; the back walls of a 3-D box through <see cref="WallGridUnder"/>): <see cref="GridUnder"/>, but drawn unless the axes
        /// said otherwise, whatever the theme's grid says. A chart read by its rings, or standing in a box, is not a chart under a theme
        /// that draws no grid; only an explicit "no grid" (a change that sets <see cref="GridStyle.Visible"/> to false) removes it.</summary>
        /// <param name="theme">The theme the axes is drawn in.</param>
        internal GridStyle ShapeGridUnder(Theme theme) => axes.ShapeGridOver(theme.DefaultGrid);

        /// <summary>The grid on the three back walls of a 3-D box: <see cref="ShapeGridUnder"/> over the theme's own wall grid
        /// (<see cref="CubeLook.WallGrid"/>, matplotlib's classic style names one) where it names one.</summary>
        /// <param name="theme">The theme the axes is drawn in.</param>
        /// <returns>The grid of the three back walls under the theme.</returns>
        internal GridStyle WallGridUnder(Theme theme) => axes.ShapeGridOver(theme.Looks.Cube.WallGridOr(theme.DefaultGrid));

        /// <summary>The grid this axes draws over <paramref name="themeGrid"/>, the grid of the theme it is drawn under: the author's
        /// change applied to it, else the axes' own grid when that is shown, else the theme's.</summary>
        private GridStyle GridOver(GridStyle themeGrid) =>
            axes.GridChange is { } change ? change.ApplyTo(themeGrid) : axes.Grid.Visible ? axes.Grid : themeGrid;

        private GridStyle ShapeGridOver(GridStyle themeGrid) => axes.GridOver(themeGrid) with { Visible = axes.GridChange?.Visible ?? true };
    }
}
