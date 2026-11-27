// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What the cells of a table are where their author names nothing: where the table stands, the fills of the headers and of the
/// data, the colour of the rules, the height of a row, how the data and the row headers are aligned, how far an aligned text stands in
/// from its cell's edge, whether a cell stands at the corner above the row headers and whether the text is made smaller to fit its cells.
/// The matplotlib themes cell a table as <c>ax.table</c> does (<see cref="Matplotlib"/>); the library's own themes as the library always
/// has (<see cref="Library"/>). Each value meets the one its author gave in this look's own members (<see cref="HeaderFillOver"/> and
/// the others).</summary>
internal sealed record TableLook : IThemeKeys<TableLook>
{
    /// <inheritdoc />
    /// <remarks>The look is named as a whole: matplotlib's (<see cref="Matplotlib"/>) or the library's.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.TablesLikeMatplotlib = (this == Matplotlib).OrNullAtRest(false);

    /// <inheritdoc />
    public static TableLook ReadKeys(ThemeDto keys) => keys.TablesLikeMatplotlib == true ? Matplotlib : Library;

    /// <summary>The library's own tables: in the plot from its top, light grey headers, white data, dark grey rules, rows 25 px high, the
    /// data left aligned and the row headers centred, an aligned text 4 px in from its cell's edge, a header cell at the corner and the
    /// text at the size it is given.</summary>
    internal static TableLook Library { get; } = new();

    /// <summary>The cells of matplotlib 3.11.2's <c>ax.table</c>: below the axes (<c>loc='bottom'</c>, the default of
    /// <c>matplotlib.table.table</c>), every cell white with a black rule (<c>Cell(edgecolor='k', facecolor='w')</c>), a row
    /// <c>Table.FONTSIZE</c> (10) times 1.2 points high on the axes it was made on, a share of that axes' height it keeps when a layout
    /// moves the axes (<see cref="RowsKeepTheirShare"/>), the data right aligned (<c>cellLoc='right'</c>) and the row labels left aligned
    /// (<c>rowLoc='left'</c>), an aligned text a tenth of its cell's width in (<c>Cell.PAD = 0.1</c>), no cell at the corner above the row
    /// labels (<c>table</c> adds none at <c>(0, -1)</c>) and the text made smaller until it fits its cells
    /// (<c>Table._auto_set_font_size</c>, on unless <c>auto_set_font_size(False)</c>).</summary>
    internal static TableLook Matplotlib { get; } = new()
    {
        Location = TableLocation.BelowAxes, HeaderFill = Colors.White, Border = Colors.Black, CellHeight = (10 * 1.2).PointsInPixels,
        DataAlignment = TextAlignment.Right, RowHeaderAlignment = TextAlignment.Left, PadPixels = 0, PadShare = 0.1,
        TextInk = GroundInk.TextColour, CornerCell = false, FitsTextToCells = true, RowsKeepTheirShare = true,
    };

    /// <summary>Where a table stands relative to its axes.</summary>
    public TableLocation Location { get; init; } = TableLocation.InPlot;

    /// <summary>Whether a header cell stands at the corner above the row headers when the table has both row and column headers.</summary>
    public bool CornerCell { get; init; } = true;

    /// <summary>Whether the text of a table is set smaller, a point at a time, until the text of every cell and its pads fit the cell.</summary>
    public bool FitsTextToCells { get; init; }

    /// <summary>Whether a row the author gave no height keeps its share of the height of the axes the table was made on
    /// (<see cref="CellHeightOver"/>), as matplotlib's rows do; else a row is <see cref="CellHeight"/> whatever moves the axes.</summary>
    public bool RowsKeepTheirShare { get; init; }

    /// <summary>How the text of a cell is inked on its fill: readable under the library's own themes, <c>text.color</c> under the
    /// matplotlib themes (a <c>Cell</c>'s text takes the axes' text colour).</summary>
    public GroundInk TextInk { get; init; } = GroundInk.Readable;

    /// <summary>The fill of the column and row header cells.</summary>
    public Color HeaderFill { get; init; } = new(200, 200, 200);

    /// <summary>The fill of the data cells.</summary>
    public Color CellFill { get; init; } = Colors.White;

    /// <summary>The colour of the rule round every cell.</summary>
    public Color Border { get; init; } = new(80, 80, 80);

    /// <summary>The height of every row, the header row included, in pixels.</summary>
    public double CellHeight { get; init; } = 25;

    /// <summary>How the text of a data column is aligned in its cell.</summary>
    public TextAlignment DataAlignment { get; init; } = TextAlignment.Left;

    /// <summary>How the text of a row header is aligned in its cell.</summary>
    public TextAlignment RowHeaderAlignment { get; init; } = TextAlignment.Center;

    /// <summary>How far a left or right aligned text stands in from its cell's edge, in pixels, besides <see cref="PadShare"/>.</summary>
    public double PadPixels { get; init; } = 4;

