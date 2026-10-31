// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>Lays out and draws one legend: a list of entries in a rectangle, under a <see cref="Legend"/> config, a theme and
/// a drawing surface. It holds no axes: which series are entries is decided by whoever owns them
/// (<see cref="AxesLegendEntryExtensions.LegendEntries(Axes, bool)"/> for an axes, <see cref="FigureLegendExtensions"/> for a
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
    /// <returns>The layout; <see langword="null"/> when the legend is hidden or lists nothing.</returns>
    internal LegendLayout? Place(IReadOnlyList<LegendEntry> entries, Rect area, Func<MarkObstacles> obstacles)
    {
        if (!legend.Visible || entries.Count == 0) return null;

        var calculator = new LegendLayoutCalculator(theme, ctx);
        var marks = calculator.PositionOf(legend) == LegendPosition.Best ? obstacles() : MarkObstacles.None;
        return calculator.Compute(legend, entries.Select(e => e.Label).ToList(), area, marks);
    }

    /// <summary>The hit-test bounds of each entry (swatch and label) as <paramref name="layout"/> places it, carrying the
    /// entry's series index. The drawing returns the same bounds, so the box a pointer is tested against is the box drawn.</summary>
    /// <param name="entries">The entries the layout was made for.</param>
    /// <param name="layout">The layout <see cref="Place"/> returned for them.</param>
    /// <returns>One bounds per entry, in entry order.</returns>
    internal static IReadOnlyList<LegendItemBounds> ItemBounds(IReadOnlyList<LegendEntry> entries, LegendLayout layout) =>
        [.. entries.Select((entry, i) => new LegendItemBounds(entry.OriginalIndex, ItemRect(layout, i)))];

    /// <summary>The rectangle of entry <paramref name="i"/>: entries fill the columns row by row.</summary>
    private static Rect ItemRect(LegendLayout layout, int i)
    {
        int col = i % layout.NCols;
        return new Rect(layout.ColumnX(col), layout.EntryY(i / layout.NCols), layout.ItemWidth(col), layout.LineHeight);
    }

    /// <summary>Draws the legend in one group (frame, shadow, title and entries inside it, so a dragged legend moves whole):
    /// each entry's swatch, then its text past the swatch.</summary>
    /// <param name="entries">The entries the layout was made for.</param>
    /// <param name="layout">The layout <see cref="Place"/> returned for them.</param>
    /// <param name="interactive">Whether each entry is wrapped in a group carrying its series index
    /// (<c>data-legend-index</c>), which the legend toggle reads; only an SVG surface writes one.</param>
    /// <returns>The hit-test bounds of each entry (<see cref="ItemBounds"/>).</returns>
    internal IReadOnlyList<LegendItemBounds> Draw(IReadOnlyList<LegendEntry> entries, LegendLayout layout, bool interactive)
    {
        // Open the legend group FIRST: the legend-drag script translates this <g class="legend">, so the frame must be
        // inside it to move with the items it frames.
        if (ctx is SvgRenderContext svgLegendGroup)
            svgLegendGroup.BeginAccessibleGroup("legend", "Chart legend");
        else
            ctx.BeginGroup("legend");

        DrawFrame(layout);

        if (!string.IsNullOrEmpty(legend.Title))
        {
            ctx.DrawText(legend.Title,
                new Point(layout.BoxX + layout.Padding + layout.TotalContentWidth / 2, layout.BoxY + layout.Padding + layout.TitleFont.Size),
                layout.TitleFont, TextAlignment.Center);
        }

        var bounds = ItemBounds(entries, layout);
        var svgItems = interactive ? ctx as SvgRenderContext : null;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var cell = bounds[i].Bounds;

            // The group carries the SERIES index (data-legend-index maps straight onto data-series-index), never the
            // entry's position in the legend: an unlabelled series ahead of this one has no entry but still has an index.
            svgItems?.BeginLegendItemGroup(entry.OriginalIndex, entry.Label);

            // The swatch reads the entry's COLOUR slot, so a series sharing a colour group shows the colour it is drawn in.
            DrawSwatch(entry.Series, entry.Swatch, cell.X, cell.Y, layout.SwatchWidth, layout.SwatchHeight,
                theme.SeriesSlot(entry.ColorSlotIndex));

            var textPoint = new Point(cell.X + layout.SwatchWidth + layout.SwatchGap, cell.Y + layout.SwatchHeight - 1);
            if (MathTextParser.ContainsMath(entry.Label))
                ctx.DrawRichText(MathTextParser.Parse(entry.Label), textPoint, layout.Font, TextAlignment.Left);
            else
                ctx.DrawText(entry.Label, textPoint, layout.Font, TextAlignment.Left);
            if (svgItems is not null) ctx.EndGroup();
        }

        ctx.EndGroup();
        return bounds;
    }

    /// <summary>The frame: a translucent box in the legend's face colour (the theme background unless named), outlined in
    /// light grey (matplotlib's <c>legend.edgecolor = '0.8'</c>) unless named, with an optional drop shadow beneath it.</summary>
    private void DrawFrame(LegendLayout layout)
    {
        if (!legend.FrameOn) return;

        var faceColor = legend.FaceColor ?? theme.Background;
        var bgColor = faceColor.WithAlpha((byte)(legend.FrameAlpha * 255));
        var edgeColor = legend.EdgeColor ?? Color.FromHex("#CCCCCC");

        if (legend.Shadow)
            ctx.DrawRectangle(new Rect(layout.BoxX + 3, layout.BoxY + 3, layout.BoxWidth, layout.BoxHeight),
                new ShapeStyle(new Color(0, 0, 0, 80), null, 0));

        // FancyBox (rounded corners) is kept in the model but the frame is drawn square: IRenderContext has no corner radius.
        ctx.DrawRectangle(new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight), new ShapeStyle(bgColor, edgeColor, 0.5));
    }

    /// <summary>Draws the legend handle a series says it has (<see cref="ChartSeries.LegendSwatch"/>) at (x, y):
    /// a line segment for a line, a single marker for a scatter, a line with caps for an error bar, stripes for the states of
    /// a timeline, a filled rectangle for patches. The colour-cycle slot decides what the series leaves to it (its colour, and
    /// the width, dash and marker it names no value for), the same rules the series renderer strokes the series by.</summary>
    private void DrawSwatch(ISeries series, LegendSwatch swatch, double x, double y, double wMax, double h, SeriesSlot slot)
    {
        double midY = y + h / 2;
        var color = slot.ColorOf(series);

        switch (swatch.Kind)
        {
            case LegendSwatchKind.Line:
                DrawSwatchLine(new Point(x, midY), new Point(x + wMax, midY),
                    slot.StrokeOf(color, swatch.LineWidth, swatch.LineStyle, swatch.RestingLineWidth));
                break;
            case LegendSwatchKind.LineMarker:
                DrawSwatchLine(new Point(x, midY), new Point(x + wMax, midY),
                    slot.StrokeOf(color, swatch.LineWidth, swatch.LineStyle, swatch.RestingLineWidth));
                if (slot.LineMarkerOf(swatch.Marker) is { } marker and not MarkerStyle.None)
                    DrawSwatchMarker(x + wMax / 2, midY, marker, swatch.MarkerSize, color);
                break;
            case LegendSwatchKind.SolidLine:
                // solid, as the series itself: the slot decides only the width
                ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY),
                    new StrokeStyle(color, slot.LineWidthOf(swatch.LineWidth), LineStyle.Solid));
                break;
            case LegendSwatchKind.Marker:
            {
                // matplotlib legend marker size ≈ series point size, scaled down slightly.
                double radius = Math.Min(h / 2 - 1, Math.Sqrt(swatch.MarkerSize / Math.PI) * (100.0 / 72.0));
                DrawSwatchMarker(x + h / 2, midY, slot.ScatterMarkerOf(swatch.Marker ?? SeriesSlot.RestingScatterMarker), radius * radius * Math.PI, color);
                break;
            }
            case LegendSwatchKind.ErrorBar:
            {
                double width = slot.LineWidthOf(swatch.LineWidth);
                ctx.DrawLine(new Point(x + 2, midY), new Point(x + wMax - 2, midY), new StrokeStyle(color, width, LineStyle.Solid));
                ctx.DrawLine(new Point(x + 2, midY - 3), new Point(x + 2, midY + 3), new StrokeStyle(color, width, LineStyle.Solid));
                ctx.DrawLine(new Point(x + wMax - 2, midY - 3), new Point(x + wMax - 2, midY + 3), new StrokeStyle(color, width, LineStyle.Solid));
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
            default:
                // Patch-like: bar/hist/area/violin/pie/etc → a filled RECTANGLE, matplotlib's handlelength × handleheight
                // (wMax carries the length, h the height), never clamped to a square.
                ctx.DrawRectangle(new Rect(x, y, wMax, h), new ShapeStyle(color, null, 0));
                break;
        }
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
    /// shapes (<see cref="MarkerRenderer"/>) draws it, so a triangle, a star or a cross in the plot is the same in the
    /// legend, and <see cref="MarkerStyle.None"/> draws nothing.</summary>
    private void DrawSwatchMarker(double cx, double cy, MarkerStyle marker, double sizePt2, Color color)
    {
        double radius = Math.Sqrt(Math.Max(sizePt2, 1) / Math.PI) * (100.0 / 72.0);
        ctx.DrawMarker(marker, new Point(cx, cy), 2 * radius, new ShapeStyle(color, null, 0));
    }
}
