// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>
/// Factory selecting the orientation-specific <see cref="ColorBarRenderer"/>
/// subtype based on <see cref="ColorBar.Orientation"/>. This is the only static
/// member in the <c>ColorBarRendering</c> hierarchy — it dispatches to a
/// polymorphic instance, so the inline-switch anti-pattern is contained at
/// exactly one well-known location.
/// </summary>
public static class ColorBarRendererFactory
{
    /// <summary>Constructs the right colorbar renderer for the given orientation.</summary>
    public static ColorBarRenderer Create(ColorBar cb, IColorMap colorMap, double min, double max,
                                           Rect plotArea, IRenderContext ctx, Theme theme, int steps = 50)
        => cb.Orientation switch
        {
            ColorBarOrientation.Horizontal => new HorizontalColorBarRenderer(cb, colorMap, min, max, plotArea, ctx, theme, steps),
            _                              => new VerticalColorBarRenderer(cb, colorMap, min, max, plotArea, ctx, theme, steps),
        };
}
