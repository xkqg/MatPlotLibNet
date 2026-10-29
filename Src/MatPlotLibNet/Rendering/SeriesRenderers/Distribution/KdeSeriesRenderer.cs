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

    /// <summary>The density curve in pixels and, when the series fills, the polygon standing on 0.</summary>
    private readonly record struct KdePixels(List<Point> Line, List<Point>? Fill);

    /// <summary>Evaluates the density and works its pixels out: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend; <see langword="null"/> for a series with no sample.</summary>
    private KdePixels? Pixels(KdeSeries series)
    {
        if (series.Curve() is not { } curve) return null;
        var (xs, density) = curve;

        var points = new List<Point>(xs.Length);
        for (int i = 0; i < xs.Length; i++)
            points.Add(Transform.DataToPixel(xs[i], density[i]));

        List<Point>? fill = null;
        if (series.Fill)
        {
            fill = new List<Point>(points.Count + 2);
            fill.AddRange(points);
            // The fill stands on 0, which a log axis clips to the edge where it begins, as matplotlib clips it.
            fill.Add(Transform.DataToPixelClippedY(xs[^1], 0));
            fill.Add(Transform.DataToPixelClippedY(xs[0], 0));
        }
        return new KdePixels(points, fill);
    }

    /// <inheritdoc />
    public override void Render(KdeSeries series)
    {
        if (Pixels(series) is not { } pixels) return;

        var color = ResolveColor(series.Color);
        if (pixels.Fill is not null)
            Ctx.DrawPolygon(pixels.Fill, new ShapeStyle(ApplyAlpha(color, series.Alpha), null, 0));
        Ctx.DrawLines(pixels.Line, Slot.StrokeOf(color, series.LineWidth, series.LineStyle));
    }
}
