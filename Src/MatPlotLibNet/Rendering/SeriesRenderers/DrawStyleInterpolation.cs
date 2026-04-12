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

        var newX = new List<double>(x.Length * 2);
        var newY = new List<double>(y.Length * 2);

        switch (style)
        {
            case DrawStyle.StepsPre:
                for (int i = 0; i < x.Length; i++)
                {
                    if (i > 0) { newX.Add(x[i]); newY.Add(y[i - 1]); }
                    newX.Add(x[i]); newY.Add(y[i]);
                }
                break;

            case DrawStyle.StepsPost:
                for (int i = 0; i < x.Length; i++)
                {
                    newX.Add(x[i]); newY.Add(y[i]);
                    if (i < x.Length - 1) { newX.Add(x[i + 1]); newY.Add(y[i]); }
                }
                break;

            case DrawStyle.StepsMid:
                for (int i = 0; i < x.Length; i++)
                {
                    if (i > 0)
                    {
                        var midX = (x[i - 1] + x[i]) / 2;
                        newX.Add(midX); newY.Add(y[i - 1]);
                        newX.Add(midX); newY.Add(y[i]);
                    }
                    newX.Add(x[i]); newY.Add(y[i]);
                }
                break;
        }

        return new([.. newX], [.. newY]);
    }
}
