// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>Nearest-neighbor resampling: each output cell copies the value of the closest input cell.</summary>
public sealed class NearestInterpolation : IInterpolationEngine
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly NearestInterpolation Instance = new();

    private NearestInterpolation() { }

    /// <inheritdoc />
    public string Name => "nearest";

    /// <inheritdoc />
    public double[,] Resample(double[,] data, int targetRows, int targetCols)
    {
        int srcRows = data.GetLength(0);
        int srcCols = data.GetLength(1);
        var result = new double[targetRows, targetCols];

        for (int r = 0; r < targetRows; r++)
        {
            int srcR = (int)Math.Round((double)r * (srcRows - 1) / Math.Max(targetRows - 1, 1));
            srcR = Math.Clamp(srcR, 0, srcRows - 1);
            for (int c = 0; c < targetCols; c++)
            {
                int srcC = (int)Math.Round((double)c * (srcCols - 1) / Math.Max(targetCols - 1, 1));
                srcC = Math.Clamp(srcC, 0, srcCols - 1);
                result[r, c] = data[srcR, srcC];
            }
        }

        return result;
    }
}
