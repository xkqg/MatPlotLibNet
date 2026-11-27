// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet.Styling;

/// <summary>How a legend's box is laid out round its rows, a value of <see cref="LegendLook.Box"/>: how far its frame pads the rows,
/// how far it stands inside the corner of what it is placed in, how tall a row and the title's band are and where their text stands,
/// how wide the box is beside a title, and which column and row each entry takes. The library's own themes keep the box a legend has
/// always had (<see cref="Library"/>); the matplotlib themes lay it out as matplotlib's <c>Legend</c> does (<see cref="Matplotlib"/>).
/// The legend's layout asks this value and nothing else.</summary>
internal abstract record LegendBox
{
    /// <summary>The box of the library's own themes.</summary>
    internal static LegendBox Library { get; } = new LibraryLegendBox();

    /// <summary>The box of the matplotlib themes.</summary>
    internal static LegendBox Matplotlib { get; } = new MatplotlibLegendBox();

    /// <summary>The padding inside the frame on every side, in pixels.</summary>
    /// <param name="fontSize">The size of the legend's text, in pixels.</param>
    /// <returns>The padding, in pixels.</returns>
    internal abstract double PaddingOf(double fontSize);

    /// <summary>How far a box placed inside an area (a corner, an edge) stands from its edges, in pixels.</summary>
    /// <param name="fontSize">The size of the legend's text, in pixels.</param>
    /// <returns>The inset, in pixels.</returns>
    internal abstract double InsetOf(double fontSize);

    /// <summary>The text every text of the legend (an entry's, the title) is boxed at least as tall as, measured in its font through the
    /// text seam (<see cref="TextExtent.AtLeast"/>), or <see langword="null"/> for a box that takes each text as it measures.</summary>
    internal virtual string? LineFloor => null;

    /// <summary>The row an entry takes: its height, where its text's baseline and its handle's top stand below its top. Every entry
    /// takes its own row, laid out by its own text.</summary>
    /// <param name="handleHeight">The height of the entry's handle, in pixels.</param>
    /// <param name="text">The extent of the entry's text, measured through the one text seam
    /// (<see cref="Rendering.TextMeasurement.TextExtentExtensions"/>' <c>ExtentOf</c> of a <c>TextLabel</c>).</param>
    /// <returns>The row: its height and where the text and the handle stand in it.</returns>
    internal abstract LegendRow RowOf(double handleHeight, TextExtent text);

    /// <summary>The band the title takes above the rows: its height, the gap below it included, and where its baseline stands below
    /// the band's top.</summary>
    /// <param name="title">The extent of the title in its font.</param>
    /// <param name="labelSpacing">The gap between two rows, in pixels.</param>
    /// <returns>The band of the title.</returns>
    internal abstract LegendRow TitleOf(TextExtent title, double labelSpacing);

    /// <summary>How tall the rectangle an entry is hit-tested by is: inside the box, whichever row it is.</summary>
    /// <param name="row">The entry's row (<see cref="RowOf"/>).</param>
    /// <param name="lineHeight">The entry's row and the gap below it.</param>
    /// <returns>The height of the hit-test rectangle, in pixels.</returns>
    internal abstract double ItemHeightOf(LegendRow row, double lineHeight);

    /// <summary>How wide the inside of the box is, padding left out: the wider of the entries and the title, under every box, so a title
    /// wider than the entries widens the box and the entries stand centred under it, as matplotlib's <c>VPacker</c> (align 'center') lays
    /// the title over the entries.</summary>
    /// <param name="entries">The width of the entries' columns together.</param>
    /// <param name="title">The width of the title; 0 without one.</param>
    /// <returns>The width of the inside of the box, in pixels.</returns>
    internal static double InnerWidthOf(double entries, double title) => Math.Max(entries, title);

    /// <summary>How many columns the entries stand in.</summary>
    /// <param name="count">How many entries there are.</param>
    /// <param name="asked">How many columns the legend asks for.</param>
    /// <returns>The number of columns.</returns>
    internal abstract int ColumnsOf(int count, int asked);

    /// <summary>The column and row entry <paramref name="index"/> takes.</summary>
    /// <param name="index">The entry's place in the list.</param>
    /// <param name="count">How many entries there are.</param>
    /// <param name="columns">How many columns they stand in (<see cref="ColumnsOf"/>).</param>
    /// <returns>The column and the row of the entry.</returns>
    internal abstract LegendCell CellOf(int index, int count, int columns);
}

/// <summary>The box a legend has always had under the library's own themes: <see cref="Padding"/> of padding, the inset of a corner
/// (<see cref="Rendering.LegendRendering.LegendPositionStrategy.Inset"/>), a row as tall as its handle with the text's baseline a pixel above
/// the handle's foot, a bold title band of its font size and <see cref="TitleGap"/>, the entries row after row.</summary>
internal sealed record LibraryLegendBox : LegendBox
{
    /// <summary>The padding inside the frame, in pixels.</summary>
    private const double Padding = 8;

    /// <summary>The gap below the title, in pixels.</summary>
    private const double TitleGap = 4;

    /// <inheritdoc />
    internal override double PaddingOf(double fontSize) => Padding;

    /// <inheritdoc />
    internal override double InsetOf(double fontSize) => Rendering.LegendRendering.LegendPositionStrategy.Inset;

