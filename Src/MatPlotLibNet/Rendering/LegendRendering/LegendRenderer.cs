// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>Lays out and draws one legend: a list of entries in a rectangle, under a <see cref="Legend"/> config, a theme and
/// a drawing surface. It holds no axes: which series are entries is decided by whoever owns them
/// (<see cref="SeriesLegendEntry.RowsOf"/> for an axes, <see cref="FigureLegendExtensions"/> for a
/// figure), so an axes and a figure draw their legends by the same rules.</summary>
/// <param name="legend">The legend config: visibility, place, columns, fonts, frame and title.</param>
/// <param name="theme">The theme that supplies the fonts, the background and each entry's colour slot.</param>
/// <param name="ctx">The surface the legend is measured against and drawn onto.</param>
internal sealed class LegendRenderer(Legend legend, Theme theme, IRenderContext ctx)
{
    /// <summary>The place and geometry of the legend of <paramref name="entries"/> in <paramref name="area"/>: the layout
    /// (<see cref="LegendLayoutCalculator"/>) the drawing and the hit-test both read. Only a legend that looks for its place
    /// (<see cref="LegendPosition.Best"/>, after the theme's say) asks for the marks it keeps clear of, and only then.</summary>
    /// <param name="entries">The entries, in the order they are listed.</param>
    /// <param name="area">The rectangle the legend is placed in or beside.</param>
    /// <param name="obstacles">The marks a <see cref="LegendPosition.Best"/> legend keeps clear of; asked for at most once.</param>
    /// <param name="drawnPast">What the axes draws past each edge of <paramref name="area"/>, which a legend outside it stands its gap
    /// past (<see cref="Layout.AxesEdgeNeeds.DrawnPast"/>); 0 on every edge for a legend of the figure.</param>
    /// <returns>The layout; <see langword="null"/> when the legend is hidden or lists nothing.</returns>
    internal LegendLayout? Place(IReadOnlyList<LegendEntry> entries, Rect area, Func<MarkObstacles> obstacles, Layout.Sides<double> drawnPast = default)
    {
        if (!legend.Visible || entries.Count == 0) return null;

        var calculator = new LegendLayoutCalculator(theme, ctx);
        var marks = legend.PositionUnder(theme) == LegendPosition.Best ? obstacles() : MarkObstacles.None;
        return calculator.Compute(legend, entries.Select(e => e.Label).ToList(), area, marks, drawnPast);
    }

    /// <summary>The hit-test bounds of each entry (swatch and label) as <paramref name="layout"/> places it, carrying the index a
    /// click on it toggles (<see cref="LegendEntry.ToggleIndex"/>). The drawing returns the same bounds, so the box a pointer is
    /// tested against is the box drawn.</summary>
    /// <param name="entries">The entries the layout was made for.</param>
    /// <param name="layout">The layout <see cref="Place"/> returned for them.</param>
    /// <returns>One bounds per entry, in entry order.</returns>
    internal static IReadOnlyList<LegendItemBounds> ItemBounds(IReadOnlyList<LegendEntry> entries, LegendLayout layout)
    {
        var bounds = new LegendItemBounds[entries.Count];
        for (int i = 0; i < entries.Count; i++)
            bounds[i] = new LegendItemBounds(entries[i].ToggleIndex, ItemRect(layout, i));

        return bounds;
    }

    /// <summary>The rectangle of entry <paramref name="i"/>: in the column the layout's box gives it (<see cref="LegendLayout.CellOf"/>),
    /// at the top and the height of its own row (<see cref="LegendLayout.Slots"/>).</summary>
    private static Rect ItemRect(LegendLayout layout, int i)
    {
        int column = layout.CellOf(i).Column;
        var slot = layout.Slots[i];
        return new Rect(layout.ColumnX(column), slot.Top, layout.ItemWidth(column), slot.HitHeight);
    }

