// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The body of a colour bar: the gradient laid out in steps along the bar, a wedge at each end the bar is extended
/// at, a line across the start of each step when edges are drawn, and the frame round the whole bar. A wedge is a rectangle a
/// tenth of the bar long, inside it, or, under the themes that draw a bar as matplotlib does (<see cref="Triangles"/>), a triangle
/// outside the interior, with the frame round the interior and the tips. The one place a colour bar's body is drawn, for the bar
/// of an axes either way round and for the bar of the figure.</summary>
/// <param name="Bar">The whole bar, its wedges included.</param>
/// <param name="Vertical">Whether the bar runs upward, its end on top, rather than to the right.</param>
/// <param name="Extend">Which ends of the bar carry a wedge for the values past the range.</param>
/// <param name="DrawEdges">Whether a line is drawn across the start of each step.</param>
/// <param name="Steps">How many steps the gradient is drawn in.</param>
internal readonly record struct ColorBarStrip(Rect Bar, bool Vertical, ColorBarExtend Extend, bool DrawEdges, int Steps)
{
    /// <summary>The share of the bar's length each wedge takes when it is a rectangle.</summary>
    internal const double ExtendFraction = 0.10;

    /// <summary>The share of the interior's length each triangle takes (matplotlib's <c>extendfrac</c> at rest, 0.05).</summary>
    internal const double TriangleFraction = 0.05;

    /// <summary>The strip of <paramref name="bar"/> drawn under <paramref name="theme"/>: its ends as the bar asks, its wedges as the
    /// theme draws them (<see cref="ColorBarLook.ExtendTriangles"/>), and the level lines of a contour of lines in place of a fill.
    /// The one place the bar of an axes and the bar of the figure build their strip.</summary>
    /// <param name="cb">The colour bar.</param>
    /// <param name="bar">The whole bar, its wedges included.</param>
    /// <param name="vertical">Whether the bar runs upward.</param>
    /// <param name="steps">How many steps the gradient is drawn in (<see cref="StepsFor"/>).</param>
    /// <param name="theme">The theme of the figure.</param>
    /// <param name="lines">The level lines, or <see langword="null"/> for a filled bar.</param>
    /// <returns>The strip.</returns>
    internal static ColorBarStrip Of(Models.ColorBar cb, Rect bar, bool vertical, int steps, Theme theme, ColorBarLines? lines) =>
        new(bar, vertical, cb.Extend, cb.DrawEdges, steps)
        {
            Lines = lines,
            Triangles = theme.Looks.ColorBar.ExtendTriangles,
            FrameWidth = theme.Looks.Frame.SpineWidthOver(null, RestingFrameWidth),
            EdgeWidth = theme.Looks.Frame.SpineWidth is { } spine ? spine / 2 : RestingEdgeWidth,
        };

    /// <summary>How wide the frame is drawn under a theme that names no spine width, in pixels.</summary>
    internal const double RestingFrameWidth = 0.5;

    /// <summary>How wide an edge between two steps is drawn under a theme that names no spine width, in pixels.</summary>
    internal const double RestingEdgeWidth = 0.3;

    /// <summary>How many steps a bar's gradient is drawn in under the library's own themes.</summary>
    internal const int RestingSteps = 50;

    /// <summary>How many steps the gradient of a bar in <paramref name="map"/> is drawn in under <paramref name="theme"/>: one for
    /// each colour of the map under a theme that asks for it (<see cref="ColorBarLook.StepPerColour"/>, matplotlib's
    /// <c>Colorbar</c>: as many as a listed map lists, 256 for any other, matplotlib's <c>N</c>), else
    /// <see cref="RestingSteps"/>. The one rule the bar of an axes and the bar of the figure read.</summary>
    /// <param name="map">The bar's colour map.</param>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The number of steps.</returns>
    internal static int StepsFor(IColorMap map, Theme theme) => theme.Looks.ColorBar.StepPerColour ? Size(map) : RestingSteps;

    /// <summary>How many colours <paramref name="map"/> has (matplotlib's <c>Colormap.N</c>): a listed map's own count, a reversed
    /// map's that of the map it reverses, and 256 for a map built from stops, as matplotlib builds one.</summary>
    private static int Size(IColorMap map) => map switch
    {
        ListedColorMap listed => listed.ColorCount,
        ReversedColorMap reversed => Size(reversed.Inner),
        _ => 256,
    };

    /// <summary>Whether the bar runs along X (to the right); a vertical bar runs along Y, upward.</summary>
    private bool AlongX => !Vertical;

    /// <summary>Whether the bar's high values come first on the screen (<see cref="AlongExtensions"/>' <c>ValueGrowthAlong</c>): a
    /// vertical bar's top is its high end.</summary>
    private bool HighFirst => Rect.ValueGrowthAlong(AlongX) < 0;

    private bool ExtendsMin => Extend is ColorBarExtend.Min or ColorBarExtend.Both;

    private bool ExtendsMax => Extend is ColorBarExtend.Max or ColorBarExtend.Both;

    /// <summary>How long one wedge is along a bar <paramref name="length"/> long: a tenth of it for a rectangle; for a triangle
    /// <see cref="TriangleFraction"/> of the interior, the bar's length shrunk by <c>1 / (1 + 0.05 per extended end)</c>, as
    /// matplotlib's <c>_ColorbarAxesLocator</c> shrinks the inner axes.</summary>
    private double WedgeLength(double length) => Triangles
        ? TriangleFraction * length / (1 + (ExtendsMin ? TriangleFraction : 0) + (ExtendsMax ? TriangleFraction : 0))
        : length * ExtendFraction;

    /// <summary>The part of the bar the gradient fills: the bar less its wedges. The ticks stand along it.</summary>
    internal Rect Gradient
    {
        get
        {
            // Along the bar less a wedge at each extended end; the one at the screen's start is the high end's on a bar whose high
            // values come first (a vertical bar), across the whole bar.
            double ext = WedgeLength(Bar.LengthAlong(AlongX));
            return Rect.Along(AlongX, Bar.StartAlong(AlongX) + ((HighFirst ? ExtendsMax : ExtendsMin) ? ext : 0),
                Bar.LengthAlong(AlongX) - (ExtendsMin ? ext : 0) - (ExtendsMax ? ext : 0),
                Bar.StartAlong(!AlongX), Bar.LengthAlong(!AlongX));
        }
    }

    /// <summary>Draws the body: the wedge at the start of the bar (its left end, or the top of a vertical bar), the steps, the
    /// wedge at its far end, then the frame; a bar of <see cref="Lines"/> draws its level lines in place of the wedges and the steps.
    /// A step is drawn in the colour the scale gives the fraction it is sampled at, one pixel longer than its share so no seam shows;
    /// a wedge in the map's colour past that end, or the colour of the end when the map has none.</summary>
    /// <param name="ctx">The surface it is drawn on.</param>
    /// <param name="map">The colour map of the gradient.</param>
    /// <param name="scale">The scale that says which fraction of the map a step takes.</param>
    /// <param name="ink">The colour of the edges and the frame.</param>
    internal void Draw(IRenderContext ctx, IColorMap map, ColorBarScale scale, Color ink)
    {
        if (Lines is { } lines) DrawLevelLines(ctx, lines, scale);
        else DrawFill(ctx, map, scale, ink);

        var frame = ShapeStyle.Outlined(ink, FrameWidth);
        if (Triangles && Extend != ColorBarExtend.Neither) ctx.DrawPolygon(Outline(), frame);
        else ctx.DrawRectangle(Bar, frame);
    }

    /// <summary>The frame of a bar with triangles: round the interior and the tip of each triangle, as matplotlib's outline
    /// (<c>_do_extends</c>: <c>[[0, 0], [0.5, bot], [1, 0], [1, 1], [0.5, top], [0, 1]]</c> along the bar, turned for a horizontal one),
    /// a tip only at an end that is extended.</summary>
    private Point[] Outline()
    {
        var g = Gradient;
        double ext = WedgeLength(Bar.LengthAlong(AlongX));
        var points = new List<Point>(6);
        // matplotlib's list in value terms: along the bar from its low end to its high end, across it from its low side to its
        // high side, each the way a value grows on the screen; a tip a wedge past an extended end, at the middle across.
        double low = g.ValueStartAlong(AlongX), high = g.ValueEndAlong(AlongX), growth = Rect.ValueGrowthAlong(AlongX);
        double near = Bar.ValueStartAlong(!AlongX), far = Bar.ValueEndAlong(!AlongX), mid = Bar.CentreAlong(!AlongX);
        points.Add(Point.Along(AlongX, low, near));
        if (ExtendsMin) points.Add(Point.Along(AlongX, low - growth * ext, mid));
        points.Add(Point.Along(AlongX, low, far));
        points.Add(Point.Along(AlongX, high, far));
        if (ExtendsMax) points.Add(Point.Along(AlongX, high + growth * ext, mid));
        points.Add(Point.Along(AlongX, high, near));

        return [.. points];
    }

    /// <summary>Draws the wedge at one end in <paramref name="color"/>: a rectangle, or a triangle with its base on the interior and
    /// its tip out at the middle of the bar's end.</summary>
    /// <param name="ctx">The surface it is drawn on.</param>
    /// <param name="atStart">Whether it is the wedge drawn first: the top of a vertical bar, the left end of a horizontal one.</param>
    /// <param name="color">Its colour.</param>
    private void DrawWedge(IRenderContext ctx, bool atStart, Color color)
    {
        var fill = ShapeStyle.Filled(color);
        var g = Gradient;
        double ext = WedgeLength(Bar.LengthAlong(AlongX));
        if (!Triangles)
        {
            // From the bar's start, or from the end of the gradient, a wedge long, across the whole bar.
            ctx.DrawRectangle(Rect.Along(AlongX, atStart ? Bar.StartAlong(AlongX) : g.EndAlong(AlongX), ext,
                Bar.StartAlong(!AlongX), Bar.LengthAlong(!AlongX)), fill);
            return;
        }

        // Its base on the end of the gradient, its tip a wedge further out at the middle of the bar.
        double edge = atStart ? g.StartAlong(AlongX) : g.EndAlong(AlongX), tip = atStart ? g.StartAlong(AlongX) - ext : g.EndAlong(AlongX) + ext;
        ctx.DrawPolygon([Point.Along(AlongX, edge, Bar.StartAlong(!AlongX)), Point.Along(AlongX, tip, Bar.CentreAlong(!AlongX)),
            Point.Along(AlongX, edge, Bar.EndAlong(!AlongX))], fill);
    }

    /// <summary>The lines drawn in place of the fill, the bar of a contour of lines; <see langword="null"/> (the default) for a bar
    /// that is filled.</summary>
    internal ColorBarLines? Lines { get; init; }

    /// <summary>Whether an extended end is a triangle outside the interior, as matplotlib's (<see cref="ColorBarLook.ExtendTriangles"/>).</summary>
    internal bool Triangles { get; init; }

    /// <summary>The length the bar's ticks are laid along: the interior where the wedges are triangles outside it (matplotlib's inner
    /// axes, whose length its <c>AutoLocator</c> reads), the whole bar otherwise, as the library's own rule reads it.</summary>
    internal double AxisLength => (Triangles ? Gradient : Bar).LengthAlong(AlongX);

    /// <summary>How wide the frame is drawn, in pixels; a tick mark reaches half of it across the frame. Under a theme that names a
    /// spine width (<see cref="AxesFrameLook.SpineWidth"/>, the matplotlib themes) that width, as matplotlib's outline is a spine of the
    /// bar's axes drawn <c>axes.linewidth</c> wide; <see cref="RestingFrameWidth"/> otherwise.</summary>
    internal double FrameWidth { get; init; } = RestingFrameWidth;

    /// <summary>How wide an edge between two steps is drawn (<see cref="DrawEdges"/>), in pixels: half the theme's spine width, as
    /// matplotlib draws its dividers <c>0.5 * axes.linewidth</c> wide, or <see cref="RestingEdgeWidth"/>.</summary>
    internal double EdgeWidth { get; init; } = RestingEdgeWidth;

    /// <summary>Draws a line across the gradient at each level the bar's range holds, where the scale puts the level, in the level's
    /// colour and the lines' width: matplotlib's <c>Colorbar.add_lines</c>, which keeps the levels within a ten-billionth of the range
    /// past either end (<c>rtol</c>) and no level that is not a number (<see cref="Range1D.HoldsClose"/>).</summary>
    private void DrawLevelLines(IRenderContext ctx, ColorBarLines lines, ColorBarScale scale)
    {
        var gradient = Gradient;
        var range = new Range1D(scale.Min, scale.Max);
        for (int i = 0; i < lines.Levels.Length; i++)
        {
            double level = lines.Levels[i];
            if (!range.HoldsClose(level)) continue;

            // Across the whole bar, at the fraction of the way up (or across) the gradient the level stands.
            double at = gradient.FractionAlong(AlongX, scale.FractionOf(level));
            var stroke = StrokeStyle.Solid(lines.Colors[i], lines.Width);
            ctx.DrawLine(Point.Along(AlongX, at, Bar.StartAlong(!AlongX)), Point.Along(AlongX, at, Bar.EndAlong(!AlongX)), stroke);
        }
    }

    /// <summary>Draws the fill: the wedges, the steps and the edges.</summary>
    private void DrawFill(IRenderContext ctx, IColorMap map, ColorBarScale scale, Color ink)
    {
        var gradient = Gradient;
        var edge = StrokeStyle.Solid(ink, EdgeWidth);
        int steps = scale.StepsOf(Steps);
        // The steps in screen order from the bar's start, each sampled at its share of the values: from the high end down on a bar
        // whose high values come first. The wedge of the end at the screen's start before them, the other after.
        bool highFirst = HighFirst;
        double Share(double onScreen) => highFirst ? 1.0 - onScreen : onScreen;
        DrawEndWedge(ctx, map, highFirst, atStart: true);

        for (int i = 0; i < steps; i++)
        {
            var color = map.GetColor(scale.GradientFraction(Share((double)i / steps), Share((i + 0.5) / steps)));
            double step = gradient.StartAlong(AlongX) + gradient.LengthAlong(AlongX) * i / steps;
            ctx.DrawRectangle(Rect.Along(AlongX, step, gradient.LengthAlong(AlongX) / steps + 1, Bar.StartAlong(!AlongX), Bar.LengthAlong(!AlongX)),
                ShapeStyle.Filled(color));
            if (DrawEdges)
                ctx.DrawLine(Point.Along(AlongX, step, Bar.StartAlong(!AlongX)), Point.Along(AlongX, step, Bar.EndAlong(!AlongX)), edge);
        }

        DrawEndWedge(ctx, map, !highFirst, atStart: false);
    }

    /// <summary>Draws the wedge of the high end in the map's over colour, or of the low end in its under colour (the colour of that end
    /// when the map has none), when that end is extended.</summary>
    private void DrawEndWedge(IRenderContext ctx, IColorMap map, bool high, bool atStart)
    {
        if (high ? ExtendsMax : ExtendsMin)
            DrawWedge(ctx, atStart, high ? map.GetOverColor() ?? map.GetColor(1.0) : map.GetUnderColor() ?? map.GetColor(0.0));
    }
}
