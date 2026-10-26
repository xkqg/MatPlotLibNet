// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>The one way an interaction modifier reaches the mapping between the pointer and the data of an axes.</summary>
internal static class ChartLayoutViewExtensions
{
    extension(IChartLayout layout)
    {
        /// <summary>The transform axes <paramref name="axesIndex"/> are drawn with. For the library's own
        /// <see cref="ChartLayout"/> that is the renderer's view (<see cref="ChartLayout.View"/>: the range drawn after
        /// autoscale and the log or symlog scale). <see cref="IChartLayout"/> is a public interface a host may implement
        /// with nothing but a plot area and a range, so any other layout is read as what it states: its range, linear over
        /// its plot area.</summary>
        internal DataTransform ViewOf(int axesIndex)
        {
            if (layout is ChartLayout drawn) return drawn.View(axesIndex);
            var (xMin, xMax, yMin, yMax) = layout.GetDataRange(axesIndex);
            return new DataTransform(xMin, xMax, yMin, yMax, layout.GetPlotArea(axesIndex));
        }

        /// <summary>The transform every outward-stacked secondary Y axis of <paramref name="axesIndex"/> is drawn with —
        /// the secondary counterpart of <see cref="ViewOf"/>, so a modifier converts a pixel position through a
        /// SPECIFIC secondary axis' own scale, not only the primary's (ROADMAP.md row 100). For the library's own
        /// <see cref="ChartLayout"/> that is the renderer's own view; any other host states only a range
        /// (<see cref="IChartLayout.GetSecondaryDataRanges"/>), read here as linear over the shared plot area.</summary>
        internal IReadOnlyList<SecondaryAxisTransform> SecondaryViewsOf(int axesIndex)
        {
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
