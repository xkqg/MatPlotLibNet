// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Linalg"/> — Solve, Inv, Det, Eigh, Svd.</summary>
public class LinalgTests
{
    private const double Tol = 1e-9;

    // ---- Solve ---------------------------------------------------------------

    [Fact]
    public void Solve_2x2_KnownSystem()
    {
        // 2x + y = 5, x + 3y = 10  → x=1, y=3
        var A = Mat.FromRows([[2, 1], [1, 3]]);
        Vec b = new double[] { 5, 10 };
        Vec x = Linalg.Solve(A, b);
        Assert.Equal(2, x.Length);
        Assert.Equal(1.0, x[0], Tol);
        Assert.Equal(3.0, x[1], Tol);
    }

    [Fact]
    public void Solve_3x3_KnownSystem()
    {
        var A = Mat.FromRows([[2, 1, -1], [-3, -1, 2], [-2, 1, 2]]);
        Vec b = new double[] { 8, -11, -3 };
        Vec x = Linalg.Solve(A, b);
        Assert.Equal(2.0,  x[0], Tol);
        Assert.Equal(3.0,  x[1], Tol);
        Assert.Equal(-1.0, x[2], Tol);
    }

    // ---- Inv -----------------------------------------------------------------

    [Fact]
    public void Inv_2x2_KnownMatrix()
    {
        // inv([[4,7],[2,6]]) = [[0.6,-0.7],[-0.2,0.4]]
        var A = Mat.FromRows([[4, 7], [2, 6]]);
        Mat inv = Linalg.Inv(A);
        Assert.Equal(0.6,  inv[0, 0], Tol);
        Assert.Equal(-0.7, inv[0, 1], Tol);
        Assert.Equal(-0.2, inv[1, 0], Tol);
        Assert.Equal(0.4,  inv[1, 1], Tol);
    }

    [Fact]
    public void Inv_MultipliedByOriginal_GivesIdentity()
    {
        var A = Mat.FromRows([[1, 2], [3, 5]]);
        Mat inv = Linalg.Inv(A);
        Mat I = A * inv;
        Assert.Equal(1.0, I[0, 0], Tol);
        Assert.Equal(0.0, I[0, 1], Tol);
        Assert.Equal(0.0, I[1, 0], Tol);
        Assert.Equal(1.0, I[1, 1], Tol);
    }

    // ---- Det -----------------------------------------------------------------

    [Fact]
    public void Det_2x2_CorrectValue()
    {
        // det([[3,8],[4,6]]) = 18-32 = -14
        var A = Mat.FromRows([[3, 8], [4, 6]]);
        Assert.Equal(-14.0, Linalg.Det(A), Tol);
    }

    [Fact]
    public void Det_Identity_ReturnsOne()
    {
        Assert.Equal(1.0, Linalg.Det(Mat.Identity(3)), Tol);
    }

    [Fact]
    public void Det_SingularMatrix_ReturnsZero()
    {
        var A = Mat.FromRows([[1, 2], [2, 4]]);   // row2 = 2×row1
        Assert.Equal(0.0, Linalg.Det(A), 1e-9);
    }

    // ---- Eigh ----------------------------------------------------------------

    [Fact]
    public void Eigh_2x2Symmetric_CorrectEigenvalues()
    {
        // [[2,1],[1,2]] → eigenvalues 1 and 3
        var A = Mat.FromRows([[2, 1], [1, 2]]);
        EighResult r = Linalg.Eigh(A);
        double[] ev = r.Eigenvalues.Data;
        Array.Sort(ev);
        Assert.Equal(1.0, ev[0], 1e-6);
        Assert.Equal(3.0, ev[1], 1e-6);
    }

    [Fact]
    public void Eigh_Eigenvalues_AreSortedAscending()
    {
        var A = Mat.FromRows([[4, 2], [2, 3]]);
        EighResult r = Linalg.Eigh(A);
        Assert.True(r.Eigenvalues[0] <= r.Eigenvalues[1]);
    }

    [Fact]
    public void Eigh_AxEqualsLambdaX()
    {
        var A = Mat.FromRows([[3, 1], [1, 3]]);
        EighResult r = Linalg.Eigh(A);
        for (int i = 0; i < 2; i++)
        {
            Vec eigvec = r.Eigenvectors.Col(i);
            Vec Av = new double[] { A[0,0]*eigvec[0]+A[0,1]*eigvec[1], A[1,0]*eigvec[0]+A[1,1]*eigvec[1] };
            Vec lv = eigvec * r.Eigenvalues[i];
            Assert.Equal(lv[0], Av[0], 1e-6);
            Assert.Equal(lv[1], Av[1], 1e-6);
        }
    }

    // ---- Svd -----------------------------------------------------------------

    [Fact]
    public void Svd_SingularValues_AllPositive()
    {
        var A = Mat.FromRows([[1, 2], [3, 4], [5, 6]]);
        SvdResult r = Linalg.Svd(A);
        foreach (double s in r.S.Data)
            Assert.True(s >= 0.0);
    }

    [Fact]
    public void Svd_SingularValues_Descending()
    {
        var A = Mat.FromRows([[1, 2, 3], [4, 5, 6]]);
        SvdResult r = Linalg.Svd(A);
        for (int i = 0; i < r.S.Length - 1; i++)
            Assert.True(r.S[i] >= r.S[i + 1] - 1e-9);
    }

    [Fact]
    public void Svd_ReconstructsOriginalMatrix()
    {
        var A = Mat.FromRows([[1, 2], [3, 4]]);
        SvdResult r = Linalg.Svd(A);
        // A ≈ U * diag(S) * Vt
        int k = r.S.Length;
        var diagData = new double[k, k];
        for (int i = 0; i < k; i++) diagData[i, i] = r.S[i];
        Mat diag = new Mat(diagData);
        Mat recon = r.U * diag * r.Vt;
        for (int row = 0; row < A.Rows; row++)
            for (int col = 0; col < A.Cols; col++)
                Assert.Equal(A[row, col], recon[row, col], 1e-6);
    }
}