    /// <summary>Draws the legend in one group (frame, shadow, title and entries inside it, so a dragged legend moves whole):
    /// each entry's swatch, then its text past the swatch.</summary>
    /// <param name="entries">The entries the layout was made for.</param>
    /// <param name="layout">The layout <see cref="Place"/> returned for them.</param>
    /// <param name="interactive">Whether each entry is wrapped in a group carrying the index it toggles
    /// (<c>data-legend-index</c>), which the legend toggle reads; only an SVG surface writes one.</param>
    /// <returns>The hit-test bounds of each entry (<see cref="ItemBounds"/>).</returns>
    internal IReadOnlyList<LegendItemBounds> Draw(IReadOnlyList<LegendEntry> entries, LegendLayout layout, bool interactive)
    {
        // Open the legend group FIRST: the legend-drag script translates this <g class="legend">, so the frame must be
        // inside it to move with the items it frames.
        ctx.BeginAccessibleGroup("legend", "Chart legend");

        DrawFrame(layout);

        // The title centred over the entries, its baseline where the box's title band puts it
        if (!string.IsNullOrEmpty(legend.Title))
        {
            ctx.Draw(TextLabel.Of(legend.Title), layout.TitleFont, new TextPlacement(new Point(layout.ColumnX(0) + layout.TotalContentWidth / 2, layout.BoxY + layout.Padding + layout.Title.Baseline), TextAlignment.Center));
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var cell = ItemRect(layout, i);

            // The group carries the index of what the entry stands for (data-legend-index maps straight onto
            // data-series-index), never the entry's place in the legend: an unlabelled series ahead of this one has no entry but
            // still has an index.
            bool grouped = interactive && ctx.BeginLegendItemGroup(entry.ToggleIndex, entry.Label);

            // The handle and the text where the entry's own row puts them (LegendBox.RowOf)
            var row = layout.Slots[i].Row;
            entry.DrawSwatch(this, new Rect(cell.X, cell.Y + row.HandleTop, layout.SwatchWidth, layout.SwatchHeight), layout.Font.Size);

            var textPoint = new Point(cell.X + layout.SwatchWidth + layout.SwatchGap, cell.Y + row.Baseline);
            ctx.Draw(TextLabel.Of(entry.Label), layout.Font, new TextPlacement(textPoint, TextAlignment.Left));
            if (grouped) ctx.EndGroup();
        }

        ctx.EndGroup();
        return ItemBounds(entries, layout);
    }

    /// <summary>Draws the handle of a series' row in <paramref name="swatch"/>: the one the series says it has, in the colour of the
    /// entry's COLOUR slot, so a series sharing a colour group shows the colour it is drawn in, or in the colour of the part the row
    /// stands for when its caller named one.</summary>
    /// <param name="entry">The series' row.</param>
    /// <param name="swatch">The handle's box.</param>
    /// <param name="fontSize">The size of the legend's text, which the spacing of the markers of a handle is a share of.</param>
    internal void DrawSwatchOf(SeriesLegendEntry entry, Rect swatch, double fontSize)
    {
        var slot = entry.SlotUnder(theme);
        DrawSwatch(entry.Series, entry.Swatch, swatch.X, swatch.Y, swatch.Width, swatch.Height, fontSize, slot, entry.HandleColorIn(slot));
    }

    /// <summary>Draws the handle of a patch's row in <paramref name="swatch"/>, as matplotlib's <c>HandlerPatch</c> does: a rectangle
    /// filling the box, in the style the patch is drawn in (<see cref="Models.Patches.Patch.DrawnStyleUnder"/>), whatever its shape.</summary>
    /// <param name="entry">The patch's row.</param>
    /// <param name="swatch">The handle's box.</param>
    internal void DrawSwatchOf(PatchLegendEntry entry, Rect swatch) => ctx.DrawRectangle(swatch, entry.Patch.DrawnStyleUnder(theme));