    /// <summary>How far a left or right aligned text stands in from its cell's edge, as a share of the cell's width, besides
    /// <see cref="PadPixels"/>.</summary>
    public double PadShare { get; init; }

    /// <summary>Where a table stands: where its author put it, else where this look stands one.</summary>
    /// <param name="given">The location its author gave, or <see langword="null"/>.</param>
    /// <returns>The location.</returns>
    internal TableLocation LocationOver(TableLocation? given) => given ?? Location;

    /// <summary>The font the text of a cell is set in on a cell of <paramref name="ground"/>: the theme's data text font at
    /// <paramref name="size"/>, inked as this look inks a cell's text (<see cref="TextInk"/>).</summary>
    /// <param name="theme">The theme the table is drawn under.</param>
    /// <param name="ground">The fill of the cell.</param>
    /// <param name="size">The size of the text, in pixels.</param>
    /// <returns>The font.</returns>
    internal Font FontOn(Theme theme, Color ground, double size) => theme.DataTextFontOn(ground, size, TextInk);

    /// <summary>How wide a cell must be to hold a text <paramref name="textWidth"/> wide and its pads: the pad given on either side, else
    /// this look's pixels on either side and the share of the text's width matplotlib adds (<c>Cell.get_required_width</c>: the text's
    /// width times 1 + 2 <c>Cell.PAD</c>).</summary>
    /// <param name="textWidth">The width of the text, in pixels.</param>
    /// <param name="given">The pad its author gave, in pixels, or <see langword="null"/>.</param>
    /// <returns>The width, in pixels.</returns>
    internal double FittedWidth(double textWidth, double? given) =>
        given is { } pad ? textWidth + 2 * pad : textWidth * (1 + 2 * PadShare) + 2 * PadPixels;

    /// <summary>The fill of a header cell: the one given, else this look's.</summary>
    /// <param name="given">The fill its author gave, or <see langword="null"/>.</param>
    /// <returns>The fill.</returns>
    internal Color HeaderFillOver(Color? given) => given ?? HeaderFill;

    /// <summary>The fill of a data cell: the one given, else this look's.</summary>
    /// <param name="given">The fill its author gave, or <see langword="null"/>.</param>
    /// <returns>The fill.</returns>
    internal Color CellFillOver(Color? given) => given ?? CellFill;

    /// <summary>The colour of the rules: the one given, else this look's.</summary>
    /// <param name="given">The colour its author gave, or <see langword="null"/>.</param>
    /// <returns>The colour.</returns>
    internal Color BorderOver(Color? given) => given ?? Border;

    /// <summary>The height of a row: the one given, else this look's, which under a look whose rows keep their share
    /// (<see cref="RowsKeepTheirShare"/>) is a share of the axes' height: matplotlib makes it <c>Table.FONTSIZE</c> times 1.2 points of the
    /// height the axes has when <c>ax.table</c> is called (<c>Table._approx_text_height</c>, table.py:399-401), where the subplot
    /// parameters place it, and draws it as that share of the height the axes has when it is drawn, so a layout engine that moves the
    /// axes afterwards stretches the rows with it.</summary>
    /// <param name="given">The height its author gave, in pixels, or <see langword="null"/>.</param>
    /// <param name="stretch">The height of the axes the table is laid at over its height at the subplot parameters (1 where nothing
    /// moved it).</param>
    /// <returns>The height, in pixels.</returns>
    internal double CellHeightOver(double? given, double stretch) => given ?? (RowsKeepTheirShare ? CellHeight * stretch : CellHeight);

    /// <summary>Whether the height of a row follows the axes the table is laid at (<see cref="CellHeightOver"/>).</summary>
    /// <param name="given">The height its author gave, in pixels, or <see langword="null"/>.</param>
    internal bool Stretches(double? given) => given is null && RowsKeepTheirShare;

    /// <summary>How the data column at <paramref name="column"/> is aligned: as given for it, else as this look aligns the data.</summary>
    /// <param name="given">The alignments its author gave by column, or <see langword="null"/>.</param>
    /// <param name="column">The data column.</param>
    /// <returns>The alignment.</returns>
    internal TextAlignment AlignmentOf(TextAlignment[]? given, int column) => given.ElementOr(column, DataAlignment);

    /// <summary>How far a left or right aligned text stands in from the edge of a cell <paramref name="cellWidth"/> wide: the pad given,
    /// else this look's pixels and share of the width.</summary>
    /// <param name="given">The pad its author gave, in pixels, or <see langword="null"/>.</param>
    /// <param name="cellWidth">The width of the cell, in pixels.</param>
    /// <returns>The pad, in pixels.</returns>
    internal double PadOver(double? given, double cellWidth) => given ?? PadPixels + PadShare * cellWidth;
}
