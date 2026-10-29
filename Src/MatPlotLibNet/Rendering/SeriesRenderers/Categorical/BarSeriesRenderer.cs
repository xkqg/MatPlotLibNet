// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="BarSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class BarSeriesRenderer : SeriesRenderer<BarSeries>
{
    /// <inheritdoc />
    public BarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>How far a label stands from the end of its bar, in pixels.</summary>
    private const double LabelPad = 4;

    /// <summary>The labels of the bars, each with the point it would stand at: beyond the end of its bar, the way the bar points
    /// in the data. A bar whose value is zero or more points towards the larger values, a bar below zero towards the smaller;
    /// where those lie on the screen depends on whether the axis runs backwards, so a label never stands inside its own bar
    /// because the axis was turned. Above the end the baseline stands the pad above it; below it, the pad and the height the
    /// letters rise above their baseline below it; right of a horizontal bar's end the text starts the pad after it, left of
    /// it the text ends the pad before it.</summary>
    private List<LabelCandidate> LabelCandidates(BarSeries series)
    {
        var candidates = new List<LabelCandidate>();
        if (!series.ShowLabels) return candidates;

        var font = ThemedFontProvider.DataTextFont(Context.Theme);
        bool backwards = Transform.ValueRunsBackwards(series.Orientation);
        foreach (var pixel in series.PixelBars(Transform))
        {
            string label = FormatValue(series.Values[pixel.Index], series.LabelFormat);
            bool towardsHigher = pixel.Rising != backwards;   // the end lies above (right of) the foot on the screen
            if (series.Orientation == BarOrientation.Vertical)
            {
                double centreX = (pixel.From.X + pixel.To.X) / 2;
                double end = pixel.VerticalEnd;
                candidates.Add(new LabelCandidate(
                    new Point(centreX, towardsHigher ? end - LabelPad : end + LabelPad + font.Size), label, font, TextAlignment.Center));
            }
            else
            {
                double centreY = (pixel.From.Y + pixel.To.Y) / 2;
                double end = pixel.HorizontalEnd;
                candidates.Add(towardsHigher
                    ? new LabelCandidate(new Point(end + LabelPad, centreY + 4), label, font, TextAlignment.Left)
                    : new LabelCandidate(new Point(end - LabelPad, centreY + 4), label, font, TextAlignment.Right));
            }
        }
        return candidates;
    }

    /// <inheritdoc />
    internal override void AddMarks(BarSeries series, MarkObstacles.Builder marks)
    {
        foreach (var pixel in series.PixelBars(Transform)) marks.AddBox(pixel.Bar);

        // A label is a text among the marks, where the label layout puts it.
        var font = ThemedFontProvider.DataTextFont(Context.Theme);
        foreach (var placed in PlaceLabels(LabelCandidates(series), Area.PlotBounds))
        {
            marks.AddBox(new TextBox(placed.FinalPoint, Ctx.MeasureText(placed.Text, placed.Font), placed.Font.Size, placed.Alignment,
                TextVerticalAlignment.Baseline, 0).Bounds());
        }
    }

    /// <inheritdoc />
    public override void Render(BarSeries series)
    {
        var baseColor = ResolveColor(series.Color);
        var fillColor = ApplyAlpha(baseColor, series.Alpha);
        double edgeWidth = series.LineWidth > 0 ? series.LineWidth : (series.EdgeColor.HasValue ? 1 : 0);
        Color? edgeColor = series.LineWidth > 0 ? (series.EdgeColor ?? baseColor) : series.EdgeColor;

        foreach (var pixel in series.PixelBars(Transform))
            Ctx.DrawRectangle(pixel.Bar, new ShapeStyle(fillColor, edgeColor, edgeWidth) { Hatch = series.Hatch, HatchColor = series.HatchColor });

        // The labels are placed in one batch after all the bars are drawn, so the layout can shift the few that would
        // otherwise stand on a neighbouring bar's label (dense category axes, narrow bars, long labels).
        DrawPlacedLabels(PlaceLabels(LabelCandidates(series), Area.PlotBounds));
    }

    /// <summary>The label of a bar that names no format: up to six significant digits, trailing zeros dropped, an
    /// exponent only below 1e-4 or from 1e6 — matplotlib's <c>bar_label</c> default, <c>'%g'</c>. .NET's <c>G6</c> picks
    /// the same digits and switches to an exponent at the same place; it writes the exponent as <c>E+06</c> where
    /// <c>'%g'</c> writes <c>e+06</c>, so the marker is lowered.</summary>
    private const string DefaultLabelFormat = "G6";

    private static string FormatValue(double value, string? format) =>
        format is not null
            ? value.ToString(format, CultureInfo.InvariantCulture)
            : value.ToString(DefaultLabelFormat, CultureInfo.InvariantCulture).Replace('E', 'e');
}
