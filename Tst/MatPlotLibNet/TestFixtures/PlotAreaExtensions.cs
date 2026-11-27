// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests;

/// <summary>Where the layout puts the plot of a figure, as a test of any suite asks for it.</summary>
public static class PlotAreaExtensions
{
    extension(Figure figure)
    {
        /// <summary>The plot area the layout gives the first axes of the figure.</summary>
        /// <param name="ctx">The surface the layout measures text on; an SVG one when it is <see langword="null"/>.</param>
        public Rect FirstPlotArea(IRenderContext? ctx = null) =>
            new ChartRenderer().ComputeLayout(figure, ctx ?? new SvgRenderContext()).PlotAreas[0];
    }
}
