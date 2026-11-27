// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The caps across the ends of a box's whiskers where its author names nothing: their colour, their width as a share of the
/// box's and the width of the line they are drawn with.</summary>
/// <param name="Color">The colour of the caps (<c>boxplot.capprops.color</c>); <see langword="null"/> draws them in the colour the box is
/// drawn in.</param>
/// <param name="Share">How wide a cap is, as a share of the box's width (<c>Axes.bxp</c>: <c>capwidths = 0.5 * widths</c>).</param>
/// <param name="Width">The width of the line of a cap, in pixels (<c>boxplot.capprops.linewidth</c>).</param>
internal readonly record struct BoxCaps(Color? Color, double Share, double Width)
{
    /// <summary>The line a cap is drawn with: its colour and width, solid, as any other line of the box (<see cref="BoxStroke"/>).</summary>
    internal BoxStroke Line => new(Color, Width);
}
