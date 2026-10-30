// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PiperSeries"/>: the cation triangle, the anion triangle and the diamond as one frame
/// (<see cref="PiperFrame"/>) fitted into the plot area at a single scale and centred, so the triangles are equilateral at any
/// size of the figure; the grid at every fifth, the percent ticks, the names of the corners, and a marker for each sample on each
/// panel, in the one colour the sample wears. The grid is part of the shape of the diagram, drawn in the theme's grid style.</summary>
internal sealed class PiperSeriesRenderer : SeriesRenderer<PiperSeries>
{
    /// <inheritdoc />
    public PiperSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PiperSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <summary>The frame over the room the diagram has: the plot area less what the names of the corners and the tick labels
    /// need outside the panels, the largest box of the frame's own shape inside it, centred. <see langword="null"/> when no such
    /// box has an area.</summary>
    private DataTransform? FrameView()
    {
        double room = ThemedFontProvider.LabelFont(Context.Theme).Size * 3;
        var bounds = Area.PlotBounds;
        var inner = new Rect(bounds.X + room, bounds.Y + room, bounds.Width - 2 * room, bounds.Height - 2 * room);
        if (!(inner.Width > 0) || !(inner.Height > 0)) return null;

        var box = inner.WithBoxAspect(PiperFrame.Height / PiperFrame.Width);
        return new DataTransform(0, PiperFrame.Width, 0, PiperFrame.Height, box);
    }

    /// <inheritdoc />
    public override void Render(PiperSeries series)
    {
        if (FrameView() is not { } view) return;

        Point Pixel(Point frame) => view.DataToPixel(frame.X, frame.Y);
        Point CationAt(TernaryPoint composition) => Pixel(composition.InFrame());
        Point AnionAt(TernaryPoint composition)
        {
            var frame = composition.InFrame();
            return Pixel(new Point(frame.X + 1 + PiperFrame.Gap, frame.Y));
        }

        var grid = Context.Theme.DefaultGrid;
        var gridStroke = new StrokeStyle(grid.Color, grid.LineWidth, grid.LineStyle);
        var outline = new ShapeStyle(null, Context.Theme.ForegroundText, 1);
        var tickFont = ThemedFontProvider.TickFont(Context.Theme);
        var labelFont = ThemedFontProvider.LabelFont(Context.Theme);

        // The two triangles: grid, outline, ticks (percent), the names of the corners.
        // The corners that face each other across the gap are named away from each other: the sodium name ends at its corner and the
        // bicarbonate name starts at its own, so the two do not run together over a gap a fifth of a triangle wide.
        DrawTriangle(CationAt, gridStroke, outline, tickFont, labelFont, ["Mg", "Ca", "Na+K"], TextAlignment.Center, TextAlignment.Right);
        DrawTriangle(AnionAt, gridStroke, outline, tickFont, labelFont, ["SO4", "HCO3+CO3", "Cl"], TextAlignment.Left, TextAlignment.Center);

        DrawDiamond(Pixel, gridStroke, outline, labelFont);

        for (int i = 0; i < series.Samples.Count; i++)
        {
            var shares = series.Samples[i].ToShares();
            if (!double.IsFinite(shares.Ca) || !double.IsFinite(shares.Cl)) continue;   // only a series built without its verb

            var style = new ShapeStyle(PartColor(series.Colors, i), null, 0);
            double radius = series.MarkerSize / 2;
            Ctx.DrawCircle(Pixel(shares.CationPlace()), radius, style);
            Ctx.DrawCircle(Pixel(shares.AnionPlace()), radius, style);
            Ctx.DrawCircle(Pixel(shares.DiamondPlace()), radius, style);
        }
    }

    /// <summary>One triangle of the diagram: its grid, its outline, its percent ticks and the names of its three corners, given
    /// in the order top, bottom left, bottom right; the two bottom names are aligned as given.</summary>
    private void DrawTriangle(Func<TernaryPoint, Point> place, StrokeStyle grid, ShapeStyle outline, Font tickFont, Font labelFont, string[] names,
        TextAlignment leftAlignment, TextAlignment rightAlignment)
    {
        Ctx.DrawTernaryGrid(place, grid);
        Ctx.DrawPolygon([place(new(1, 0, 0)), place(new(0, 1, 0)), place(new(0, 0, 1))], outline);
        Ctx.DrawTernaryTicks(place, tickFont, fraction => (fraction * 100).ToString("0", CultureInfo.InvariantCulture));

        double belowBase = tickFont.Size + TernaryDrawingExtensions.TickGap + 4 + labelFont.Size;
        var apex = place(new(1, 0, 0));
        var left = place(new(0, 1, 0));
        var right = place(new(0, 0, 1));
        Ctx.DrawText(names[0], new Point(apex.X, apex.Y - 6), labelFont, TextAlignment.Center);
        Ctx.DrawText(names[1], new Point(left.X, left.Y + belowBase), labelFont, leftAlignment);
        Ctx.DrawText(names[2], new Point(right.X, right.Y + belowBase), labelFont, rightAlignment);
    }

    /// <summary>The diamond: its grid (a line at each fifth of A and of B), its outline, and the names of the two upper edges.
    /// A, the calcium and magnesium of the cations, grows toward the left vertex; B, the sulphate and chloride of the anions,
    /// toward the right.</summary>
    private void DrawDiamond(Func<Point, Point> pixel, StrokeStyle grid, ShapeStyle outline, Font labelFont)
    {
        foreach (double f in TernaryDrawingExtensions.Fifths)
        {
            Ctx.DrawLine(pixel(PiperFrame.DiamondAt(f, 0)), pixel(PiperFrame.DiamondAt(f, 1)), grid);   // A = f
            Ctx.DrawLine(pixel(PiperFrame.DiamondAt(0, f)), pixel(PiperFrame.DiamondAt(1, f)), grid);   // B = f
        }

        var bottom = pixel(PiperFrame.DiamondAt(0, 0));
        var right = pixel(PiperFrame.DiamondAt(0, 1));
        var top = pixel(PiperFrame.DiamondAt(1, 1));
        var left = pixel(PiperFrame.DiamondAt(1, 0));
        Ctx.DrawPolygon([bottom, right, top, left], outline);

        Ctx.DrawText("Ca+Mg", new Point((left.X + top.X) / 2 - 8, (left.Y + top.Y) / 2 - 8), labelFont, TextAlignment.Right);
        Ctx.DrawText("SO4+Cl", new Point((right.X + top.X) / 2 + 8, (right.Y + top.Y) / 2 - 8), labelFont, TextAlignment.Left);
    }
}
