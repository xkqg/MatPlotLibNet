// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>The one way an interaction modifier reaches the mapping between the pointer and the data of an axes: whether a pointer
/// falls on an axes and which, a press of the left button, the press read as its axes and data, the transforms of the axes and the
/// ranges its secondary axes show between two pixels.</summary>
internal static class ChartLayoutViewExtensions
{
    extension(IChartLayout layout)
    {
        /// <summary>Whether the pointer at (<paramref name="pixelX"/>, <paramref name="pixelY"/>) falls on an axes.</summary>
        /// <param name="pixelX">The pointer's X, in pixels.</param>
        /// <param name="pixelY">The pointer's Y, in pixels.</param>
        /// <returns><see langword="true"/> when an axes lies under the pointer.</returns>
        internal bool IsOnAxes(double pixelX, double pixelY) => layout.HitTestAxes(pixelX, pixelY) is not null;

        /// <summary>The axes the pointer at (<paramref name="pixelX"/>, <paramref name="pixelY"/>) fell in, the first when it fell in
        /// none: the axes a gesture acts on once it was claimed.</summary>
        /// <param name="pixelX">The pointer's X, in pixels.</param>
        /// <param name="pixelY">The pointer's Y, in pixels.</param>
        /// <returns>The index of the axes.</returns>
        internal int AxesOrFirstAt(double pixelX, double pixelY) => layout.HitTestAxes(pixelX, pixelY) ?? 0;

        /// <summary>The axes a press of the left button fell on: the gesture every left-button modifier opens on, beside a condition of
        /// its own (a key held or not, a tool active).</summary>
        /// <param name="press">The press.</param>
        /// <returns>The index of the axes, or <see langword="null"/> for another button or a press off every axes.</returns>
        internal int? LeftPressAxes(PointerInputArgs press) =>
            press.Button == PointerButton.Left ? layout.HitTestAxes(press.X, press.Y) : null;

        /// <summary>Whether <paramref name="press"/> is a press of the left button on an axes (<see cref="LeftPressAxes"/>).</summary>
        /// <param name="press">The press.</param>
        /// <returns><see langword="true"/> for a left press on an axes.</returns>
        internal bool IsLeftPressOnAxes(PointerInputArgs press) => layout.LeftPressAxes(press) is not null;

        /// <summary>The press at (<paramref name="pixelX"/>, <paramref name="pixelY"/>) as a modifier reads it: the axes it fell in (the
        /// first when it fell in none) and the data point under it.</summary>
        /// <param name="pixelX">The press's X, in pixels.</param>
        /// <param name="pixelY">The press's Y, in pixels.</param>
        /// <returns>The press, or <see langword="null"/> when no data lies under it.</returns>
        internal AxesPress? PressAt(double pixelX, double pixelY)
        {
            int axesIndex = layout.AxesOrFirstAt(pixelX, pixelY);
            return layout.PixelToData(pixelX, pixelY, axesIndex) is { } data ? new AxesPress(axesIndex, data) : null;
        }

        /// <summary>The range every Y axis stacked past the primary of <paramref name="axesIndex"/> shows between the pixels
        /// <paramref name="from"/> and <paramref name="to"/>, each read through that axis' own transform (<see cref="SecondaryViewsOf"/>),
        /// so a log or symlog axis follows its own scale: the one rule a zoom rectangle and a pan move the secondary axes by.</summary>
        /// <param name="axesIndex">The axes.</param>
        /// <param name="from">The first pixel, read first.</param>
        /// <param name="to">The second pixel.</param>
        /// <param name="range">The range the two data points make: their box (<see cref="DataRange.Between"/>) or the corners as given
        /// (<see cref="DataRange.FromCorners"/>).</param>
        /// <returns>One range per secondary axis, or <see langword="null"/> when the axes has none.</returns>
        internal List<SecondaryAxisRange>? SecondaryRangesBetween(int axesIndex, Point from, Point to, Func<Point, Point, DataRange> range)
        {
            var views = layout.SecondaryViewsOf(axesIndex);
            if (views.Count == 0) return null;

            var ranges = new List<SecondaryAxisRange>(views.Count);
            foreach (var v in views)
            {
                var a = v.Transform.PixelToData(from);
                var b = v.Transform.PixelToData(to);
                ranges.Add(new SecondaryAxisRange(v.Side, v.SlotIndex, range(a, b)));
            }

            return ranges;
        }

