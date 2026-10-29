// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>What a polar series draws through.</summary>
internal static class PolarSeriesContextExtensions
{
    extension(SeriesRenderContext context)
    {
        /// <summary>The transform a polar series is drawn with: out to the extent of the axes' radial scale when the axes handed
        /// one over, otherwise out to the extent the series makes alone.</summary>
        /// <param name="series">The series that is drawn.</param>
        internal PolarTransform PolarTransformFor(ISeries series) =>
            new(context.Area.PlotBounds, (context.RadialScale ?? RadialScale.Alone(series)).Extent);
    }
}
