// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>What a ternary series draws through.</summary>
internal static class TernarySeriesContextExtensions
{
    extension(SeriesRenderContext context)
    {
        /// <summary>The transform a ternary series is drawn with: the frame one unit wide and √3/2 high
        /// (<see cref="TernaryFrame"/>) over the plot area, which is the triangle's own box. The axes draws its triangle through
        /// the same rectangle (<c>TernaryAxesRenderer.ComputeViewTransform</c>), so a marker stands where the grid says.</summary>
        internal DataTransform TernaryViewTransform() => new(0, 1, 0, TernaryFrame.Height, context.Area.PlotBounds);

        /// <summary>The pixel of each sample of <paramref name="series"/> that can be placed, in the order given.</summary>
        /// <param name="series">The series that is drawn.</param>
        internal List<Point> PixelsOf(TernarySeries series)
        {
            var view = context.TernaryViewTransform();
            return [.. series.FramePoints().Select(mark => view.DataToPixel(mark.Position.X, mark.Position.Y))];
        }
    }
}
