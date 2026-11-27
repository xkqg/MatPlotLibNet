// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StemSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class StemSeriesRenderer : SeriesRenderer<StemSeries>
{
    /// <inheritdoc />
    public StemSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(StemSeries series, MarkObstacles.Builder marks)
    {
        foreach (var stem in Stems(series))
        {
            marks.AddPoint(stem.Head);
            if (stem.Foot is { } foot) marks.AddPath([foot, stem.Head]);
        }
    }

    /// <summary>How large a stem's head is drawn, in pixels across, under a theme that draws no line markers of its own.</summary>
    private const double HeadSize = 8;

    /// <summary>One stem on the plot: the head and, when the stem can be drawn, where it starts.</summary>
    private readonly record struct StemPixels(Point Head, Point? Foot);

    /// <summary>The stems that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a
    /// legend. A stem whose head the axes cannot place is left out whole, as matplotlib's head a thousand decades beyond a log
    /// axis is never seen. A stem whose foot (the baseline 0) a log axis cannot place rises from the edge where the axis begins:
    /// matplotlib places that foot a thousand decades below the axis and the plot cuts the stem at the edge
    /// (<see cref="DataTransform.CutSegment"/>). A foot that cannot be placed at all (a logit axis masks 0) leaves the head
    /// alone.</summary>
    private List<StemPixels> Stems(StemSeries series)
    {
        var stems = new List<StemPixels>(series.XData.Length);
        for (int i = 0; i < series.XData.Length; i++)
        {
            var head = Transform.DataToPixel(series.XData[i], series.YData[i]);
            if (!head.IsDrawable()) continue;
            var stem = Transform.CutSegment(Transform.DataToPixelUnclipped(series.XData[i], 0), head);
            stems.Add(new StemPixels(head, stem?.From));
        }
        return stems;
    }

    /// <inheritdoc />
    /// <remarks>Drawn in matplotlib's order: every stem (its <c>LineCollection</c>), then every head (a <c>Line2D</c>'s markers),
    /// then the baseline (a <c>Line2D</c>), so the baseline lies over a head at 0. The stems and the baseline are as wide as the
    /// theme's lines (<c>lines.linewidth</c>) and stand on the pixel grid of a raster export; a head is a line's marker
    /// (<c>lines.markersize</c> across, outlined as the theme outlines one) where the theme draws line markers of its own.</remarks>
    public override void Render(StemSeries series)
    {
        var theme = Context.Theme;
        var stems = Stems(series);
        double width = Slot.LineWidthOf(LineWidthRule.OnePixelCollection);

        var stemStroke = StrokeStyle.Solid(series.StemColor ?? SeriesColor, width);
        foreach (var stem in stems)
        {
            if (stem.Foot is not { } foot) continue;
            // vlines draws a LineCollection of vertical segments, which Agg puts on the pixel grid, with butt ends.
            var drawn = Ctx.OnPixelGrid(new PixelSegment(foot, stem.Head), width, projectingEnds: false);
            Ctx.DrawLine(drawn.From, drawn.To, stemStroke);
        }

        var markerColor = series.MarkerColor ?? SeriesColor;
        var head = MarkerShape.Of(series.Marker, theme);
        var headStyle = Slot.LineMarkerStyleOf(series.Marker, markerColor, null, null, null);
        double headSize = Context.Theme.Looks.Markers.LineEdge is null ? HeadSize : LineSeries.RestingMarkerSize.PointsInPixels;
        foreach (var stem in stems) head.Draw(Ctx, stem.Head, headSize, headStyle);

        // The baseline runs at 0 under the stem locations the X axis can place, when the Y axis can place 0.
        if (series.XData.DrawableRange(Transform.XRule) is { } span)
        {
            var left = Transform.DataToPixel(span.Lo, 0);
            var right = Transform.DataToPixel(span.Hi, 0);
            if (left.CanDrawLineTo(right))
            {
                // A Line2D of two points, which Agg snaps; its solid lines end projecting.
                var drawn = Ctx.OnPixelGrid(new PixelSegment(left, right), width, projectingEnds: true);
                Ctx.DrawLine(drawn.From, drawn.To, StrokeStyle.Solid(series.BaselineColorIn(theme), width));
            }
        }
    }
}
