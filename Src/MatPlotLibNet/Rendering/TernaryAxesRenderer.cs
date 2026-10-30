// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes in the ternary coordinate system: an equilateral triangle that a composition of three parts is placed
/// in, with a grid at every fifth and the parts named at the corners.
/// <para>The parts are named by the labels the axes already carries: the part at the apex by the Y label, the part at the
/// bottom left by the Z label, the part at the bottom right by the X label. The ticks run counter-clockwise, the way mpltern
/// draws them: the right-hand part along the base from left to right, the top part up the right edge, the left-hand part down
/// the left edge. A line of the grid joins the edge its part is ticked on to the next edge round.</para>
/// <para>The plot area this is given is the triangle's own box (<see cref="PlotAspectExtensions.FittedTo"/>), and it is the
/// whole of the frame: <see cref="AxesRenderer.ComputeViewTransform"/> is an ordinary transform over a frame one unit wide,
/// so the hit test and the pointer conversion read it like any other. The tick labels and the names of the corners are drawn
/// outside the box, in the margins of the figure.</para></summary>
public sealed class TernaryAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new ternary axes renderer.</summary>
    /// <param name="axes">The axes model to render.</param>
    /// <param name="plotArea">The pixel-space box of the triangle.</param>
    /// <param name="ctx">The drawing surface to emit primitives onto.</param>
    /// <param name="theme">The active visual theme.</param>
    public TernaryAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    /// <remarks>The frame one unit wide and √3/2 high over the plot area, whatever the series: nothing zooms into a triangle.</remarks>
    internal override DataTransform? ComputeViewTransform() => new(0, 1, 0, TernaryFrame.Height, PlotArea);

    /// <inheritdoc />
    public override void Render()
    {
        var view = new DataTransform(0, 1, 0, TernaryFrame.Height, PlotArea);
        DrawnTransform = view;
        Point Place(TernaryPoint composition)
        {
            var frame = composition.InFrame();
            return view.DataToPixel(frame.X, frame.Y);
        }

        Point[] corners = [Place(new(1, 0, 0)), Place(new(0, 1, 0)), Place(new(0, 0, 1))];
        Ctx.DrawPolygon(corners, new ShapeStyle(Theme.AxesBackground, null, 0));

        var grid = Axes.ShapeGridUnder(Theme);
        if (grid.Visible)
            Ctx.DrawTernaryGrid(Place, new StrokeStyle(grid.Color, grid.LineWidth, grid.LineStyle));

        RenderSeries();

        Ctx.DrawPolygon(corners, new ShapeStyle(null, Theme.ForegroundText, 1));

        var tickFont = TickFont();
        Ctx.DrawTernaryTicks(Place, tickFont, fraction => fraction.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
        DrawCornerNames(corners, tickFont);

        RenderLegend();
        RenderTitle();
    }

    /// <summary>The names of the parts at their corners: above the apex, and under the two base corners, clear of the tick
    /// labels. The title of the axes is raised above the name at the apex.</summary>
    private void DrawCornerNames(Point[] corners, Font tickFont)
    {
        var apex = corners[0];
        var bottomLeft = corners[1];
        var bottomRight = corners[2];
        double belowBase = tickFont.Size + TernaryDrawingExtensions.TickGap + 4;

        if (Axes.YAxis.Label is { } top)
        {
            var font = NameFont(Axes.YAxis);
            DrawName(top, new Point(apex.X, apex.Y - 6), font);
            MeasuredTopTickBand = font.Size + 6;
        }

        if (Axes.ZAxis.Label is { } left)
            DrawName(left, new Point(bottomLeft.X, bottomLeft.Y + belowBase + NameFont(Axes.ZAxis).Size), NameFont(Axes.ZAxis));

        if (Axes.XAxis.Label is { } right)
            DrawName(right, new Point(bottomRight.X, bottomRight.Y + belowBase + NameFont(Axes.XAxis).Size), NameFont(Axes.XAxis));
    }

    private Font NameFont(Axis axis)
    {
        var baseFont = LabelFont();
        return axis.LabelStyle?.ApplyTo(baseFont) ?? baseFont;
    }

    private void DrawName(string text, Point at, Font font)
    {
        if (MathTextParser.ContainsMath(text))
            Ctx.DrawRichText(MathTextParser.Parse(text), at, font, TextAlignment.Center);
        else
            Ctx.DrawText(text, at, font, TextAlignment.Center);
    }
}
