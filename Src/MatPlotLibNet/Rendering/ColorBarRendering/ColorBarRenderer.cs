// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>
/// Abstract base for orientation-specific colorbar rendering: each subclass places the bar beside
/// its plot and its tick labels and label beside the bar; the bar's body (the gradient, the wedges
/// of the Extend modes, the edges and the frame) is drawn by one strip for both orientations.
/// </summary>
/// <remarks>
/// Two concrete subclasses — horizontal colour bar renderer and
/// vertical colour bar renderer — own the orientation-specific
/// layout logic via polymorphism, so <c>AxesRenderer.RenderColorBar</c> carries no
/// switch on the orientation.
/// </remarks>
public abstract class ColorBarRenderer
{
    /// <summary>The colorbar configuration model (Orientation, Extend, Label, etc.).</summary>
    protected ColorBar Cb { get; }

    /// <summary>The resolved color map (from ColorBar.ColorMap, then data provider, then Viridis default).</summary>
    protected IColorMap ColorMap { get; }

    /// <summary>The lower bound of the colorbar value range.</summary>
    protected double Min { get; }

    /// <summary>The upper bound of the colorbar value range.</summary>
    protected double Max { get; }

    /// <summary>The plot area within which the colorbar is positioned (typically alongside the axes).</summary>
    protected Rect PlotArea { get; }

    /// <summary>The render context to draw into (SVG / Skia / etc.).</summary>
    protected IRenderContext Ctx { get; }

    /// <summary>The active visual theme (used for foreground text, frame border, etc.).</summary>
    protected Theme Theme { get; }

    /// <summary>The number of gradient steps along the colorbar (default <see cref="ColorBarStrip.RestingSteps"/>).</summary>
    protected int Steps { get; }

    /// <summary>Constructs a renderer with all the state needed to draw the colorbar.</summary>
    protected ColorBarRenderer(ColorBar cb, IColorMap colorMap, double min, double max,
                                Rect plotArea, IRenderContext ctx, Theme theme, int steps = ColorBarStrip.RestingSteps)
    {
        Cb = cb;
        ColorMap = colorMap;
        Min = min;
        Max = max;
        PlotArea = plotArea;
        Ctx = ctx;
        Theme = theme;
        Steps = steps;
    }

    /// <summary>The normaliser the chart colours its values with, which decides where a tick stands along the gradient: the
    /// linear one unless the factory is given the series' own.</summary>
    internal INormalizer Normalizer { get; init; } = LinearNormalizer.Instance;

    /// <summary>The cell the layout gave the axes, which a bar that takes its room from them stands in
    /// (<see cref="ColorBarRoomExtensions"/>); <see langword="null"/> when it is not known.</summary>
    internal Rect? Cell { get; init; }

    /// <summary>How far past the plot area the axes draws on the bar's side, which a bar in a margin stands past
    /// (<see cref="ColorBarRoom.ClearanceOf"/>); 0 when it is not known.</summary>
    internal double Clearance { get; init; }

    /// <summary>The room the figure's colour bars take (<see cref="ColorBarRoom"/>): set by the factory, from the theme when it is not
    /// told.</summary>
    internal ColorBarRoom Room { get; init; } = ColorBarRoom.Margin;

    /// <summary>The lines the bar draws in place of a fill, those of the contour of lines that backs it
    /// (<see cref="Rendering.ColorBarRendering.ResolvedColorBarExtensions"/>); <see langword="null"/> for a bar that is filled.</summary>
    internal ColorBarLines? Lines { get; init; }

    /// <summary>The rectangle the bar's body stands in, by the one rule for it (<see cref="ColorBarRoom.BodyOf"/>).</summary>
    private protected Rect Body => Room.BodyOf(Cb, PlotArea, Cell, Clearance);

    /// <summary>The ticks along a gradient of <paramref name="lengthPx"/>, in the theme's tick font, placed where the chart's
    /// normaliser sends their values: those of the bar's own locator when it was given one.</summary>
    /// <param name="lengthPx">The length of the whole bar.</param>
    /// <param name="vertical">Whether the bar is vertical.</param>
    /// <returns>The ticks with their labels.</returns>
    internal ColorBarScale ScaleAlong(double lengthPx, bool vertical) =>
        ColorBarScale.For(Min, Max, lengthPx, vertical, Normalizer, Ctx, Theme.TickFont(), Theme, Cb.Locator);

    /// <summary>Draws the bar's body, the same for both orientations: the strip in <see cref="Body"/> (the gradient, the wedges of the
    /// Extend modes, the edges and the frame), the ticks along it (<see cref="ScaleAlong"/>) and the marks on them
    /// (<see cref="ColorBarMarks"/>). An orientation then places only its labels, by what this returns.</summary>
    /// <param name="vertical">Whether the bar is vertical.</param>
    /// <returns>The body's rectangle, the ticks along it, the gradient they stand over and the marks.</returns>
    private protected ColorBarBody DrawBody(bool vertical)
    {
        var body = Body;
        var strip = ColorBarStrip.Of(Cb, body, vertical, Steps, Theme, Lines);
        var scale = ScaleAlong(strip.AxisLength, vertical);
        strip.Draw(Ctx, ColorMap, scale, Theme.ForegroundText);
        var gradient = strip.Gradient;
        var marks = ColorBarMarks.Under(Theme);
        marks.Draw(Ctx, scale, gradient, vertical, Theme.ForegroundText, strip.FrameWidth);
        return new ColorBarBody(body, scale, gradient, marks);
    }

    /// <summary>Renders the colorbar onto the context. Concrete subclasses implement
    /// orientation-specific layout (horizontal below the plot, vertical to the right).</summary>
    public abstract void Render();

    /// <summary>A colour bar's body as <see cref="ColorBarRenderer.DrawBody"/> drew it, which the labels of an orientation are placed by.</summary>
    /// <param name="Area">The rectangle the body stands in.</param>
    /// <param name="Scale">The ticks along the bar, with their labels.</param>
    /// <param name="Gradient">The rectangle of the gradient the ticks stand over (the bar without the wedges at its ends).</param>
    /// <param name="Marks">The tick marks the theme draws on the bar, whose gap the labels stand past.</param>
    internal readonly record struct ColorBarBody(Rect Area, ColorBarScale Scale, Rect Gradient, ColorBarMarks Marks);
}
