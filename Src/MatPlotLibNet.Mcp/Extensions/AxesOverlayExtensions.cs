// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Mcp;

/// <summary>What an axes draws with no series on it, for the spec reader's question whether a figure is a picture.</summary>
internal static class AxesOverlayExtensions
{
    extension(Axes axes)
    {
        /// <summary>Whether the axes, or an inset the renderer draws on it, holds an overlay the renderer draws: a span, a patch, a
        /// reference line, an annotation, a trendline, a horizontal level, a Fibonacci retracement or a signal. Only the Cartesian
        /// renderer draws them (the polar, ternary and 3-D ones read no overlay). An inset without bounds never gets here: reading
        /// the figure leaves it out, as the renderer would not draw it. The breaks of an axis are not counted, nor is a legend or a colour bar: they describe what series draw, so with none
        /// they are no picture.</summary>
        internal bool DrawsAnOverlay =>
            (axes.CoordinateSystem == CoordinateSystem.Cartesian
                && axes.Annotations.Count + axes.ReferenceLines.Count + axes.Spans.Count + axes.Patches.Count + axes.Trendlines.Count
                    + axes.HorizontalLevels.Count + axes.FibonacciRetracements.Count + axes.Signals.Count > 0)
            || axes.Insets.Any(inset => inset.DrawsAnOverlay);
    }
}