    /// <summary>The frame: a box in the legend's face colour at the opacity the legend or its theme names, outlined in the colour they
    /// name (<see cref="LegendLookExtensions"/>), as wide as the theme's look says, with rounded corners where they are asked for
    /// (matplotlib's <c>fancybox</c>: a radius of 0.2 of the text's size), and an optional drop shadow beneath it in the look's colour
    /// and offset. It is a patch with a mitred edge, asked of the surface's pixel grid as a bar is (matplotlib's frame is snapped); a
    /// surface with no grid, which every surface is under the library's own themes, draws it where it stands.</summary>
    private void DrawFrame(LegendLayout layout)
    {
        if (!legend.FrameOn) return;

        var look = theme.Looks.Legend;

        if (legend.Shadow)
            ctx.DrawRectangle(new Rect(layout.BoxX + look.ShadowOffset, layout.BoxY + look.ShadowOffset, layout.BoxWidth, layout.BoxHeight),
                ShapeStyle.Filled(look.ShadowColor));

        var box = new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight);
        var style = ShapeStyle.Edged(legend.FillColorUnder(theme), legend.EdgeColorUnder(theme), look.FrameWidth);
        if (legend.FancyBoxUnder(theme))
            CalloutShape.Of(BoxStyle.Round).Draw(ctx, ctx.OnPixelGrid(box, style), FancyBoxRounding * layout.Font.Size, style);
        else
            ctx.DrawRectangleOnPixelGrid(box, style);
    }

    /// <summary>The radius of a rounded frame corner as a share of the legend's text size: matplotlib's
    /// <c>FancyBboxPatch(boxstyle="round,rounding_size=0.2", mutation_scale=fontsize)</c>.</summary>
    private const double FancyBoxRounding = 0.2;

    /// <summary>Draws the legend handle a series says it has (<see cref="ChartSeries.LegendSwatch"/>) at (x, y):
    /// a line segment for a line, a single marker for a scatter (a polar and a 3-D one included), a line with caps for an error bar, stripes for the states of
    /// a timeline, a filled rectangle for patches. The colour-cycle slot decides what the series leaves to it (its colour, and
    /// the width, dash and marker it names no value for), the same rules the series renderer strokes the series by.</summary>
    private void DrawSwatch(ISeries series, LegendSwatch swatch, double x, double y, double wMax, double h, double fontSize, SeriesSlot slot,
        Color color)
    {
        double midY = y + h / 2;
        int numPoints = legend.NumPointsUnder(theme);

        switch (swatch.Kind)
        {
            case LegendSwatchKind.Line:
            {
                var (from, to) = LegendLook.LineSpan(numPoints, wMax, fontSize);
                DrawSwatchLine(new Point(x + from, midY), new Point(x + to, midY),
                    slot.StrokeOf(color, swatch.Stroke, swatch.LineRule));
                break;
            }
            case LegendSwatchKind.LineMarker:
            {
                // matplotlib's HandlerLine2D: the line runs the handle, with one marker in its middle, or runs from the first of
                // several markers to the last (numpoints).
                var (from, to) = LegendLook.LineSpan(numPoints, wMax, fontSize);
                DrawSwatchLine(new Point(x + from, midY), new Point(x + to, midY),
                    slot.StrokeOf(color, swatch.Stroke, swatch.LineRule));
                if (slot.LineMarkerOf(swatch.Marker) is { } marker and not MarkerStyle.None)
                {
                    double size = LegendLook.MarkerSizeIn(swatch.MarkerSize ?? 0, h);
                    var look = slot.LineMarkerStyleOf(marker, color, swatch.MarkerFace, swatch.MarkerEdge, swatch.MarkerEdgeWidth);
                    foreach (double stop in LegendLook.MarkerStops(numPoints, wMax, fontSize))
                        DrawSwatchMarker(x + stop, midY, marker, size, look);
                }
                break;
            }
            case LegendSwatchKind.SolidLine:
            {
                // solid, as the series itself: the slot decides only the width
                var (from, to) = LegendLook.LineSpan(numPoints, wMax, fontSize);
                ctx.DrawLine(new Point(x + from, midY), new Point(x + to, midY),
                    slot.StrokeOf(color, GivenStroke.Solid(swatch.Stroke.Width), swatch.LineRule));
                break;
            }
            case LegendSwatchKind.Marker:
                // The handle shows the series' own size (matplotlib's HandlerPathCollection at legend.markerscale 1), through the one
                // rule of a marker's size in a handle (LegendLook.MarkerSizeIn: a point at least, the handle's height at most).
                DrawScatterHandle(x, y, wMax, h, fontSize, slot.ScatterMarkerOf(swatch.Marker),
                    LegendLook.MarkerSizeIn(swatch.MarkerSize ?? 0, h), ShapeStyle.Filled(color));
                break;
            case LegendSwatchKind.Scatter:
            {
                // The same, in the size and with the edge the theme gives a scatter marker (the plot's own).
                var shape = slot.ScatterMarkerOf(swatch.Marker);
                DrawScatterHandle(x, y, wMax, h, fontSize, shape, LegendLook.MarkerSizeIn(slot.ScatterSizeOf(swatch.MarkerSize), h), ScatterHandleStyle(slot, shape, color));
                break;
            }
            case LegendSwatchKind.Scatter3D:
            {
                // A 3-D scatter point is the marker the series names at the 3-D size; the handle shows the full size (the plot draws
                // the nearest point so).
                var shape = swatch.Marker ?? MarkerStyle.Circle;
                DrawScatterHandle(x, y, wMax, h, fontSize, shape,
                    LegendLook.MarkerSizeIn(slot.Scatter3DSizeOf(swatch.MarkerSize), h), ScatterHandleStyle(slot, shape, color));
                break;
            }
            case LegendSwatchKind.ErrorBar:
            {
                double width = slot.LineWidthOf(swatch.Stroke.Width, swatch.LineRule);
                // The caps are the plot's where the theme names a cap size (matplotlib's errorbar.capsize: none under v2); 3 pixels under the
                // library's own themes.
                double cap = theme.Looks.Lines.ErrorBarCapSize is null ? 3 : theme.Looks.Lines.CapSizeOver(swatch.CapSize, 3);
                ctx.DrawLine(new Point(x + 2, midY), new Point(x + wMax - 2, midY), StrokeStyle.Solid(color, width));
                if (cap > 0)
                {
                    ctx.DrawLine(new Point(x + 2, midY - cap), new Point(x + 2, midY + cap), StrokeStyle.Solid(color, width));
                    ctx.DrawLine(new Point(x + wMax - 2, midY - cap), new Point(x + wMax - 2, midY + cap), StrokeStyle.Solid(color, width));
                }
                break;
            }
            case LegendSwatchKind.Stripes when (swatch.States ?? []).DistinctFills(theme) is { Count: > 0 } fills:
            {
                // One stripe for each distinct state, side by side across the swatch, each in the fill the series draws it in.
                double stripeWidth = wMax / fills.Count;
                for (int i = 0; i < fills.Count; i++)
                    ctx.DrawRectangle(new Rect(x + i * stripeWidth, y, stripeWidth, h), fills[i]);
                break;
            }
            case LegendSwatchKind.EdgedPatch when series is IEdgedPatch { GivenEdge: var given } && theme.Looks.Patch.EdgeOver(given) is { } edge:
                // A patch under the matplotlib themes: the rectangle filled and edged as its patches are (HandlerPatch and
                // HandlerPolyCollection copy the face, the edge and the opacity), from the one edge and opacity the series gives them.
                ctx.DrawRectangle(new Rect(x, y, wMax, h), edge.StyleOf(color.WithOpacityGiven(given.Alpha)));
                break;
            default:
                // Patch-like: bar/hist/area/violin/pie/etc → a filled RECTANGLE, matplotlib's handlelength × handleheight
                // (wMax carries the length, h the height), never clamped to a square.
                ctx.DrawRectangle(new Rect(x, y, wMax, h), ShapeStyle.Filled(color));
                break;
        }
    }

    /// <summary>The style a scatter handle's marker is drawn with: the theme's size-independent fill and edge of a scatter marker, or the
    /// fill alone for a cross and a plus, which are strokes in the fill colour (the plot draws them so).</summary>
    private static ShapeStyle ScatterHandleStyle(SeriesSlot slot, MarkerStyle shape, Color color) =>
        MarkerShape.Of(shape).TakesAnEdge ? slot.ScatterStyleOf(color) : ShapeStyle.Filled(color);

    /// <summary>Draws the markers of the handle of a scatter in the box at (<paramref name="x"/>, <paramref name="y"/>), where the legend and its
    /// theme say (<see cref="LegendLookExtensions"/>' <c>ScatterHandleUnder</c>): spread as matplotlib's <c>HandlerPathCollection</c>
    /// places them, a single one in the middle, several along the handle, each at its own height (<see cref="LegendLook.MarkerStops"/>,
    /// <see cref="LegendLook.ScatterHeights"/>); or the one marker where the library has always drawn it, half the handle's height from
    /// its start.</summary>
    private void DrawScatterHandle(double x, double y, double width, double height, double fontSize, MarkerStyle shape, double size, ShapeStyle style)
    {
        if (legend.ScatterHandleUnder(theme) == LegendScatterHandle.AtHandleStart)
        {
            DrawSwatchMarker(x + height / 2, y + height / 2, shape, size, style);
            return;
        }

        int count = legend.ScatterPointsUnder(theme);
        var stops = LegendLook.MarkerStops(count, width, fontSize);
        var heights = LegendLook.ScatterHeights(count);
        for (int i = 0; i < stops.Length; i++)
            DrawSwatchMarker(x + stops[i], y + height * (1 - heights[i]), shape, size, style);
    }

    /// <summary>Draws the line of a legend handle from <paramref name="from"/> to <paramref name="to"/>: the very stroke the
    /// series is drawn with, so a series whose line is <see cref="LineStyle.None"/> has none in its legend entry either, only
    /// its marker.</summary>
    private void DrawSwatchLine(Point from, Point to, StrokeStyle stroke)
    {
        if (stroke.Style != LineStyle.None)
            ctx.DrawLine(from, to, stroke);
    }

    /// <summary>Draws a single legend marker at (cx, cy) in the shape the plot draws it in: the one owner of marker
    /// shapes (<see cref="Rendering.LegendRendering.LegendRenderer"/>) draws it, so a triangle, a star or a cross in the plot is the same in the
    /// legend, and <see cref="MarkerStyle.None"/> draws nothing. <paramref name="size"/> is the marker's size across, in pixels,
    /// and <paramref name="style"/> its fill and edge.</summary>
    private void DrawSwatchMarker(double cx, double cy, MarkerStyle marker, double size, ShapeStyle style) =>
        DrawMarker(ctx, marker, new Point(cx, cy), size, style, theme);

    /// <summary>Draws a single marker at <paramref name="center"/>.</summary>
    /// <param name="style">Shape to draw.</param>
    /// <param name="center">Centre of the marker in pixel space.</param>
    /// <param name="size">Full bounding-box edge / diameter in pixels.</param>
    /// <param name="shape">Fill/stroke/stroke-width. For Cross / Plus, <c>Fill</c> is used as the stroke
    /// colour (those shapes have no fill) and a zero <c>StrokeThickness</c> falls back to <c>size / 8</c>.</param>
    /// <param name="theme">The theme the marker is drawn under, whose shapes it takes (<see cref="Theme.Default"/>: the library's
    /// own).</param>
    /// <param name="ctx">The <see cref="IRenderContext"/> it works on.</param>
    internal static void DrawMarker(IRenderContext ctx, MarkerStyle style, Point center, double size, ShapeStyle shape, Theme theme) =>
        MarkerShape.Of(style, theme).Draw(ctx, center, size, shape);
}
