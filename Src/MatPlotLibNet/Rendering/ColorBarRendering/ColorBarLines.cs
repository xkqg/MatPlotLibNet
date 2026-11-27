// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The lines the colour bar of a contour of lines draws in place of a fill: one across the bar at each level, in the colour
/// and the width the contour draws that level's line in, as matplotlib's <c>Colorbar.add_lines</c> draws them for a <c>ContourSet</c>
/// that is not filled.</summary>
/// <param name="Levels">The levels, ascending.</param>
/// <param name="Colors">The colour of each level's line, beside <paramref name="Levels"/>.</param>
/// <param name="Width">The width of every line, in pixels.</param>
internal readonly record struct ColorBarLines(double[] Levels, Color[] Colors, double Width);
