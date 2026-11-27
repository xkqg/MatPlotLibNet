// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>One value for each of the four sides of a rectangle: which edges of the figure's grid a cell touches
/// (<c>Sides&lt;bool&gt;</c>), the room an axes needs or the figure keeps on each edge, the pads of a layout and the bounds of its
/// margins. A direction reads its start and its end through the Along members (<see cref="AlongExtensions"/>): the left and the
/// right across the grid, the top and the bottom down it.</summary>
/// <typeparam name="T">What each side holds.</typeparam>
/// <param name="Left">The left side.</param>
/// <param name="Bottom">The bottom side.</param>
/// <param name="Top">The top side.</param>
/// <param name="Right">The right side.</param>
internal readonly record struct Sides<T>(T Left, T Bottom, T Top, T Right);
