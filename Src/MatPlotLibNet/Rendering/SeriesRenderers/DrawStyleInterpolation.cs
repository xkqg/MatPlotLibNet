// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>The step interpolation a line and an area share (<see cref="LineSeriesRenderer"/>,
/// <see cref="AreaSeriesRenderer"/>), as a member of the points it steps.</summary>
internal static class DrawStyleInterpolation
{
    extension(XYData data)
    {
        /// <summary>Expands these points according to <paramref name="style"/>. Returns them unchanged when style is
        /// <see langword="null"/>, <see cref="DrawStyle.Default"/>, or the data has fewer than 2 points.</summary>
        /// <param name="style">Step interpolation style, or <see langword="null"/> for no interpolation.</param>
        internal XYData SteppedBy(DrawStyle? style)
        {
            var (x, y) = data;
            if (style is null or DrawStyle.Default || x.Length < 2) return data;

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
                        if (i > 0) { newX[k] = x[i - 1]; newY[k++] = y[i]; }
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

    extension(DrawStyle style)
    {
        /// <summary>Where data point <paramref name="point"/> of <paramref name="count"/> stands in the points <c>SteppedBy</c> gives
        /// for this style: each data point is kept as it is, with the corners of the steps between it and the next (one for a pre or
        /// post step, two for a mid step), so a range of the data is a range of the stepped points.</summary>
        /// <param name="point">The index of the data point.</param>
        /// <param name="count">How many data points are stepped.</param>
        /// <returns>The index of the same point in the stepped points.</returns>
        internal int IndexOfPoint(int point, int count) => style switch
        {
            _ when count < 2 => point,
            DrawStyle.StepsPre or DrawStyle.StepsPost => 2 * point,
            DrawStyle.StepsMid => 3 * point,
            _ => point,
        };
    }
}
