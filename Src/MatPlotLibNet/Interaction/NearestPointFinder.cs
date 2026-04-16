// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Interaction;

/// <summary>Scans all visible <see cref="XYSeries"/> in a subplot and returns the data point
/// closest to a given data-space position, provided it falls within a pixel-distance threshold.
/// Used by the hover tooltip pipeline to show the nearest point's coordinates.</summary>
public static class NearestPointFinder
{
    /// <summary>Finds the nearest data point to <c>(dataX, dataY)</c> across all visible
    /// <see cref="XYSeries"/> in the specified subplot.</summary>
    /// <param name="figure">The figure containing the target subplot.</param>
    /// <param name="axesIndex">Index of the subplot to search within.</param>
    /// <param name="dataX">Cursor position X in data space.</param>
    /// <param name="dataY">Cursor position Y in data space.</param>
    /// <param name="layout">Current chart layout for data-to-pixel conversion.</param>
    /// <param name="maxPixelDistance">Maximum pixel distance from the cursor to consider a point
    /// as a hover candidate. Points farther away are ignored.</param>
    /// <returns>The closest point within the threshold, or <c>null</c> if none qualifies.</returns>
    public static NearestPointResult? Find(
        Figure figure, int axesIndex,
        double dataX, double dataY,
        IChartLayout layout,
        double maxPixelDistance = 20.0)
    {
        if (axesIndex < 0 || axesIndex >= figure.SubPlots.Count)
            return null;

        var axes = figure.SubPlots[axesIndex];
        var plotArea = layout.GetPlotArea(axesIndex);
        var (xMin, xMax, yMin, yMax) = layout.GetDataRange(axesIndex);

        double xSpan = xMax - xMin;
        double ySpan = yMax - yMin;
        if (xSpan == 0 || ySpan == 0) return null;

        NearestPointResult? best = null;

        for (int sIdx = 0; sIdx < axes.Series.Count; sIdx++)
        {
            if (axes.Series[sIdx] is not XYSeries xySeries) continue;
            if (!xySeries.Visible) continue;

            var xData = xySeries.XData;
            var yData = xySeries.YData;
            if (xData is null || yData is null) continue;

            int count = Math.Min(xData.Length, yData.Length);
            for (int i = 0; i < count; i++)
            {
                double dx = xData[i] - dataX;
                double dy = yData[i] - dataY;

                // Convert data-space deltas to pixel-space deltas.
                double pxDx = dx / xSpan * plotArea.Width;
                double pxDy = dy / ySpan * plotArea.Height;
                double pixelDist = Math.Sqrt(pxDx * pxDx + pxDy * pxDy);

                if (pixelDist > maxPixelDistance) continue;

                if (best is null || pixelDist < best.PixelDistance)
                {
                    best = new NearestPointResult(
                        xySeries.Label ?? $"Series {sIdx}",
                        sIdx,
                        xData[i], yData[i],
                        pixelDist);
                }
            }
        }

        return best;
    }
}
