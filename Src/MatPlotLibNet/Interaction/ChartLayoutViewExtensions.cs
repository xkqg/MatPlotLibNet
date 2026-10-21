// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>The one way an interaction modifier reaches the mapping between the pointer and the data of an axes.</summary>
internal static class ChartLayoutViewExtensions
{
    /// <summary>The transform axes <paramref name="axesIndex"/> are drawn with. For the library's own
    /// <see cref="ChartLayout"/> that is the renderer's view (<see cref="ChartLayout.View"/>: the range drawn after
    /// autoscale and the log or symlog scale). <see cref="IChartLayout"/> is a public interface a host may implement
    /// with nothing but a plot area and a range, so any other layout is read as what it states: its range, linear over
    /// its plot area.</summary>
    internal static DataTransform ViewOf(this IChartLayout layout, int axesIndex)
    {
        if (layout is ChartLayout drawn) return drawn.View(axesIndex);
        var (xMin, xMax, yMin, yMax) = layout.GetDataRange(axesIndex);
        return new DataTransform(xMin, xMax, yMin, yMax, layout.GetPlotArea(axesIndex));
    }
}
