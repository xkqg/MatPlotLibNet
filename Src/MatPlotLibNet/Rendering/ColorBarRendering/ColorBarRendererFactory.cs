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
    /// <summary>Constructs the right colorbar renderer for the given orientation, its ticks placed evenly along the range: the
    /// renderer of the overload that takes a normaliser, with the linear one.</summary>
    public static ColorBarRenderer Create(ColorBar cb, IColorMap colorMap, double min, double max,
                                           Rect plotArea, IRenderContext ctx, Theme theme, int steps = ColorBarStrip.RestingSteps)
        => Create(cb, colorMap, min, max, LinearNormalizer.Instance, plotArea, ctx, theme, steps: steps);

    /// <summary>Constructs the right colorbar renderer for the given orientation, placing its ticks where
    /// <paramref name="normalizer"/> sends their values: the normaliser the series that backs the bar colours its values with.
    /// The overload without one places them evenly along the range, as for a linear normaliser. <paramref name="cell"/> is the
    /// cell the layout gave the axes, which a bar that takes its room from them stands in; <paramref name="clearance"/> is what
    /// the axes draws past its plot on the bar's side, which a bar in a margin stands past; <paramref name="lines"/> are the level
    /// lines of the contour of lines that backs the bar, drawn in place of a fill; <paramref name="room"/> is the room the figure's
    /// bars take, the theme's outside the constrained layout when it is not given.</summary>
    /// <param name="cb">The colour bar.</param>
    /// <param name="colorMap">The colour map of the gradient.</param>
    /// <param name="min">The value at the low end of the bar.</param>
    /// <param name="max">The value at the high end of the bar.</param>
    /// <param name="normalizer">The normaliser that sends a value to a place on the bar.</param>
    /// <param name="plotArea">The plot area of the axes the bar belongs to.</param>
    /// <param name="ctx">The context the bar is measured and drawn with.</param>
    /// <param name="theme">The theme the bar is drawn under.</param>
    /// <param name="cell">The cell the layout gave the axes; <see langword="null"/> when it is not known.</param>
    /// <param name="steps">The number of colour steps the gradient is drawn in.</param>
    /// <param name="clearance">How far past the plot area the axes draws on the bar's side, in pixels.</param>
    /// <param name="lines">The lines of the bar's ticks; <see langword="null"/> for the resting ones.</param>
    /// <param name="room">The room of the colour bars; <see langword="null"/> takes the room the theme gives them outside a
    /// constrained layout.</param>
    /// <returns>The renderer of the bar.</returns>
    internal static ColorBarRenderer Create(ColorBar cb, IColorMap colorMap, double min, double max, INormalizer normalizer,
                                             Rect plotArea, IRenderContext ctx, Theme theme, Rect? cell = null, int steps = ColorBarStrip.RestingSteps,
                                             double clearance = 0, ColorBarLines? lines = null, ColorBarRoom? room = null)
    {
        var where = room ?? ColorBarRoom.Under(theme, constrained: false);
        return cb.Orientation switch
        {
            ColorBarOrientation.Horizontal => new HorizontalColorBarRenderer(cb, colorMap, min, max, plotArea, ctx, theme, steps) { Normalizer = normalizer, Cell = cell, Clearance = clearance, Lines = lines, Room = where },
            _                              => new VerticalColorBarRenderer(cb, colorMap, min, max, plotArea, ctx, theme, steps) { Normalizer = normalizer, Cell = cell, Clearance = clearance, Lines = lines, Room = where },
        };
    }
}
