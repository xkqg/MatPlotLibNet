// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StatTileSeries"/> — a headline number with its label, an optional gap caption,
/// and an optional inline trend, filling the plot area it is given.
/// <para>A bare big number is a failed dashboard pattern: without a comparative, a reader cannot tell whether
/// it is good or bad. So the tile is drawn as four things — the value, the label, the gap in words, and the
/// trend that says which way it is going.</para></summary>
internal sealed class StatTileSeriesRenderer : SeriesRenderer<StatTileSeries>
{
    /// <summary>Vertical share of the tile given to the inline sparkline, measured from the bottom.</summary>
    private const double TrendShare = 0.22;

    /// <summary>Baseline step between two caption lines, at the caption's own 11 pt.</summary>
    private const double CaptionLineHeight = 14;

    /// <summary>The headline's font size on a tile with room for it.</summary>
    private const double HeadlineSize = 44;

    /// <summary>The smallest the headline may shrink to when the stack has to fit a short tile.</summary>
    private const double ShortestHeadline = 24;

    /// <summary>Drop from the headline's baseline to the label's — the number plus its breathing room.</summary>
    private const double HeadlineDrop = 30;

    /// <summary>Baseline step from the label to the first caption line.</summary>
    private const double LabelLineHeight = 18;

    /// <summary>Breathing room left either side of a caption line, so a wrapped line never touches the tile edge.</summary>
    private const double CaptionMargin = 6;

    /// <summary>What separates two caption lines — both platforms' newlines, so a caption composed on either
    /// one stacks the same way.</summary>
    private static readonly string[] CaptionLineBreaks = [Environment.NewLine, "\n"];
    /// <inheritdoc />
    public StatTileSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(StatTileSeries series)
    {
        var bounds = Area.PlotBounds;

        // A tile at rest wears the theme's NEUTRAL shade — never a colour from the series cycle. The cycler
        // exists to tell data series apart; a state mark is not a data series. Letting a resting tile take a
        // cycle colour is how a wall ends up in five cheerful hues, and then the one tile that actually needs
        // attention has nothing to stand out against. Colour appears only when the caller sets an accent, and
        // an accent means something is wrong.
        var color = series.AccentColor ?? Context.Theme.Alarm.Resting;

        // "No information" is a pattern, not a colour: a silent source is not a broken one.
        if (series.Hatch != HatchPattern.None)
        {
            Ctx.DrawRectangle(bounds, new ShapeStyle(Context.Theme.AxesBackground, null, 0)
            {
                Hatch = series.Hatch,
                HatchColor = series.HatchColor
            });
        }

        bool hasTrend = series.Trend is { Count: >= 2 };
        double cx = bounds.X + bounds.Width / 2;

        // The whole STACK is centred — headline, label and every caption line — so a tile that carries more
        // lines takes the free room ABOVE its headline instead of growing down into the sparkline strip
        // (reported from an ops wall, 2026-08-30: "there is plenty of room at the top of the tile"). One line
        // of each renders exactly where it always did; each extra line lifts the block by half its height.
        var captionFont = new Font { Size = 11, Color = color };
        string[] captionLines = WrapCaption(series.Caption, captionFont, bounds.Width - 2 * CaptionMargin);
        double bodyHeight = hasTrend ? bounds.Height * (1 - TrendShare) : bounds.Height;

        // THE STACK FITS THE BODY. Headline, label and every caption line are laid out inside the body the
        // sparkline leaves, and when they do not fit the HEADLINE shrinks — down to ShortestHeadline — rather
        // than the caption spilling over the sparkline or out of the tile (seen on an ops wall: a two-line
        // caption on a 110 px tile printed straight through the trend line and past the tile's own edge).
        double labelDrop = string.IsNullOrEmpty(series.Label) ? 0 : LabelLineHeight;
        double belowHeadline = labelDrop + (captionLines.Length * CaptionLineHeight);
        double scale = 1;
        if (HeadlineDrop + belowHeadline > bodyHeight)
        {
            scale = Math.Clamp((bodyHeight - belowHeadline) / HeadlineDrop, ShortestHeadline / HeadlineSize, 1);
        }
        double headlineDrop = HeadlineDrop * scale;
        double stackHeight = headlineDrop + belowHeadline;
        double cy = bounds.Y + (bodyHeight - stackHeight) / 2 + (headlineDrop / 2);
        // Where the stack actually ENDS — the sparkline starts under it, never through it.
        double stackBottom = cy + stackHeight - headlineDrop + CaptionLineHeight / 2;

        Ctx.DrawText(series.FormattedValue, new Point(cx, cy),
            new Font { Size = HeadlineSize * scale, Weight = FontWeight.Bold, Color = color }, TextAlignment.Center);

        double below = cy + headlineDrop;
        if (!string.IsNullOrEmpty(series.Label))
        {
            // The theme's ink, like the value above and the caption below. Without a Color the SVG text
            // carries no fill at all and inherits BLACK — invisible on any operator ground, which is
            // exactly where a tile lives (reported from the Ait console: unreadable tile names).
            Ctx.DrawText(series.Label, new Point(cx, below),
                new Font { Size = 14, Color = Context.Theme.ForegroundText }, TextAlignment.Center);
            below += LabelLineHeight;
        }

        // The gap line — the element that answers "is this good or bad" without anyone doing arithmetic.
        // MULTI-LINE: a caption may carry more than one question ("is this good or bad" and "measured over
        // what"), and two answers crammed onto one row run wider than the tile (reported from an ops wall,
        // 2026-08-30). Newline-separated lines are drawn stacked, centred, in the caption's own ink.
        foreach (var line in captionLines)
        {
            Ctx.DrawText(line, new Point(cx, below), captionFont, TextAlignment.Center);
            below += CaptionLineHeight;
        }

        if (hasTrend)
        {
            RenderTrend(series, bounds, color, Math.Max(bodyHeight, stackBottom - bounds.Y));
        }
    }

