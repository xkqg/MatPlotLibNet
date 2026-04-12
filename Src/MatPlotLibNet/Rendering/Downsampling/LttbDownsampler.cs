// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Downsampling;

/// <summary>
/// Implements the Largest-Triangle-Three-Buckets (LTTB) algorithm for time-series downsampling.
/// O(n) in practice — selects the point in each bucket that forms the largest triangle with the
/// previous selected point and the average of the next bucket, preserving visual peaks and troughs.
/// </summary>
/// <remarks>
/// The first and last data points are always preserved. Input X data must be sorted in ascending
/// order; unsorted data produces undefined output. When the input already contains fewer points
/// than <c>targetPoints</c>, the original arrays are returned unchanged without allocation.
/// </remarks>
public sealed class LttbDownsampler : IDownsampler
{
    /// <inheritdoc />
    public XYData Downsample(double[] x, double[] y, int targetPoints)
    {
        int n = x.Length;
        if (n == 0) return new([], []);
        if (n <= targetPoints) return new(x, y);

        var sampledX = new double[targetPoints];
        var sampledY = new double[targetPoints];

        // Always keep first and last
        sampledX[0] = x[0];
        sampledY[0] = y[0];
        sampledX[targetPoints - 1] = x[n - 1];
        sampledY[targetPoints - 1] = y[n - 1];

        // Bucket the middle portion (indices 1 .. n-2) into (targetPoints - 2) buckets
        int bucketCount = targetPoints - 2;
        double bucketSize = (double)(n - 2) / bucketCount;

        int prevSelected = 0;

        for (int b = 0; b < bucketCount; b++)
        {
            // Current bucket range
            int start = (int)Math.Floor((b + 0) * bucketSize) + 1;
            int end   = (int)Math.Floor((b + 1) * bucketSize) + 1;
            end = Math.Min(end, n - 1);

            // Next bucket average (for the triangle reference point)
            int nextStart = end;
            int nextEnd   = (int)Math.Floor((b + 2) * bucketSize) + 1;
            nextEnd = Math.Min(nextEnd, n - 1);

            double avgX = 0, avgY = 0;
            int nextLen = nextEnd - nextStart;
            if (nextLen > 0)
            {
                for (int i = nextStart; i < nextEnd; i++) { avgX += x[i]; avgY += y[i]; }
                avgX /= nextLen;
                avgY /= nextLen;
            }
            else
            {
                avgX = x[n - 1];
                avgY = y[n - 1];
            }

            // Pick the point in current bucket that forms the largest triangle
            double maxArea = -1;
            int selected = start;
            double px = x[prevSelected], py = y[prevSelected];

            for (int i = start; i < end; i++)
            {
                // Triangle area = 0.5 * |det| of (prev→current, prev→avg)
                double area = Math.Abs((px - avgX) * (y[i] - py) - (px - x[i]) * (avgY - py)) * 0.5;
                if (area > maxArea) { maxArea = area; selected = i; }
            }

            sampledX[b + 1] = x[selected];
            sampledY[b + 1] = y[selected];
            prevSelected = selected;
        }

        return new(sampledX, sampledY);
    }
}
