// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
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
        var font = Context.Theme.DataTextFont();
        foreach (var stage in Stages(series))
        {
            marks.AddBox(stage.Bar);
            marks.AddBox(new TextBox(stage.Label, Ctx.ExtentOf(TextLabel.Of(series.Labels[stage.Index]), font)).Bounds());
        }
    }

    /// <summary>One stage of a funnel that can be drawn: its place in the series, its bar and where its name stands (centred on the
    /// middle of its row).</summary>
    private readonly record struct FunnelStage(int Index, Rect Bar, TextPlacement Label);

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
            double x = Range1D.CentredIn(bounds.X, bounds.Width, barW).Lo;
            double y = bounds.Y + i * rowH;
            var bar = new Rect(x, y + 2, barW, rowH - 4);
            if (!bar.IsDrawable()) continue;
            yield return new FunnelStage(i, bar, new TextPlacement(new Point(bounds.Center.X, y + rowH / 2), TextAlignment.Center) { Vertical = TextVerticalAlignment.Center });
        }
    }

    /// <inheritdoc />
    public override void Render(FunnelSeries series)
    {
        double size = Context.Theme.DataTextFont().Size;
        foreach (var stage in Stages(series))
        {
            var ground = PartColor(series.Colors, stage.Index);
            Ctx.DrawRectangle(stage.Bar, ShapeStyle.Filled(ground));
            Ctx.Draw(TextLabel.Of(series.Labels[stage.Index]), Context.Theme.DataTextFontOn(ground, size, GroundInk.Readable), stage.Label);
        }
    }
}
