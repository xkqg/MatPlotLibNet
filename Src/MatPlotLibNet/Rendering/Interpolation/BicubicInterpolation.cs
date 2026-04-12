// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>Bicubic (Catmull-Rom) resampling: each output value uses a 4×4 neighborhood for smooth interpolation.
/// Output is clamped to [min, max] of the input data to suppress ringing artifacts.</summary>
public sealed class BicubicInterpolation : IInterpolationEngine
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly BicubicInterpolation Instance = new();

    private BicubicInterpolation() { }

    /// <inheritdoc />
    public string Name => "bicubic";

    /// <inheritdoc />
    public double[,] Resample(double[,] data, int targetRows, int targetCols)
    {
        int srcRows = data.GetLength(0);
        int srcCols = data.GetLength(1);
        var result = new double[targetRows, targetCols];

        // Compute data range for clamping (prevents ringing artifacts)
        double dataMin = double.MaxValue, dataMax = double.MinValue;
        foreach (double v in data)
        {
            if (v < dataMin) dataMin = v;
            if (v > dataMax) dataMax = v;
        }

        double rowScale = (double)(srcRows - 1) / Math.Max(targetRows - 1, 1);
        double colScale = (double)(srcCols - 1) / Math.Max(targetCols - 1, 1);

        for (int r = 0; r < targetRows; r++)
        {
            double sr = r * rowScale;
            int rBase = (int)sr;
            double wr = sr - rBase;

            for (int c = 0; c < targetCols; c++)
            {
                double sc = c * colScale;
                int cBase = (int)sc;
                double wc = sc - cBase;

                // Catmull-Rom: 4×4 neighborhood
                double value = 0;
                for (int m = -1; m <= 2; m++)
                {
                    double rowWeight = CatmullRom(m - wr);
                    for (int n = -1; n <= 2; n++)
                    {
                        double colWeight = CatmullRom(n - wc);
                        int ri = Math.Clamp(rBase + m, 0, srcRows - 1);
                        int ci = Math.Clamp(cBase + n, 0, srcCols - 1);
                        value += rowWeight * colWeight * data[ri, ci];
                    }
                }

                result[r, c] = Math.Clamp(value, dataMin, dataMax);
            }
        }

        return result;
    }

    /// <summary>Catmull-Rom kernel: k(t) for 1D weight at distance t.</summary>
    private static double CatmullRom(double t)
    {
        t = Math.Abs(t);
        if (t <= 1) return 1.5 * t * t * t - 2.5 * t * t + 1;
        if (t <= 2) return -0.5 * t * t * t + 2.5 * t * t - 4 * t + 2;
        return 0;
    }
}