    // The caption's lines: the caller's own breaks FIRST (they mean something — one answer per line), then a
    // WRAP of whatever is still wider than the tile, at word boundaries. This is the CSS behaviour a caption
    // needs on a wall: an ops caption grows with what it has to say, and a tile is a fixed column (reported
    // 2026-08-30, "threshold 250 · 2148 msg · 1 s" ran out over its neighbours). A word that does not fit on
    // its own is never cut — it is better to overflow one word than to render half of one.
    private string[] WrapCaption(string? caption, Font font, double width)
    {
        if (string.IsNullOrEmpty(caption))
        {
            return [];
        }
        var lines = new List<string>();
        foreach (var declared in caption.Split(CaptionLineBreaks, StringSplitOptions.None))
        {
            if (width <= 0 || Ctx.MeasureText(declared, font).Width <= width)
            {
                lines.Add(declared);
                continue;
            }
            var line = new StringBuilder();
            foreach (var word in declared.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : $"{line} {word}";
                if (line.Length > 0 && Ctx.MeasureText(candidate, font).Width > width)
                {
                    lines.Add(line.ToString());
                    line.Clear().Append(word);
                    continue;
                }
                line.Clear().Append(candidate);
            }
            if (line.Length > 0)
            {
                lines.Add(line.ToString());
            }
        }
        return [.. lines];
    }

    /// <summary>Draws the inline trend by DELEGATING to <see cref="SparklineSeriesRenderer"/> in a sub-area of
    /// the tile.
    /// <para>The sparkline renderer already knows how to draw an axis-less, frame-less line that fills the area
    /// it is handed — and <see cref="SeriesRenderer"/> exposes that area as an overridable seam precisely so a
    /// composite can hand it a smaller one. Re-implementing the polyline here would be a second copy of the
    /// same twelve lines, and the copy that nobody maintains is the one that drifts.</para></summary>
    private void RenderTrend(StatTileSeries series, Rect bounds, Color color, double bodyHeight)
    {
        double top = bounds.Y + bodyHeight;
        var strip = new Rect(bounds.X + bounds.Width * 0.1, top,
                             bounds.Width * 0.8, Math.Max(1, bounds.Height - bodyHeight - 4));

        var subContext = Context with { Area = new RenderArea(strip, Area.Context) };

        var sparkline = new SparklineSeries([.. series.Trend!]) { Color = series.TrendColor ?? color };
        new SparklineSeriesRenderer(subContext).Render(sparkline);
    }
}
