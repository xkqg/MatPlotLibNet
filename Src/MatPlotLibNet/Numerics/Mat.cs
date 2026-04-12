// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics.Tensors;
using System.Runtime.InteropServices;

namespace MatPlotLibNet.Numerics;

/// <summary>Immutable 2-D matrix backed by a contiguous row-major <c>double[,]</c>.
/// Element-wise operators use <see cref="TensorPrimitives"/> SIMD acceleration;
/// matrix multiply uses row-dot-column via <c>TensorPrimitives.Dot</c>.</summary>
/// <param name="Data">The underlying row-major data array.</param>
public readonly record struct Mat(double[,] Data)
{
    // -------------------------------------------------------------------------
    // Shape
    // -------------------------------------------------------------------------

    /// <summary>Number of rows.</summary>
    public int Rows => Data.GetLength(0);

    /// <summary>Number of columns.</summary>
    public int Cols => Data.GetLength(1);

    /// <summary>Returns <c>(Rows, Cols)</c>.</summary>
    public (int Rows, int Cols) Shape => (Rows, Cols);

    // -------------------------------------------------------------------------
    // Accessors
    // -------------------------------------------------------------------------

    /// <summary>Gets the element at <paramref name="row"/>, <paramref name="col"/>.</summary>
    public double this[int row, int col] => Data[row, col];

    /// <summary>Returns a mutable span over an entire row — zero allocation, CLR layout guaranteed.</summary>
    public Span<double> RowSpan(int row) =>
        MemoryMarshal.CreateSpan(ref Data[row, 0], Cols);

    /// <summary>Returns a copy of row <paramref name="row"/> as a <see cref="Vec"/>.</summary>
    public Vec Row(int row)
    {
        var r = new double[Cols];
        RowSpan(row).CopyTo(r);
        return new Vec(r);
    }

    /// <summary>Returns a copy of column <paramref name="col"/> as a <see cref="Vec"/>.</summary>
    public Vec Col(int col)
    {
        var c = new double[Rows];
        for (int r = 0; r < Rows; r++)
            c[r] = Data[r, col];
        return new Vec(c);
    }

    // -------------------------------------------------------------------------
    // Transpose
    // -------------------------------------------------------------------------

    /// <summary>Returns the transpose — a new <see cref="Mat"/> with rows and columns swapped.</summary>
    public Mat T
    {
        get
        {
            var dst = new double[Cols, Rows];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    dst[c, r] = Data[r, c];
            return new Mat(dst);
        }
    }

    // -------------------------------------------------------------------------
    // Operators
    // -------------------------------------------------------------------------

    /// <summary>Matrix multiplication: <c>A @ B</c>. Uses <c>TensorPrimitives.Dot</c> per output cell.</summary>
    public static Mat operator *(Mat a, Mat b)
    {
        if (a.Cols != b.Rows)
            throw new ArgumentException($"Cannot multiply {a.Rows}×{a.Cols} by {b.Rows}×{b.Cols}.");
        var dst = new double[a.Rows, b.Cols];
        // Transpose B once so inner loop is row-dot-row (cache-friendly)
        Mat bt = b.T;
        for (int r = 0; r < a.Rows; r++)
        {
            ReadOnlySpan<double> rowA = a.RowSpan(r);
            for (int c = 0; c < b.Cols; c++)
                dst[r, c] = TensorPrimitives.Dot<double>(rowA, bt.RowSpan(c));
        }
        return new Mat(dst);
    }

    /// <summary>Element-wise addition.</summary>
    public static Mat operator +(Mat a, Mat b)
    {
        AssertSameShape(a, b);
        var dst = new double[a.Rows, a.Cols];
        TensorPrimitives.Add(AsSpan(a), AsSpan(b), AsSpanMut(dst));
        return new Mat(dst);
    }

    /// <summary>Element-wise subtraction.</summary>
    public static Mat operator -(Mat a, Mat b)
    {
        AssertSameShape(a, b);
        var dst = new double[a.Rows, a.Cols];
        TensorPrimitives.Subtract(AsSpan(a), AsSpan(b), AsSpanMut(dst));
        return new Mat(dst);
    }

    /// <summary>Scalar multiplication: every element × <paramref name="scalar"/>.</summary>
    public static Mat operator *(Mat a, double scalar)
    {
        var dst = new double[a.Rows, a.Cols];
        TensorPrimitives.Multiply(AsSpan(a), scalar, AsSpanMut(dst));
        return new Mat(dst);
    }

    // -------------------------------------------------------------------------
    // Factories
    // -------------------------------------------------------------------------

    /// <summary>Creates a <see cref="Mat"/> from a jagged array (rows × cols).</summary>
    public static Mat FromRows(double[][] rows)
    {
        int r = rows.Length;
        int c = rows[0].Length;
        var dst = new double[r, c];
        for (int i = 0; i < r; i++)
            rows[i].AsSpan().CopyTo(MemoryMarshal.CreateSpan(ref dst[i, 0], c));
        return new Mat(dst);
    }

    /// <summary>Creates an <paramref name="n"/>×<paramref name="n"/> identity matrix.</summary>
    public static Mat Identity(int n)
    {
        var dst = new double[n, n];
        for (int i = 0; i < n; i++)
            dst[i, i] = 1.0;
        return new Mat(dst);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    /// <summary>Flattens the 2-D array to a contiguous read-only span (CLR layout guarantee).</summary>
    internal static ReadOnlySpan<double> AsSpan(Mat m) =>
        MemoryMarshal.CreateReadOnlySpan(ref m.Data[0, 0], m.Rows * m.Cols);

    internal static Span<double> AsSpanMut(double[,] d) =>
        MemoryMarshal.CreateSpan(ref d[0, 0], d.GetLength(0) * d.GetLength(1));

    private static void AssertSameShape(Mat a, Mat b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols)
            throw new ArgumentException($"Shape mismatch: {a.Rows}×{a.Cols} vs {b.Rows}×{b.Cols}.");
    }
}