    /// <inheritdoc />
    internal override LegendRow RowOf(double handleHeight, TextExtent text) => new(handleHeight, handleHeight - 1, 0);

    /// <inheritdoc />
    /// <remarks>The letters of the title rise its font size above its baseline (<see cref="TextExtent.Ascent"/>), as the library has
    /// always placed it.</remarks>
    internal override LegendRow TitleOf(TextExtent title, double labelSpacing) => new(title.Ascent + TitleGap, title.Ascent, 0);

    /// <inheritdoc />
    /// <remarks>The row and the gap below it: the padding (8 pixels) is wider than the gap for any font up to 16 pixels.</remarks>
    internal override double ItemHeightOf(LegendRow row, double lineHeight) => lineHeight;

    /// <inheritdoc />
    internal override int ColumnsOf(int count, int asked) => Math.Max(1, asked);

    /// <inheritdoc />
    internal override LegendCell CellOf(int index, int count, int columns) => new(index / columns, index % columns);
}

/// <summary>The box of matplotlib's <c>Legend</c> (<c>_init_legend_box</c>, placed by <c>_get_anchored_bbox</c>): <c>legend.borderpad</c> (0.4
/// of the font size) of padding, <c>legend.borderaxespad</c> (0.5 of it) inside a corner, a row as tall as its text or its handle, whichever
/// rises higher above the baseline (an <c>HPacker</c> aligned on the baseline, the handle's foot on it), plus the text's descent; the title a
/// band of its text and <c>labelspacing</c>, the box as wide as the wider of the title and the entries, which stand centred under it; the entries
/// dealt to the columns in turn by <c>np.array_split</c>, an empty column dropped.</summary>
internal sealed record MatplotlibLegendBox : LegendBox
{
    /// <summary><c>legend.borderpad</c>, in multiples of the font size.</summary>
    private const double BorderPad = 0.4;

    /// <summary><c>legend.borderaxespad</c>, in multiples of the font size.</summary>
    private const double BorderAxesPad = 0.5;

    /// <inheritdoc />
    internal override double PaddingOf(double fontSize) => BorderPad * fontSize;

    /// <inheritdoc />
    internal override double InsetOf(double fontSize) => BorderAxesPad * fontSize;

    /// <inheritdoc />
    /// <remarks>'lp': matplotlib's <c>TextArea</c> boxes a text at least as high above its baseline and as deep below it as 'lp'
    /// (printed with 3.11.2 at 10 points: 'x' alone is 13.889 tall, 'x' in a legend 14.337, the rise of 'lp' 11 pixels).</remarks>
    internal override string? LineFloor => "lp";

    /// <inheritdoc />
    internal override LegendRow RowOf(double handleHeight, TextExtent text)
    {
        double baseline = Math.Max(handleHeight, text.Ascent);
        return new LegendRow(baseline + text.Descent, baseline, baseline - handleHeight);
    }

    /// <inheritdoc />
    internal override LegendRow TitleOf(TextExtent title, double labelSpacing) => new(title.Height + labelSpacing, title.Ascent, 0);

    /// <inheritdoc />
    /// <remarks>The row itself, the box of the entry's <c>HPacker</c>: the padding (0.4 of the font size) is narrower than the gap below a
    /// row (0.5 of it), so a row and its gap would reach past the frame.</remarks>
    internal override double ItemHeightOf(LegendRow row, double lineHeight) => row.Height;

    /// <inheritdoc />
    internal override int ColumnsOf(int count, int asked) => Math.Clamp(asked, 1, Math.Max(1, count));

    /// <inheritdoc />
    /// <remarks>The first <c>count % columns</c> columns take one entry more than the rest.</remarks>
    internal override LegendCell CellOf(int index, int count, int columns)
    {
        int shortest = count / columns, longer = count % columns;
        int inLonger = longer * (shortest + 1);
        if (index < inLonger) return new LegendCell(index % (shortest + 1), index / (shortest + 1));
        int past = index - inLonger;
        return new LegendCell(past % shortest, longer + past / shortest);
    }
}

/// <summary>The band a row of a legend (or its title) takes.</summary>
/// <param name="Height">Its height, in pixels.</param>
/// <param name="Baseline">How far below its top the baseline of its text stands.</param>
/// <param name="HandleTop">How far below its top the top of its handle stands.</param>
internal readonly record struct LegendRow(double Height, double Baseline, double HandleTop);

/// <summary>Where one entry of a legend stands, as its layout placed it: the top of its row, the row (<see cref="LegendBox.RowOf"/>, laid
/// out by the entry's own text) and how tall the rectangle it is hit-tested by is (<see cref="LegendBox.ItemHeightOf"/>). The drawing
/// and the hit-test both read it.</summary>
/// <param name="Top">The Y of the top of the entry's row, in pixels.</param>
/// <param name="Row">The entry's row.</param>
/// <param name="HitHeight">The height of the entry's hit-test rectangle, in pixels.</param>
internal readonly record struct LegendSlot(double Top, LegendRow Row, double HitHeight);

/// <summary>Where an entry of a legend stands among the others.</summary>
/// <param name="Row">Its row, from the top.</param>
/// <param name="Column">Its column, from the left.</param>
internal readonly record struct LegendCell(int Row, int Column);
