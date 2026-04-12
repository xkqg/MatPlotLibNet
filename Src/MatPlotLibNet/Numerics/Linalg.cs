// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Eigenvalue/eigenvector result for a real symmetric matrix (ascending order).</summary>
/// <param name="Eigenvalues">Eigenvalues sorted ascending.</param>
/// <param name="Eigenvectors">Column-major eigenvectors: column i corresponds to eigenvalue i.</param>
public readonly record struct EighResult(Vec Eigenvalues, Mat Eigenvectors);

/// <summary>Thin SVD result: A ≈ U · diag(S) · Vt.</summary>
/// <param name="U">Left singular vectors (m × k, orthonormal columns).</param>
/// <param name="S">Singular values in descending order (length k = min(m,n)).</param>
/// <param name="Vt">Right singular vectors transposed (k × n, orthonormal rows).</param>
public readonly record struct SvdResult(Mat U, Vec S, Mat Vt);

/// <summary>Linear algebra operations for <see cref="Mat"/> and <see cref="Vec"/>.</summary>
public static class Linalg
{
    // -------------------------------------------------------------------------
    // Solve  Ax = b  via LU with partial pivoting (Doolittle)
    // -------------------------------------------------------------------------

    /// <summary>Solves Ax = b via LU decomposition with partial pivoting.</summary>
    /// <param name="a">Square coefficient matrix A (n × n).</param>
    /// <param name="b">Right-hand side vector of length n.</param>
    /// <returns>Solution vector x such that Ax = b.</returns>
    public static Vec Solve(Mat a, Vec b)
    {
        int n = a.Rows;
        double[,] lu = (double[,])a.Data.Clone();
        int[] piv = Enumerable.Range(0, n).ToArray();
        LuDecompose(lu, piv, n);
        return new Vec(LuSolve(lu, piv, b.Data, n));
    }

    /// <summary>Computes the matrix inverse via LU.</summary>
    /// <param name="a">Square invertible matrix (n × n).</param>
    /// <returns>The inverse of <paramref name="a"/>.</returns>
    public static Mat Inv(Mat a)
    {
        int n = a.Rows;
        double[,] lu = (double[,])a.Data.Clone();
        int[] piv = Enumerable.Range(0, n).ToArray();
        LuDecompose(lu, piv, n);
        var inv = new double[n, n];
        var e = new double[n];
        for (int col = 0; col < n; col++)
        {
            Array.Clear(e);
            e[col] = 1.0;
            double[] col_vec = LuSolve(lu, piv, e, n);
            for (int row = 0; row < n; row++)
                inv[row, col] = col_vec[row];
        }
        return new Mat(inv);
    }

    /// <summary>Computes the determinant via LU diagonal product with pivot sign.</summary>
    /// <param name="a">Square matrix (n × n).</param>
    /// <returns>Determinant of <paramref name="a"/>; 0 for singular or near-singular matrices.</returns>
    public static double Det(Mat a)
    {
        int n = a.Rows;
        double[,] lu = (double[,])a.Data.Clone();
        int[] piv = Enumerable.Range(0, n).ToArray();
        int swaps = LuDecompose(lu, piv, n);
        double det = swaps % 2 == 0 ? 1.0 : -1.0;
        for (int i = 0; i < n; i++)
            det *= lu[i, i];
        return det;
    }

    // -------------------------------------------------------------------------
    // Eigh  — Jacobi iteration for real symmetric matrices
    // -------------------------------------------------------------------------

    /// <summary>Eigendecomposition for real symmetric matrices via Jacobi sweeps.
    /// Returns eigenvalues sorted ascending with corresponding eigenvectors.</summary>
    /// <param name="a">Real symmetric square matrix (n × n). Only the upper triangle is used.</param>
    /// <returns><see cref="EighResult"/> with eigenvalues ascending and matching eigenvectors.</returns>
    public static EighResult Eigh(Mat a)
    {
        int n = a.Rows;
        double[,] A = (double[,])a.Data.Clone();
        double[,] V = Mat.Identity(n).Data;   // accumulate rotations

        const int MaxSweeps = 100;
        for (int sweep = 0; sweep < MaxSweeps; sweep++)
        {
            double offDiag = 0;
            for (int p = 0; p < n - 1; p++)
                for (int q = p + 1; q < n; q++)
                    offDiag += A[p, q] * A[p, q];

            if (offDiag < 1e-20) break;

            for (int p = 0; p < n - 1; p++)
            {
                for (int q = p + 1; q < n; q++)
                {
                    double apq = A[p, q];
                    if (Math.Abs(apq) < 1e-15) continue;

                    double tau = (A[q, q] - A[p, p]) / (2.0 * apq);
                    // When tau=0, Math.Sign returns 0 → degenerate; use +1 (45° rotation).
                    double t = (tau >= 0 ? 1.0 : -1.0) / (Math.Abs(tau) + Math.Sqrt(1.0 + tau * tau));
                    double c = 1.0 / Math.Sqrt(1.0 + t * t);
                    double s = t * c;

                    JacobiRotate(A, V, p, q, c, s, n);
                }
            }
        }

        // Extract eigenvalues and sort ascending
        double[] eigvals = new double[n];
        for (int i = 0; i < n; i++) eigvals[i] = A[i, i];

        int[] idx = Enumerable.Range(0, n).OrderBy(i => eigvals[i]).ToArray();
        double[] sortedVals = idx.Select(i => eigvals[i]).ToArray();

        double[,] sortedVecs = new double[n, n];
        for (int col = 0; col < n; col++)
            for (int row = 0; row < n; row++)
                sortedVecs[row, col] = V[row, idx[col]];

        return new EighResult(new Vec(sortedVals), new Mat(sortedVecs));
    }

