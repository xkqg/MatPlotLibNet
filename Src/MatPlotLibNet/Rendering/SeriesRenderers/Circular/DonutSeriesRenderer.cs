// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="DonutSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class DonutSeriesRenderer : SeriesRenderer<DonutSeries>
{
    /// <inheritdoc />
    public DonutSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(DonutSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(DonutSeries series)
    {
        var bounds = Area.PlotBounds;
        var frame = RoundFrame.In(bounds, RoundFrame.DiscShare);
        double cx = frame.Centre.X, cy = frame.Centre.Y;
        double outerR = frame.Radius;
        double innerR = outerR * series.InnerRadius;
        // A wedge's edge: matplotlib's patch rule under the matplotlib themes (black 1 pt under classic), none otherwise.
        var edges = Context.Theme.PatchEdgesOf(series);
        var edge = edges.Of();
        // A pie's rule: a slice that cannot be drawn (missing, or infinite) counts as 0 in the total and draws no wedge, and a
        // total of nothing or less is no whole to share.
        if (series.Sizes.DrawableTotal() is not { } total) return;
        double angle = series.StartAngleUnder(Context.Theme).ToRadians();
        for (int i = 0; i < series.Sizes.Length; i++)
        {
            if (!series.Sizes[i].IsDrawable()) continue;
            double sweep = series.SweepOf(i, total, Context.Theme);
            var color = PartColor(series.Colors, i);
            // A donut lays its wedges as a pie does (WedgeSeries.SweepOf): as given, else as its theme lays wedges (WedgeLook).
            Ctx.DrawPath(RingWedge.CounterClockwise(frame.Centre, innerR, outerR, angle, angle + sweep).Path(), edges.StyleOf(color, edge));
            angle += sweep;
        }
        if (series.CenterText is not null)
            Ctx.Draw(TextLabel.Of(series.CenterText), Context.Theme.DataTextFont(series.TextSizeUnder(Context.Theme), weight: FontWeight.Bold), new TextPlacement(new Point(cx, cy), TextAlignment.Center) { Vertical = TextVerticalAlignment.Center });
    }
}