        /// <summary>The range every Y axis stacked past the primary of <paramref name="axesIndex"/> shows after a zoom by
        /// <paramref name="scale"/> about <paramref name="pixel"/>, each by its own transform's rule (<see cref="DataTransform.ZoomedAbout"/>),
        /// the rule the primary is zoomed by.</summary>
        /// <param name="axesIndex">The axes.</param>
        /// <param name="pixel">The pointer.</param>
        /// <param name="scale">The share of the range the zoom keeps.</param>
        /// <returns>One range per secondary axis, or <see langword="null"/> when the axes has none.</returns>
        internal List<SecondaryAxisRange>? SecondaryRangesZoomedAbout(int axesIndex, Point pixel, double scale)
        {
            var views = layout.SecondaryViewsOf(axesIndex);
            if (views.Count == 0) return null;

            var ranges = new List<SecondaryAxisRange>(views.Count);
            foreach (var v in views) ranges.Add(new SecondaryAxisRange(v.Side, v.SlotIndex, v.Transform.ZoomedAbout(pixel, scale)));
            return ranges;
        }

        /// <summary>The transform axes <paramref name="axesIndex"/> are drawn with. For the library's own
        /// <see cref="ChartLayout"/> that is the renderer's view (<see cref="ChartLayout.View"/>: the range drawn after
        /// autoscale and the log or symlog scale). <see cref="IChartLayout"/> is a public interface a host may implement
        /// with nothing but a plot area and a range, so any other layout is read as what it states: its range, linear over
        /// its plot area.</summary>
        internal DataTransform ViewOf(int axesIndex)
        {
            if (layout is LayoutSlot slot) return slot.Current.ViewOf(axesIndex);
            if (layout is ChartLayout drawn) return drawn.View(axesIndex);
            var (xMin, xMax, yMin, yMax) = layout.GetDataRange(axesIndex);
            return new DataTransform(xMin, xMax, yMin, yMax, layout.GetPlotArea(axesIndex));
        }

        /// <summary>The transform every Y axis stacked past the primary of <paramref name="axesIndex"/> is drawn with —
        /// the counterpart of <see cref="ViewOf"/> for those axes, so a modifier converts a pixel position through a
        /// SPECIFIC axis' own scale, not only the primary's. For the library's own
        /// <see cref="ChartLayout"/> that is the renderer's own view; any other host states only a range
        /// (<see cref="IChartLayout.GetSecondaryDataRanges"/>), read here as linear over the shared plot area.</summary>
        internal IReadOnlyList<SecondaryAxisTransform> SecondaryViewsOf(int axesIndex)
        {
            if (layout is LayoutSlot slot) return slot.Current.SecondaryViewsOf(axesIndex);
            if (layout is ChartLayout drawn) return drawn.SecondaryViews(axesIndex);
            var area = layout.GetPlotArea(axesIndex);
            var (xMin, xMax, _, _) = layout.GetDataRange(axesIndex);
            return layout.GetSecondaryDataRanges(axesIndex)
                .Select(r => new SecondaryAxisTransform(r.Side, r.SlotIndex,
                    new DataTransform(xMin, xMax, r.Range.YMin, r.Range.YMax, area)))
                .ToList();
        }
    }
}

/// <summary>A press as a modifier reads it: the axes it fell in and the data point under it.</summary>
/// <param name="AxesIndex">The index of the axes the press fell in.</param>
/// <param name="Data">The data point under the press.</param>
internal readonly record struct AxesPress(int AxesIndex, DataPoint Data);
