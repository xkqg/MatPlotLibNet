// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>Bilinear resampling: each output value is a weighted average of the 2×2 neighborhood in the source.</summary>
public sealed class BilinearInterpolation : IInterpolationEngine
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly BilinearInterpolation Instance = new();

    private BilinearInterpolation() { }

    /// <inheritdoc />
    public string Name => "bilinear";

    /// <inheritdoc />
    public double[,] Resample(double[,] data, int targetRows, int targetCols)
    {
        int srcRows = data.GetLength(0);
        int srcCols = data.GetLength(1);
        var result = new double[targetRows, targetCols];

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

                double top    = data[r0, c0] * (1 - wc) + data[r0, c1] * wc;
                double bottom = data[r1, c0] * (1 - wc) + data[r1, c1] * wc;
                result[r, c]  = top * (1 - wr) + bottom * wr;
            }
        }

        return result;
    }
}
