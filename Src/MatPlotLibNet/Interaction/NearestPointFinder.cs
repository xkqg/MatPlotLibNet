// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Finds the data point of a subplot closest to a given data-space position, provided it falls within a
/// pixel-distance threshold. Every series a hover can land on answers for itself: a series of
/// points, a ternary diagram and a state timeline. Used by the hover tooltip pipeline to show the nearest point's
/// coordinates.</summary>
public static class NearestPointFinder
{
    /// <summary>Finds the nearest data point to <c>(dataX, dataY)</c> across the visible series of the specified subplot:
    /// the series are asked in order, and a later one answers only when it beats the answer so far (a state timeline's
    /// segment under the cursor always does).</summary>
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
        // Distances are measured on screen: every point and the cursor go through the transform the axes are drawn with,
        // so a log or symlog axis measures what the reader sees, not a linear stretch of its range.
        var view = layout.ViewOf(axesIndex);
        if (view.DataXMax == view.DataXMin || view.DataYMax == view.DataYMin) return null;

        var search = HoverSearch.At(axes, view, dataX, dataY, maxPixelDistance);
        NearestPointResult? best = null;
        for (int seriesIndex = 0; seriesIndex < axes.Series.Count; seriesIndex++)
        {
            if (axes.Series[seriesIndex] is IHoverTarget target)
                best = target.Nearest(search, seriesIndex, best);
        }

        return best;
    }
}
