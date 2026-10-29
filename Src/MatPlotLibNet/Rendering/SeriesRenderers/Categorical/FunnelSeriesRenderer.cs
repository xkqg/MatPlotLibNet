// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="FunnelSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class FunnelSeriesRenderer : SeriesRenderer<FunnelSeries>
{
    /// <inheritdoc />
    public FunnelSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(FunnelSeries series, MarkObstacles.Builder marks)
    {
        var font = ThemedFontProvider.DataTextFont(Context.Theme);
        foreach (var stage in Stages(series))
        {
            marks.AddBox(stage.Bar);
            marks.AddBox(new TextBox(stage.Label, Ctx.MeasureText(series.Labels[stage.Index], font), font.Size,
                TextAlignment.Center, TextVerticalAlignment.Baseline, 0).Bounds());
        }
    }

    /// <summary>One stage of a funnel that can be drawn: its place in the series, its bar and where its name stands.</summary>
    private readonly record struct FunnelStage(int Index, Rect Bar, Point Label);

    /// <summary>The stages that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a
    /// legend. Scaled to the widest stage that can be drawn; with none (no stage at all, or none that can be drawn) the plot
    /// stays empty. A stage that cannot be drawn is skipped with its label; the stages around it keep their rows.</summary>
    private IEnumerable<FunnelStage> Stages(FunnelSeries series)
    {
        if (series.Values.DrawableRange() is not { } range || range.Hi <= 0) yield break;
        double maxVal = range.Hi;
        var bounds = Area.PlotBounds;
        double rowH = bounds.Height / series.Values.Length;
        for (int i = 0; i < series.Values.Length; i++)
        {
            double frac = series.Values[i] / maxVal;
            double barW = bounds.Width * frac;
            double x = bounds.X + (bounds.Width - barW) / 2;
            double y = bounds.Y + i * rowH;
            var bar = new Rect(x, y + 2, barW, rowH - 4);
            if (!bar.IsDrawable()) continue;
            yield return new FunnelStage(i, bar, new Point(bounds.X + bounds.Width / 2, y + rowH / 2 + 4));
        }
    }

    /// <inheritdoc />
    public override void Render(FunnelSeries series)
    {
        foreach (var stage in Stages(series))
        {
            Ctx.DrawRectangle(stage.Bar, new ShapeStyle(PartColor(series.Colors, stage.Index), null, 0));
            Ctx.DrawText(series.Labels[stage.Index], stage.Label, ThemedFontProvider.DataTextFont(Context.Theme), TextAlignment.Center);
        }
    }
}
