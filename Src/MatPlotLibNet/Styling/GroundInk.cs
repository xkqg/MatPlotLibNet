// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>How a series text that stands on a ground the series paints itself (a pie wedge, a table cell, a treemap tile) is inked: in the
/// theme's text colour where that colour's contrast with the ground reaches <see cref="Contrast"/>, else in black or white, whichever
/// stands out. The one owner of the two inks: <see cref="Readable"/>, the library's, and <see cref="TextColour"/>, matplotlib's for the
/// text it draws on a patch of its own. A kind's look names the one its text takes (<see cref="WedgeLook.PercentInk"/>,
/// <see cref="TableLook.TextInk"/>); a kind matplotlib has no artist for keeps <see cref="Readable"/> under every theme.</summary>
/// <param name="Contrast">The contrast with the ground the theme's text colour must reach to be kept.</param>
internal readonly record struct GroundInk(double Contrast)
{
    /// <summary>The library's ink: the theme's text colour where it reaches the contrast WCAG asks of a graphic against what lies behind
    /// it (3 : 1), else black or white. A light text on a white cell or a black one on a navy segment is a text nobody reads.</summary>
    internal static GroundInk Readable => new(3.0);

    /// <summary>matplotlib's ink for the text it draws on a patch of its own (a pie's percentages, a table's cells): <c>text.color</c>,
    /// whatever the ground (printed with 3.11.2: <c>autopct</c> texts <c>get_color()</c> 'k' under the classic style, on its pure blue
    /// wedge too, 'black' under the default one).</summary>
    internal static GroundInk TextColour => new(0);

    /// <summary>The colour a text is written in on a ground seen as <paramref name="seen"/>.</summary>
    /// <param name="foreground">The theme's text colour.</param>
    /// <param name="seen">The ground as it is seen, its alpha laid over the axes background.</param>
    /// <returns>The colour of the text.</returns>
    internal Color On(Color foreground, Color seen) =>
        foreground.ContrastRatioWith(seen) >= Contrast ? foreground : seen.ContrastingTextColor();
}
