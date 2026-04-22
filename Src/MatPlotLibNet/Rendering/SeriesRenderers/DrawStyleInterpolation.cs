// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Shared step-interpolation utility used by <see cref="LineSeriesRenderer"/> and
/// <see cref="AreaSeriesRenderer"/>.</summary>
internal static class DrawStyleInterpolation
{
    /// <summary>Expands <paramref name="x"/>/<paramref name="y"/> according to
    /// <paramref name="style"/>. Returns the input unchanged when style is
    /// <see langword="null"/>, <see cref="DrawStyle.Default"/>, or the data has fewer than 2 points.</summary>
    /// <param name="x">Input X values.</param>
    /// <param name="y">Input Y values, parallel to <paramref name="x"/>.</param>
    /// <param name="style">Step interpolation style, or <see langword="null"/> for no interpolation.</param>
    internal static XYData Apply(double[] x, double[] y, DrawStyle? style)
    {
        if (style is null or DrawStyle.Default || x.Length < 2) return new(x, y);

        int n = x.Length;
        int outLen = style == DrawStyle.StepsMid ? 3 * n - 2 : 2 * n - 1;
        var newX = new double[outLen];
        var newY = new double[outLen];
        int k = 0;

        switch (style)
        {
            case DrawStyle.StepsPre:
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) { newX[k] = x[i]; newY[k++] = y[i - 1]; }
                    newX[k] = x[i]; newY[k++] = y[i];
                }
                break;

            case DrawStyle.StepsPost:
                for (int i = 0; i < n; i++)
                {
                    newX[k] = x[i]; newY[k++] = y[i];
                    if (i < n - 1) { newX[k] = x[i + 1]; newY[k++] = y[i]; }
                }
                break;

            case DrawStyle.StepsMid:
                for (int i = 0; i < n; i++)
                {
                    if (i > 0)
                    {
                        double midX = (x[i - 1] + x[i]) / 2;
                        newX[k] = midX; newY[k++] = y[i - 1];
                        newX[k] = midX; newY[k++] = y[i];
                    }
                    newX[k] = x[i]; newY[k++] = y[i];
                }
                break;
        }

        return new(newX, newY);
    }
}
