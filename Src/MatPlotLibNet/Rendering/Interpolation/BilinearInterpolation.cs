// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>Bilinear resampling: each output value is a weighted average of the 2×2 neighborhood in the source.</summary>
public sealed class BilinearInterpolation : IInterpolationEngine, IColourResampler
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly BilinearInterpolation Instance = new();

    private BilinearInterpolation() { }

    /// <inheritdoc />
    public string Name => "bilinear";

    /// <inheritdoc />
    public double[,] Resample(double[,] data, int targetRows, int targetCols)
    {
        var result = new double[targetRows, targetCols];
        ResampleInto<double>(data.AsRows(), data.GetLength(0), data.GetLength(1), result.AsRows(), targetRows, targetCols);
        return result;
    }

    /// <inheritdoc />
    void IColourResampler.ResampleColours(ReadOnlySpan<ColourSample> source, int rows, int columns, Span<ColourSample> target,
        int targetRows, int targetColumns) =>
        ResampleInto(source, rows, columns, target, targetRows, targetColumns);

    /// <summary>The one resampling of this engine, of a value or of a colour: each output cell the blend of the four input cells around
    /// the point it maps to.</summary>
    private static void ResampleInto<T>(ReadOnlySpan<T> data, int srcRows, int srcCols, Span<T> result, int targetRows, int targetCols)
        where T : struct, IAdditionOperators<T, T, T>, IMultiplyOperators<T, double, T>
    {
        double rowScale = (double)(srcRows - 1) / Math.Max(targetRows - 1, 1);
        double colScale = (double)(srcCols - 1) / Math.Max(targetCols - 1, 1);

        for (int r = 0; r < targetRows; r++)
        {
            double sr = r * rowScale;
            int r0 = Math.Clamp((int)sr, 0, srcRows - 1);
            int r1 = Math.Clamp(r0 + 1, 0, srcRows - 1);
            double wr = sr - r0;

            for (int c = 0; c < targetCols; c++)
            {
                double sc = c * colScale;
                int c0 = Math.Clamp((int)sc, 0, srcCols - 1);
                int c1 = Math.Clamp(c0 + 1, 0, srcCols - 1);
                double wc = sc - c0;

                var top    = data[r0 * srcCols + c0] * (1 - wc) + data[r0 * srcCols + c1] * wc;
                var bottom = data[r1 * srcCols + c0] * (1 - wc) + data[r1 * srcCols + c1] * wc;
                result[r * targetCols + c] = top * (1 - wr) + bottom * wr;
            }
        }
    }
}