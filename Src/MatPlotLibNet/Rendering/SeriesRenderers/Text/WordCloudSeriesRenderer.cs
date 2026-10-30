// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="WordCloudSeries"/>: the boxes <see cref="WordCloudLayout"/> packs into the plot area, each word
/// drawn at its box, heaviest first, in a colour that can be read against the background. Every word carries its weight as a
/// title and says how wide it is to be set, so a viewer's own font cannot make it run into its neighbour. A word that found no
/// room is announced on <see cref="ChartDiagnostics"/>, lightest first.</summary>
internal sealed class WordCloudSeriesRenderer : SeriesRenderer<WordCloudSeries>
{
    /// <summary>The baseline of a word, as a share of the height of its box from the top: the share of the box the letters stand on.</summary>
    private const double Baseline = 0.8;

    /// <summary>How many unplaced words an announcement names before it says how many more there are.</summary>
    private const int NamedInAnnouncement = 8;

    /// <inheritdoc />
    public WordCloudSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(WordCloudSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(WordCloudSeries series)
    {
        if (series.Words.Length == 0) return;

        var baseFont = ThemedFontProvider.LabelFont(Context.Theme);
        var result = WordCloudLayout.Compute(
            new WordCloudSpec(series.Words, series.Weights, series.Sizes, baseFont), Area.PlotBounds, FontMetricsDefault.Current);
        Announce(result);

        var background = Context.Theme.AxesBackground;
        foreach (int i in series.ReadingOrder())
        {
            var placement = result.Words[i];
            if (!placement.Placed) continue;

            var colour = Readable(PartColor(series.Colors, i), background, placement.Size);
            var at = new Point(placement.Box.X, placement.Box.Y + placement.Box.Height * Baseline);

            // The weight is the title of the word, whether or not tooltips are on: size alone would be the only way to read it.
            var svg = Ctx as SvgRenderContext;
            svg?.BeginTooltipGroup($"{placement.Word}: {placement.Weight.ToString("G", CultureInfo.InvariantCulture)}");
            Ctx.SetNextTextLength(placement.Box.Width);
            Ctx.DrawText(placement.Word, at, baseFont with { Size = placement.Size, Color = colour }, TextAlignment.Left);
            svg?.EndTooltipGroup();
        }
    }

    /// <summary>Says which words found no room: a cloud that quietly leaves words out reads as a cloud of fewer words.</summary>
    private static void Announce(WordCloudResult result)
    {
        if (result.Unplaced == 0) return;

        var left = result.Words.Where(word => !word.Placed).OrderBy(word => word.Weight).ThenBy(word => word.Word, StringComparer.Ordinal).ToArray();
        string named = string.Join(", ", left.Take(NamedInAnnouncement).Select(word => word.Word));
        string more = left.Length > NamedInAnnouncement ? $" and {left.Length - NamedInAnnouncement} more" : "";
        ChartDiagnostics.Emit(new ChartDiagnostic(
            nameof(WordCloudSeries),
            $"{result.Unplaced} of {result.Words.Count} words did not fit in the plot and were not drawn, even at the smallest size: {named}{more}.",
            null));
    }

    /// <summary>The colour, moved toward the text colour until it can be read against the background: 4.5 to 1 for text under 24
    /// points, 3 to 1 for larger text (WCAG 1.4.3). The colours of a theme's cycle are chosen to tell series apart, and some of them
    /// are too pale to read as small text.</summary>
    private Color Readable(Color colour, Color background, double size)
    {
        double needed = size < 24 ? 4.5 : 3.0;
        var ink = Context.Theme.ForegroundText;
        for (int step = 0; step < 10 && colour.ContrastRatioWith(background) < needed; step++)
        {
            colour = new Color(
                (byte)Math.Round(colour.R + (ink.R - colour.R) * 0.2),
                (byte)Math.Round(colour.G + (ink.G - colour.G) * 0.2),
                (byte)Math.Round(colour.B + (ink.B - colour.B) * 0.2));
        }

        return colour;
    }
}