    // -------------------------------------------------------------------------
    // Svd  — one-sided Jacobi SVD
    // -------------------------------------------------------------------------

    /// <summary>Thin SVD via one-sided Jacobi: A ≈ U · diag(S) · Vt.</summary>
    /// <param name="a">Input matrix (m × n).</param>
    /// <returns><see cref="SvdResult"/> with U (m × k), S (length k), Vt (k × n) where k = min(m, n).</returns>
    public static SvdResult Svd(Mat a)
    {
        int m = a.Rows, n = a.Cols;
        int k = Math.Min(m, n);

        // Work on At*A to get V; then U = A*V / sigma
        // For thin SVD use eigendecomposition of At*A
        Mat At = a.T;
        Mat AtA = At * a;     // n×n symmetric

        EighResult eigh = Eigh(AtA);

        // Singular values = sqrt of eigenvalues (clamp negatives from round-off)
        double[] svals = new double[k];
        for (int i = 0; i < k; i++)
            svals[i] = Math.Sqrt(Math.Max(0.0, eigh.Eigenvalues[n - 1 - i]));  // descending

        // V columns = eigenvectors of AtA (last n eigenvectors = largest eigenvalues)
        double[,] Vdata = new double[n, k];
        for (int col = 0; col < k; col++)
            for (int row = 0; row < n; row++)
                Vdata[row, col] = eigh.Eigenvectors[row, n - 1 - col];

        Mat V = new Mat(Vdata);
        Mat Vt = V.T;

        // U = A * V / sigma  (for non-zero singular values)
        double[,] Udata = new double[m, k];
        Mat AV = a * V;
        for (int col = 0; col < k; col++)
        {
            double s = svals[col];
            for (int row = 0; row < m; row++)
                Udata[row, col] = s > 1e-14 ? AV[row, col] / s : 0.0;
        }

        return new SvdResult(new Mat(Udata), new Vec(svals), Vt);
    }

    // -------------------------------------------------------------------------
    // LU helpers
    // -------------------------------------------------------------------------

    /// <summary>In-place Doolittle LU with partial pivoting.
    /// Returns the number of row swaps (for determinant sign).</summary>
    private static int LuDecompose(double[,] lu, int[] piv, int n)
    {
        int swaps = 0;
        for (int k = 0; k < n; k++)
        {
            // Find pivot
            int maxRow = k;
            double maxVal = Math.Abs(lu[k, k]);
            for (int i = k + 1; i < n; i++)
            {
                if (Math.Abs(lu[i, k]) > maxVal)
                {
                    maxVal = Math.Abs(lu[i, k]);
                    maxRow = i;
                }
            }

            if (maxRow != k)
            {
                for (int j = 0; j < n; j++)
                    (lu[k, j], lu[maxRow, j]) = (lu[maxRow, j], lu[k, j]);
                (piv[k], piv[maxRow]) = (piv[maxRow], piv[k]);
                swaps++;
            }

            if (Math.Abs(lu[k, k]) < 1e-15) continue;  // singular / near-singular

            for (int i = k + 1; i < n; i++)
            {
                lu[i, k] /= lu[k, k];
                for (int j = k + 1; j < n; j++)
                    lu[i, j] -= lu[i, k] * lu[k, j];
            }
        }
        return swaps;
    }

    private static double[] LuSolve(double[,] lu, int[] piv, double[] b, int n)
    {
        // Apply row permutation
        double[] x = new double[n];
        for (int i = 0; i < n; i++) x[i] = b[piv[i]];

        // Forward substitution (L)
        for (int i = 1; i < n; i++)
            for (int j = 0; j < i; j++)
                x[i] -= lu[i, j] * x[j];

        // Back substitution (U)
        for (int i = n - 1; i >= 0; i--)
        {
            for (int j = i + 1; j < n; j++)
                x[i] -= lu[i, j] * x[j];
            x[i] /= lu[i, i];
        }
        return x;
    }

    // -------------------------------------------------------------------------
    // Jacobi rotation helper
    // -------------------------------------------------------------------------

    private static void JacobiRotate(double[,] A, double[,] V, int p, int q, double c, double s, int n)
    {
        // Update A
        double app = A[p, p], aqq = A[q, q], apq = A[p, q];
        A[p, p] = c * c * app - 2 * s * c * apq + s * s * aqq;
        A[q, q] = s * s * app + 2 * s * c * apq + c * c * aqq;
        A[p, q] = 0.0;
        A[q, p] = 0.0;

        for (int r = 0; r < n; r++)
        {
            if (r != p && r != q)
            {
                double arp = A[r, p], arq = A[r, q];
                A[r, p] = c * arp - s * arq;
                A[p, r] = A[r, p];
                A[r, q] = s * arp + c * arq;
                A[q, r] = A[r, q];
            }

            double vrp = V[r, p], vrq = V[r, q];
            V[r, p] = c * vrp - s * vrq;
            V[r, q] = s * vrp + c * vrq;
        }
    }
}
