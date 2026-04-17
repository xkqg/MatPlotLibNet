// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

public sealed class MatrixTests
{
    [Fact]
    public void Pmatrix_ParsesWithStartAndEnd()
    {
        var rt = MathTextParser.Parse(@"$\begin{pmatrix} a & b \\ c & d \end{pmatrix}$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.MatrixStart && s.Text == "pmatrix");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.MatrixEnd && s.Text == "pmatrix");
    }

    [Fact]
    public void Matrix_ContainsCells()
    {
        var rt = MathTextParser.Parse(@"$\begin{matrix} a & b \\ c & d \end{matrix}$");
        var cells = rt.Spans.Where(s => s.Kind == TextSpanKind.MatrixCell).ToList();
        Assert.Equal(4, cells.Count);
    }

    [Fact]
    public void Matrix_CellContent_Correct()
    {
        var rt = MathTextParser.Parse(@"$\begin{pmatrix} x & y \\ z & w \end{pmatrix}$");
        var cells = rt.Spans.Where(s => s.Kind == TextSpanKind.MatrixCell).Select(s => s.Text.Trim()).ToList();
        Assert.Contains("x", cells);
        Assert.Contains("y", cells);
        Assert.Contains("z", cells);
        Assert.Contains("w", cells);
    }

    [Fact]
    public void Matrix_HasRowSeparators()
    {
        var rt = MathTextParser.Parse(@"$\begin{bmatrix} 1 & 2 \\ 3 & 4 \end{bmatrix}$");
        var rowSeps = rt.Spans.Count(s => s.Kind == TextSpanKind.MatrixRowSeparator);
        Assert.True(rowSeps >= 2); // one per row
    }

    [Fact]
    public void Bmatrix_ParsesCorrectly()
    {
        var rt = MathTextParser.Parse(@"$\begin{bmatrix} a & b \end{bmatrix}$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.MatrixStart && s.Text == "bmatrix");
    }

    [Fact]
    public void Vmatrix_ParsesCorrectly()
    {
        var rt = MathTextParser.Parse(@"$\begin{vmatrix} a & b \\ c & d \end{vmatrix}$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.MatrixStart && s.Text == "vmatrix");
    }

    [Fact]
    public void Matrix_CellsHaveReducedScale()
    {
        var rt = MathTextParser.Parse(@"$\begin{matrix} x \end{matrix}$");
        var cell = rt.Spans.First(s => s.Kind == TextSpanKind.MatrixCell);
        Assert.True(cell.FontSizeScale < 1.0);
    }

    [Fact]
    public void ThreeByThree_Matrix_Has9Cells()
    {
        var rt = MathTextParser.Parse(@"$\begin{pmatrix} 1 & 2 & 3 \\ 4 & 5 & 6 \\ 7 & 8 & 9 \end{pmatrix}$");
        var cells = rt.Spans.Count(s => s.Kind == TextSpanKind.MatrixCell);
        Assert.Equal(9, cells);
    }
}
