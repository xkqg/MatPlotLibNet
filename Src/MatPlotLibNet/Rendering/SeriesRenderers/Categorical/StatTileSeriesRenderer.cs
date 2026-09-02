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

    /// <summary>The breathing space above the headline, as a share of the body — a FIXED anatomy, so the number
    /// lands at the same height in every tile of a row however many caption lines its neighbours carry.</summary>
    private const double TopPadShare = 0.10;

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

    /// <summary>The chevron's size — the disclosure mark in the tile's top-right corner. It is a POINTING
    /// DEVICE TARGET, so it is sized to be aimed at rather than merely seen (WCAG 2.5.8 asks 24x24 CSS px for
    /// one; the whole card is the real target and this is its mark, so it sits under that at 14).</summary>
    private const double ChevronSize = 14;

    /// <summary>Inset from the tile's corner to the chevron.</summary>
    private const double ChevronInset = 8;

    /// <inheritdoc />
    public override void Render(StatTileSeries series)
    {
        var bounds = Area.PlotBounds;

        // A tile that LEADS somewhere is one anchor around everything it draws, so the whole card is the
        // target — a reader clicks the number, the label or the sparkline and all three go to the same place.
        bool linked = !string.IsNullOrEmpty(series.Url);
        if (linked)
        {
            var where = series.Expanded ? "close details" : "open details";
            var aria = string.IsNullOrEmpty(series.Label) ? where : $"{series.Label} — {where}";
            Ctx.BeginHyperlink(series.Url!, aria, series.Expanded);
            RenderHitArea(bounds);
        }

        RenderBody(series, bounds);

        if (linked)
        {
            RenderChevron(series, bounds);
            Ctx.EndHyperlink();
        }
    }

    /// <summary>The card's own hit area: a rectangle over the tile's bounds that paints NOTHING and takes every
    /// pointer event. An anchor wrapped around text and a sparkline is only hittable where ink actually lands, so
    /// without this a reader has to aim at a glyph (reported from an ops wall, 2026-09-02). Fill-opacity 0 rather
    /// than <c>fill="none"</c>: a transparent fill is still PAINTED as far as hit-testing is concerned, an absent
    /// one is not.</summary>
    private void RenderHitArea(Rect bounds)
    {
        Ctx.BeginGroup("mpl-tile-hit");
        Ctx.DrawRectangle(bounds, new ShapeStyle(Context.Theme.AxesBackground with { A = 0 }, null, 0));
        Ctx.EndGroup();
    }

    /// <summary>The disclosure chevron: right-pointing while the linked detail is closed ("there is more"),
    /// down-pointing while it is open ("shown below"). Drawn in the label's ink so it reads as part of the
    /// tile, never as an alarm. Wrapped in its own group so a style sheet or a test can find it.</summary>
    private void RenderChevron(StatTileSeries series, Rect bounds)
    {
        double right = bounds.X + bounds.Width - ChevronInset;
        double top = bounds.Y + ChevronInset;
        double half = ChevronSize / 2;
        Point[] points = series.Expanded
            ? [new(right - ChevronSize, top), new(right, top), new(right - half, top + ChevronSize)]
            : [new(right - ChevronSize, top), new(right, top + half), new(right - ChevronSize, top + ChevronSize)];

        Ctx.BeginGroup("mpl-tile-chevron");
        Ctx.DrawPolygon(points, new ShapeStyle(Context.Theme.ForegroundText, null, 0));
        Ctx.EndGroup();
    }

    /// <summary>The tile's four things — headline, label, caption, trend — exactly as before the link existed.</summary>
    private void RenderBody(StatTileSeries series, Rect bounds)
    {

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
        // ALWAYS reserved, trend or no trend: a ROW of tiles has ONE anatomy, and a stack that drops by the
        // strip's height the moment a tile has no sparkline breaks the line of numbers the eye reads across the
        // row (reported from the Ait ops wall 2026-08-31 — the two tiles without a trend sat visibly lower).
        // A tile without one leaves the strip empty rather than growing into it.
        double bodyHeight = bounds.Height * (1 - TrendShare);

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
        // ANCHORED AT THE TOP, not centred. A row is read as ONE LINE of numbers, and a centred stack moves the
        // number by half of whatever each tile happens to carry under it: measured on the Ait wall 2026-08-31 the
        // row's numbers sat at y = 65, 72 and 79 depending on how many caption lines a tile had. Anchoring puts
        // every number and every label at one height and lets the captions grow down into the room that is left.
        double cy = bounds.Y + (bodyHeight * TopPadShare) + headlineDrop;
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
