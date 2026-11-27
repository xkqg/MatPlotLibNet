// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>The pads a layout keeps (<see cref="ConstrainedLayoutEngine"/>): past what it reserves for an axes on each edge of the
/// figure, how much of the pads of two axes whose needs meet in the gap between them that gap keeps only once, and the bounds its
/// margins and gaps are held in.</summary>
/// <param name="Edges">The pad past what an axes needs on each of its edges.</param>
/// <param name="Shared">How much of the two pads in a gap the gap keeps once: none for the library's, which keeps each axes' pad, and
/// a whole pad for matplotlib's <c>tight_layout</c>, whose gap is what the two axes draw into it and one pad.</param>
/// <param name="Bounds">The floors and the caps of the margins and the gaps (<see cref="LayoutBounds"/>).</param>
internal readonly record struct LayoutPads(Sides<double> Edges, double Shared, LayoutBounds Bounds)
{
    /// <summary>The library's pads: five pixels on the left and below, eight on the right and above, each axes' own in a gap, and the
    /// library's bounds.</summary>
    internal static LayoutPads Library { get; } = new(
        new Sides<double>(ConstrainedLayoutEngine.PadLeft, ConstrainedLayoutEngine.PadBottom, ConstrainedLayoutEngine.PadTop,
            ConstrainedLayoutEngine.PadRight), 0, LayoutBounds.Library);

    /// <summary>The pads <paramref name="figure"/> is laid out with: under a theme whose figure look names a tight pad
    /// (<see cref="Styling.FigureLook.TightPad"/>, the matplotlib themes), a tight layout keeps that many of the theme's font size at
    /// every edge and once in every gap, as matplotlib's <c>tight_layout</c> keeps its <c>pad</c> (and <c>h_pad</c>, <c>w_pad</c>,
    /// which default to it), and no bound on a margin or a gap, as <c>tight_layout</c> keeps none (<see cref="LayoutBounds.None"/>);
    /// any other layout, and every layout under the library's themes, the library's pads and bounds.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <returns>The pads of the figure.</returns>
    internal static LayoutPads Of(Figure figure) =>
        figure.Spacing.TightLayout && figure.Theme.Looks.Figure.TightPad is { } pad
            ? Even(pad * figure.Theme.DataTextFont().Size)
            : Library;

    /// <summary>One pad of <paramref name="pad"/> pixels at every edge and once in every gap, and no bound: matplotlib's
    /// <c>tight_layout</c>.</summary>
    private static LayoutPads Even(double pad) => new(new Sides<double>(pad, pad, pad, pad), pad, LayoutBounds.None);

    /// <summary>The gap two needs take that meet in it, each with its own pad: both, less <see cref="Shared"/> when both draw
    /// something there, and never less than either needs alone.</summary>
    /// <param name="first">The need of the axes on one side of the gap, with its pad; 0 when it draws nothing there.</param>
    /// <param name="second">The need of the axes on the other side, with its pad; 0 when it draws nothing there.</param>
    /// <returns>The width or height of the gap, in pixels.</returns>
    internal double Stacked(double first, double second) =>
        Math.Max(Math.Max(first, second), first > 0 && second > 0 ? first + second - Shared : first + second);
}
