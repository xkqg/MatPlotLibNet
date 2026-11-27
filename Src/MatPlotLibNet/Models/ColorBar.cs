// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models;

/// <summary>Configures a color bar that displays a colormap gradient alongside the plot area.</summary>
public sealed record ColorBar
{
    /// <summary>Whether the colour bar is drawn. Off by default, so a configured bar stays hidden until this is switched on.</summary>
    public bool Visible { get; init; }

    /// <summary>The colour map the gradient is drawn with. <see langword="null"/> (the default) takes the colour map of the
    /// first series that provides one, and viridis when no series does.</summary>
    public IColorMap? ColorMap { get; init; }

    /// <summary>The value at the low end of the bar. Used when no series on the axes provides a range of its own, which
    /// takes precedence; default 0.</summary>
    public double Min { get; init; }

    /// <summary>The value at the high end of the bar. Used when no series on the axes provides a range of its own, which
    /// takes precedence; default 1.</summary>
    public double Max { get; init; } = 1;

    /// <summary>A caption drawn beside the bar, rotated to run along it for a vertical bar. <see langword="null"/> (the
    /// default) draws none.</summary>
    public string? Label { get; init; }

    /// <summary>The thickness of the bar, in pixels. Default <see cref="RestingWidth"/>, which a vertical bar is drawn at under the library's own themes; a
    /// horizontal bar uses it only when <see cref="Aspect"/> is 0 or less. Under the matplotlib themes a bar given no thickness is
    /// as thick as it is long over <see cref="Aspect"/>, as matplotlib draws it; a thickness you give is drawn under every theme.</summary>
    public double Width { get => GivenWidth ?? RestingWidth; init => GivenWidth = value; }

    /// <summary>The gap between the axes and the bar, in pixels. Default <see cref="RestingPadding"/>, the gap under the library's own themes: a bar beside
    /// the axes stands that far past what the axes draws past its right edge (a second Y axis with its labels and title, mirrored Y
    /// tick labels, end labels; the plot area itself when there is none of these), and a bar below stands that far under the lowest
    /// thing the axes draws under its plot (its X tick labels, or its X axis title when it has one), so the bar covers none of them;
    /// every layout keeps the room for the
    /// bar and its labels, the default one only where its margin is smaller than they need. Under the matplotlib themes the bar
    /// takes its room from the axes, as matplotlib's <c>fig.colorbar</c> does, and a bar given no gap stands 0.05 of the axes' cell
    /// away (0.15 of its height below them); under the constrained layout it stands in the margin as matplotlib's constrained layout
    /// stands it, 0.05 of the axes' width (of their height below them) past what the axes draws there. A gap you give is drawn
    /// under every theme.</summary>
    public double Padding { get => GivenPadding ?? RestingPadding; init => GivenPadding = value; }

    /// <summary>The thickness the bar reads when it was given none.</summary>
    internal const double RestingWidth = 20;

    /// <summary>The gap the bar reads when it was given none.</summary>
    internal const double RestingPadding = 10;

    /// <summary>The thickness the author gave, <see langword="null"/> when none: a theme that gives a bar matplotlib's room draws
    /// it at its own thickness then.</summary>
    internal double? GivenWidth { get; private init; }

    /// <summary>The gap the author gave, <see langword="null"/> when none.</summary>
    internal double? GivenPadding { get; private init; }

    /// <summary>Whether the bar grows an extra slot past its low end, its high end or both, for the values outside the
    /// range; a slot shows the colour map's under or over colour. Neither end by default. Under the library's own themes a slot is
    /// a rectangle a tenth of the bar long, inside it; under the matplotlib themes it is a triangle 0.05 of the bar's interior
    /// long, outside the interior, as matplotlib draws it, and the frame runs round its tip.</summary>
    public ColorBarExtend Extend { get; init; } = ColorBarExtend.Neither;

    /// <summary>Whether the bar runs vertically beside the plot area (the default) or horizontally below it.</summary>
    public ColorBarOrientation Orientation { get; init; } = ColorBarOrientation.Vertical;

    /// <summary>The length of the bar as a fraction of the plot area's side it runs along, centred on it: 1 (the default)
    /// spans the whole side, 0.5 half of it.</summary>
    public double Shrink { get; init; } = 1.0;

    /// <summary>Whether thin lines are drawn between the steps of the gradient. Off by default.</summary>
    public bool DrawEdges { get; init; }

    /// <summary>The ratio of a bar's length to its thickness. Default 20; with a value of 0 or less the bar is
    /// <see cref="Width"/> thick instead. Under the library's own themes a vertical bar takes its thickness from
    /// <see cref="Width"/>; under the matplotlib themes a bar given no <see cref="Width"/> takes it from this ratio either way
    /// round, as matplotlib's <c>aspect</c>.</summary>
    public double Aspect { get; init; } = 20;

    /// <summary>The tick locator that places the bar's ticks, as matplotlib's <c>Colorbar.locator</c>: every tick it places inside
    /// the bar's range is named, whatever the length of the bar. <see langword="null"/> (the default) lets the bar choose as many
    /// ticks as fit along its length, and a bar of bands names its boundaries.</summary>
    public ITickLocator? Locator { get; init; }

    /// <summary>The one rule for a colour bar that a caller asks for, the bar after a <c>WithColorBar</c> call: the bar already set is
    /// kept, a bar not yet set starts visible (asking for a bar is asking to see it), and the caller's edit is applied last.
    /// <c>AxesBuilder.WithColorBar</c> and <c>FigureBuilder.WithColorBar</c> both take their bar through it.</summary>
    /// <param name="bar">The bar already set, or <see langword="null"/> when there is none.</param>
    /// <param name="configure">Function that receives the bar and returns a modified copy; <see langword="null"/> keeps it.</param>
    /// <returns>The bar to set.</returns>
    internal static ColorBar AskedFor(ColorBar? bar, Func<ColorBar, ColorBar>? configure)
    {
        var asked = bar ?? new ColorBar { Visible = true };
        return configure is null ? asked : configure(asked);
    }
}
