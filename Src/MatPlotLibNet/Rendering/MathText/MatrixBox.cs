// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A matrix (<c>\begin{pmatrix} a &amp; b \\ c &amp; d \end{pmatrix}</c>; matplotlib has none, so it is laid out as LaTeX's
/// <c>matrix</c> environments are): its cells at the text's size, each centred in its column, the columns a
/// <see cref="MathConstants.MatrixColumnGap"/> apart, each row at least LaTeX's array strut tall, the whole centred on the math axis, and
/// the environment's delimiters grown to it (<see cref="SizedGlyph"/>).</summary>
/// <param name="environment">The environment's name, which the flat view carries.</param>
/// <param name="left">The left delimiter's character, or empty.</param>
/// <param name="right">The right delimiter's character, or empty.</param>
/// <param name="rows">The cells, row by row.</param>
internal sealed class MatrixBox(string environment, string left, string right, MathBox[][] rows) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var c = style.Constants;
        int columns = 0;
        foreach (var row in rows) columns = Math.Max(columns, row.Length);
        var widths = new double[columns];
        var tops = new double[rows.Length];
        var bottoms = new double[rows.Length];
        // The cells are laid one after the other, row by row, so the ink of cell k runs from marks[k] to marks[k + 1].
        int cellCount = 0;
        foreach (var row in rows) cellCount += row.Length;
        var marks = new MathInkMark[cellCount + 1];
        var cellWidths = new double[cellCount];
        int k = 0;
        for (int r = 0; r < rows.Length; r++)
        {
            tops[r] = c.MatrixStrutHeight * style.Size;
            bottoms[r] = c.MatrixStrutDepth * style.Size;
            for (int col = 0; col < rows[r].Length; col++, k++)
            {
                marks[k] = ink.Mark;
                var size = rows[r][col].Lay(ink, style);
                cellWidths[k] = size.Width;
                widths[col] = Math.Max(widths[col], size.Width);
                tops[r] = Math.Max(tops[r], size.Height);
                bottoms[r] = Math.Max(bottoms[r], size.Depth);
            }
        }

        marks[cellCount] = ink.Mark;
        double total = 0;
        for (int r = 0; r < rows.Length; r++) total += tops[r] + bottoms[r];
        double gap = c.MatrixColumnGap * style.Size, gridWidth = 0;
        for (int col = 0; col < columns; col++) gridWidth += widths[col] + (col > 0 ? gap : 0);
        // The rows stand centred on the math axis, as TeX centres a matrix.
        double height = Range1D.Around(style.AxisHeight, total).Hi, depth = total - height;

        double x = 0, extentTop = height, extentBottom = depth;
        var leftSize = Delimiter(ink, style, left, height, depth);
        x += leftSize.Width;
        double baseline = -height;
        k = 0;
        for (int r = 0; r < rows.Length; r++)
        {
            baseline += tops[r];
            double column = x;
            for (int col = 0; col < rows[r].Length; col++, k++)
            {
                ink.Shift(marks[k], marks[k + 1], column + CentredOffset(widths[col], cellWidths[k]), baseline);
                column += widths[col] + gap;
            }

            baseline += bottoms[r];
        }

        x += gridWidth;
        var rightFrom = ink.Mark;
        var rightSize = Delimiter(ink, style, right, height, depth);
        ink.Shift(rightFrom, x, 0);
        x += rightSize.Width;
        extentTop = Math.Max(extentTop, Math.Max(leftSize.Height, rightSize.Height));
        extentBottom = Math.Max(extentBottom, Math.Max(leftSize.Depth, rightSize.Depth));
        return new MathExtent(x, extentTop, extentBottom);
    }

    /// <summary>A delimiter grown to the matrix, at the origin; nothing for an empty one.</summary>
    private static MathExtent Delimiter(MathInk ink, in MathStyle style, string glyph, double height, double depth) =>
        glyph.Length == 0 ? default : SizedGlyph.Lay(ink, style, glyph, height, depth);

    /// <inheritdoc />
    internal override bool IsCharNode => false;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => writer.Matrix(environment, rows);
}
