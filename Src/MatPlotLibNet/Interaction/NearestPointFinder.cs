// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
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
        // Distances are measured on screen: every point and the cursor go through the transform the axes are drawn with,
        // so a log or symlog axis measures what the reader sees, not a linear stretch of its range.
        var view = layout.ViewOf(axesIndex);
        if (view.DataXMax == view.DataXMin || view.DataYMax == view.DataYMin) return null;
        var cursor = view.DataToPixel(dataX, dataY);

        NearestPointResult? best = null;

        for (int sIdx = 0; sIdx < axes.Series.Count; sIdx++)
        {
            if (axes.Series[sIdx] is StateTimelineSeries timeline)
            {
                var segment = SegmentUnder(timeline, dataX);
                if (segment is { } hit)
                {
                    var span = timeline.Segments[hit];
                    best = new NearestPointResult(timeline.Label ?? $"Series {sIdx}", sIdx, dataX, dataY, 0.0)
                    {
                        PointIndex = hit,
                        Detail = $"{span.Label} ({AsText(span.Start, axes)}–{AsText(span.End, axes)})",
                    };
                }

                continue;
            }

            if (axes.Series[sIdx] is not XYSeries xySeries) continue;
            if (!xySeries.Visible) continue;

            var xData = xySeries.XData;
            var yData = xySeries.YData;
            if (xData is null || yData is null) continue;

            int count = Math.Min(xData.Length, yData.Length);
            for (int i = 0; i < count; i++)
            {
                var at = view.DataToPixel(xData[i], yData[i]);
                double pxDx = at.X - cursor.X;
                double pxDy = at.Y - cursor.Y;
                double pixelDist = Math.Sqrt(pxDx * pxDx + pxDy * pxDy);

                // Not "> max": a point that maps to no pixel (NaN, inside an axis break or at or below zero on a log
                // axis) is never within reach.
                if (!(pixelDist <= maxPixelDistance)) continue;

                if (best is null || pixelDist < best.PixelDistance)
                {
                    best = new NearestPointResult(
                        xySeries.Label ?? $"Series {sIdx}",
                        sIdx,
                        xData[i], yData[i],
                        pixelDist)
                    {
                        // The loop walks the shorter of the two arrays, so this index addresses a sample in
                        // both. It is the only value here that keys back to the caller's own row.
                        PointIndex = i,
                    };
                }
            }
        }

        return best;
    }

    /// <summary>A position along X as a reader would say it: a date and time on a date axis, a plain number
    /// otherwise. A value no date can hold falls back to the number rather than failing a hover.</summary>
    private static string AsText(double x, Axes axes)
    {
        bool isDate = axes.XAxis.Scale == AxisScale.Date && x >= MinOADate && x <= MaxOADate;
        return isDate
            ? DateTime.FromOADate(x).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : x.ToString("G5", CultureInfo.InvariantCulture);
    }

    // The span of OLE Automation dates DateTime can represent (0001-01-01 to 9999-12-31).
    private const double MinOADate = -657434.0;
    private const double MaxOADate = 2958465.99999999;

    /// <summary>Index of the segment under <paramref name="dataX"/>, or <c>null</c> in a gap or on a hidden
    /// timeline. A segment is a span, so it is hit by containing the cursor, edges included; where segments
    /// overlap the one drawn last is the one the reader sees, so it answers.</summary>
    private static int? SegmentUnder(StateTimelineSeries timeline, double dataX)
    {
        if (!timeline.Visible) return null;

        for (int i = timeline.Segments.Count - 1; i >= 0; i--)
        {
            var segment = timeline.Segments[i];
            if (dataX >= segment.Start && dataX <= segment.End) return i;
        }

        return null;
    }
}
