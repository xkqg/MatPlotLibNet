// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>A text placed on the figure itself rather than in a subplot (matplotlib's <c>figtext</c>): a note, a source
/// line, a panel letter. Its position is a pair of fractions of the figure, so it stays where it is put whatever the
/// subplots show, and it is drawn above every subplot.</summary>
public sealed class FigureText
{
    /// <summary>The text that is drawn, as given.</summary>
    public string Text { get; }

    /// <summary>The horizontal position of the text's anchor point as a fraction of the figure's width: 0 at the left edge,
    /// 1 at the right.</summary>
    public double X { get; }

    /// <summary>The vertical position of the text's anchor point as a fraction of the figure's height, counted up from the
    /// bottom as in matplotlib: 0 at the bottom edge, 1 at the top, so 0.97 is just under the top.</summary>
    public double Y { get; }

    /// <summary>The font the text is drawn in. <see langword="null"/> (the default) takes the theme's text font, the one an
    /// annotation is drawn in; a font that carries no colour of its own takes <see cref="Color"/>.</summary>
    public Font? Font { get; set; }

    /// <summary>The colour of the text. <see langword="null"/> (the default) takes the theme's text colour.</summary>
    public Color? Color { get; set; }

    /// <summary>How the text stands against its point horizontally: <see cref="TextAlignment.Left"/> (the default) starts
    /// the text at the point, <see cref="TextAlignment.Center"/> centres it on the point and <see cref="TextAlignment.Right"/>
    /// ends it at the point.</summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>How the text stands against its point vertically: <see cref="TextVerticalAlignment.Baseline"/> (the
    /// default) puts the baseline on the point, <see cref="TextVerticalAlignment.Center"/> centres the text on it,
    /// <see cref="TextVerticalAlignment.Top"/> hangs the text from it and <see cref="TextVerticalAlignment.Bottom"/> stands
    /// the text on it.</summary>
    public TextVerticalAlignment VerticalAlignment { get; set; } = TextVerticalAlignment.Baseline;

    /// <summary>The angle the text is turned through about its point, in degrees, counter-clockwise when positive. 0 (the
    /// default) leaves the text horizontal.</summary>
    public double Rotation { get; set; }

    /// <summary>Creates a text at a place on the figure. A position outside 0 to 1, or one that is not a number, is kept
    /// but not drawn: the text has no place on the figure.</summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="x">The horizontal position as a fraction of the figure's width, from the left.</param>
    /// <param name="y">The vertical position as a fraction of the figure's height, from the bottom.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public FigureText(string text, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        X = x;
        Y = y;
    }

    /// <summary>The point the text is anchored at on a figure of the given size, in pixels with y downward:
    /// <c>(X·width, (1 − Y)·height)</c>. <see langword="null"/> when a fraction is not a finite number or lies outside 0 to 1,
    /// so the text is skipped rather than drawn off the figure.</summary>
    /// <param name="width">The figure's width in pixels.</param>
    /// <param name="height">The figure's height in pixels.</param>
    internal Point? AnchorOn(double width, double height) =>
        X is >= 0 and <= 1 && Y is >= 0 and <= 1
            ? new Point(X * width, (1 - Y) * height)
            : null;

    /// <summary>Whether the text shows on a figure of the given size: it has a place on the figure (<see cref="AnchorOn"/>) and
    /// some character in it that is not a blank. The one rule for what the picture carries of its own, read by the data table
    /// and by the MCP reader.</summary>
    /// <param name="width">The figure's width in pixels.</param>
    /// <param name="height">The figure's height in pixels.</param>
    internal bool IsShownOn(double width, double height) =>
        AnchorOn(width, height) is not null && !string.IsNullOrWhiteSpace(Text);
}
