// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="KdeSeries"/> as a smooth Gaussian KDE density curve with optional fill.</summary>
internal sealed class KdeSeriesRenderer : SeriesRenderer<KdeSeries>
{
    /// <inheritdoc />
    public KdeSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(KdeSeries series, MarkObstacles.Builder marks)
    {
        if (Pixels(series) is not { } pixels) return;
        AddPolyline(marks, pixels.Line);
        if (pixels.Fill is not null) marks.AddPath(pixels.Fill);
    }

    /// <summary>The density curve in pixels and, when the series fills, the polygon standing on 0, each cut where it leaves the
    /// plot towards a value a log axis cannot place.</summary>
    private readonly record struct KdePixels(IReadOnlyList<Point> Line, IReadOnlyList<Point>? Fill);

    /// <summary>Evaluates the density and works its pixels out: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend; <see langword="null"/> for a series with no sample.</summary>
    private KdePixels? Pixels(KdeSeries series)
    {
        if (series.CurveUnder(Context.Theme.Looks) is not { } curve) return null;
        var (xs, density) = curve;

        var points = new List<Point>(xs.Length);
        for (int i = 0; i < xs.Length; i++)
            points.Add(Transform.DataToPixelUnclipped(xs[i], density[i]));

        IReadOnlyList<Point>? fill = null;
        if (series.FillsUnder(Context.Theme))
        {
            var outline = new List<Point>(points.Count + 2);
            outline.AddRange(points);
            // The fill stands on 0, which a log axis clips to the edge where it begins, as matplotlib clips it.
            outline.Add(Base(xs[^1]));
            outline.Add(Base(xs[0]));
            // A fill with a corner the axes cannot place (a density above 1 on a logit axis, or its base 0 there, both masked)
            // is not filled.
            if (outline.IsDrawable()) fill = Transform.CutBeyondTheStart(outline, closed: true);
        }
        return new KdePixels(Transform.CutBeyondTheStart(points, closed: false), fill);
    }

    /// <summary>The corner of the fill under <paramref name="x"/>: on 0, clipped to the edge where a log Y axis begins, at the
    /// <paramref name="x"/> the curve stands at (beyond a log X axis when it cannot place it, for the cut to take it off).</summary>
    private Point Base(double x) => Transform.DataToPixelClippedY(x, 0) with { X = Transform.DataToPixelUnclipped(x, 0).X };

    /// <inheritdoc />
    public override void Render(KdeSeries series)
    {
        if (Pixels(series) is not { } pixels) return;

        var color = ColorOf(series);
        if (pixels.Fill is not null)
            Ctx.DrawPolygon(pixels.Fill, ShapeStyle.Filled(color.WithOpacityGiven(series.Alpha)));
        DrawSeriesLine(pixels.Line, series, color);
    }
}
