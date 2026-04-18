// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Mat"/> 2-D matrix record struct behavior.</summary>
public class MatTests
{
    // ---- Shape ----------------------------------------------------------------

    [Fact]
    public void Shape_ReportsCorrectDimensions()
    {
        var m = Mat.FromRows([[1, 2, 3], [4, 5, 6]]);
        Assert.Equal((2, 3), m.Shape);
        Assert.Equal(2, m.Rows);
        Assert.Equal(3, m.Cols);
    }

    [Fact]
    public void Indexer_ReturnsCorrectElement()
    {
        var m = Mat.FromRows([[1, 2], [3, 4]]);
        Assert.Equal(1.0, m[0, 0]);
        Assert.Equal(2.0, m[0, 1]);
        Assert.Equal(3.0, m[1, 0]);
        Assert.Equal(4.0, m[1, 1]);
    }

    // ---- Factory --------------------------------------------------------------

    [Fact]
    public void Identity_DiagonalIsOneOffDiagonalIsZero()
    {
        var m = Mat.Identity(3);
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                Assert.Equal(r == c ? 1.0 : 0.0, m[r, c]);
    }

    [Fact]
    public void FromRows_StoresCorrectValues()
    {
        var m = Mat.FromRows([[1, 2], [3, 4], [5, 6]]);
        Assert.Equal(3, m.Rows);
        Assert.Equal(2, m.Cols);
        Assert.Equal(5.0, m[2, 0]);
    }

    // ---- Row / Col slices -----------------------------------------------------

    [Fact]
    public void Row_ReturnsCorrectVec()
    {
        var m = Mat.FromRows([[1, 2, 3], [4, 5, 6]]);
        Vec row = m.Row(1);
        Assert.Equal([4.0, 5.0, 6.0], row.Data);
    }

    [Fact]
    public void Col_ReturnsCorrectVec()
    {
        var m = Mat.FromRows([[1, 2], [3, 4], [5, 6]]);
        Vec col = m.Col(1);
        Assert.Equal([2.0, 4.0, 6.0], col.Data);
    }

    [Fact]
    public void RowSpan_ReturnsCorrectSpan()
    {
        var m = Mat.FromRows([[10, 20, 30], [40, 50, 60]]);
        Span<double> span = m.RowSpan(1);
        Assert.Equal(3, span.Length);
        Assert.Equal(40.0, span[0]);
        Assert.Equal(60.0, span[2]);
    }

    // ---- Transpose ------------------------------------------------------------

    [Fact]
    public void Transpose_2x3_Returns3x2()
    {
        var m = Mat.FromRows([[1, 2, 3], [4, 5, 6]]);
        Mat t = m.T;
        Assert.Equal((3, 2), t.Shape);
        Assert.Equal(1.0, t[0, 0]);
        Assert.Equal(4.0, t[0, 1]);
        Assert.Equal(2.0, t[1, 0]);
        Assert.Equal(5.0, t[1, 1]);
        Assert.Equal(3.0, t[2, 0]);
        Assert.Equal(6.0, t[2, 1]);
    }

    // ---- Operators ------------------------------------------------------------

    [Fact]
    public void Multiply_2x2_ByIdentity_ReturnsSame()
    {
        var m = Mat.FromRows([[1, 2], [3, 4]]);
        Mat result = m * Mat.Identity(2);
        Assert.Equal(1.0, result[0, 0]);
        Assert.Equal(2.0, result[0, 1]);
        Assert.Equal(3.0, result[1, 0]);
        Assert.Equal(4.0, result[1, 1]);
    }

    [Fact]
    public void Multiply_2x3_By3x2_CorrectResult()
    {
        var a = Mat.FromRows([[1, 2, 3], [4, 5, 6]]);
        var b = Mat.FromRows([[7, 8], [9, 10], [11, 12]]);
        Mat c = a * b;
        Assert.Equal((2, 2), c.Shape);
        Assert.Equal(58.0,  c[0, 0]);   // 1*7+2*9+3*11
        Assert.Equal(64.0,  c[0, 1]);   // 1*8+2*10+3*12
        Assert.Equal(139.0, c[1, 0]);   // 4*7+5*9+6*11
        Assert.Equal(154.0, c[1, 1]);   // 4*8+5*10+6*12
    }

    [Fact]
    public void Add_TwoMats_ElementWise()
    {
        var a = Mat.FromRows([[1, 2], [3, 4]]);
        var b = Mat.FromRows([[5, 6], [7, 8]]);
        Mat c = a + b;
        Assert.Equal(6.0,  c[0, 0]);
        Assert.Equal(8.0,  c[0, 1]);
        Assert.Equal(10.0, c[1, 0]);
        Assert.Equal(12.0, c[1, 1]);
    }

    [Fact]
    public void Subtract_TwoMats_ElementWise()
    {
        var a = Mat.FromRows([[5, 6], [7, 8]]);
        var b = Mat.FromRows([[1, 2], [3, 4]]);
        Mat c = a - b;
        Assert.Equal(4.0, c[0, 0]);
        Assert.Equal(4.0, c[0, 1]);
        Assert.Equal(4.0, c[1, 0]);
        Assert.Equal(4.0, c[1, 1]);
    }

    [Fact]
    public void ScalarMultiply_ScalesAllElements()
    {
        var m = Mat.FromRows([[1, 2], [3, 4]]);
        Mat r = m * 3.0;
        Assert.Equal(3.0, r[0, 0]);
        Assert.Equal(6.0, r[0, 1]);
        Assert.Equal(9.0, r[1, 0]);
        Assert.Equal(12.0, r[1, 1]);
    }

    [Fact]
    public void Transpose_TwiceIsIdentity()
    {
        var m = Mat.FromRows([[1, 2, 3], [4, 5, 6]]);
        Mat tt = m.T.T;
        Assert.Equal(m.Shape, tt.Shape);
        for (int r = 0; r < m.Rows; r++)
            for (int c = 0; c < m.Cols; c++)
                Assert.Equal(m[r, c], tt[r, c]);
    }

    /// <summary>Multiplying with mismatched inner dimensions must throw with an
    /// informative shape message. Covers the dimension-mismatch arm of operator*.</summary>
    [Fact]
    public void Multiply_ShapeMismatch_Throws()
    {
        var a = Mat.FromRows([[1, 2, 3]]);    // 1x3
        var b = Mat.FromRows([[1, 2], [3, 4]]); // 2x2 — a.Cols (3) != b.Rows (2)
        var ex = Assert.Throws<ArgumentException>(() => { _ = a * b; });
        Assert.Contains("1×3", ex.Message);
        Assert.Contains("2×2", ex.Message);
    }

    /// <summary>Element-wise + and - must throw when shapes differ — covers the
    /// AssertSameShape mismatch arm.</summary>
    [Theory]
    [InlineData("add")]
    [InlineData("sub")]
    public void AddOrSubtract_ShapeMismatch_Throws(string op)
    {
        var a = Mat.FromRows([[1, 2]]);      // 1x2
        var b = Mat.FromRows([[1, 2, 3]]);   // 1x3
        Assert.Throws<ArgumentException>(() =>
        {
            _ = op == "add" ? a + b : a - b;
        });
    }
}
