// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using System.Text;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="PieSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class PieSeriesRenderer : SeriesRenderer<PieSeries>
{
    /// <inheritdoc />
    public PieSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PieSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(PieSeries series)
    {
        // The total of the slices that are there: a slice that cannot be drawn (missing, or infinite) counts as 0 and
        // draws nothing of its own below; with no total above nothing there is no whole to share.
        if (series.Sizes.DrawableTotal() is not { } total) return;
        // A pie told its radius takes that share of half the shorter side, otherwise the share a disc takes.
        var frame = RoundFrame.In(Area.PlotBounds, series.Radius ?? RoundFrame.DiscShare);
        double cx = frame.Centre.X, cy = frame.Centre.Y, radius = frame.Radius;
        // A wedge's edge: matplotlib's patch rule under the matplotlib themes (black 1 pt under classic, none under v2), a white pixel
        // under the library's own.
        var edges = Context.Theme.PatchEdgesOf(series);
        var edge = edges.Of();
        // From the start angle, each wedge its sweep (WedgeSeries.SweepOf), in radians counter-clockwise with Y up: as given, else as
        // the theme lays wedges (WedgeLook: the top and clockwise under the library's own themes, three o'clock and counter-clockwise
        // under the matplotlib themes).
        double startAngle = series.StartAngleUnder(Context.Theme).ToRadians();
        double nameBand = Context.Theme.Looks.Wedges.NameCentreBand;
        string? percentFormat = series.AutoPct is { } autoPct ? ToNetPercentFormat(autoPct) : null;

        // Collect outer-label candidates during the slice pass so LabelLayoutEngine can
        // resolve collisions in one batch at the end. Small wedges would otherwise draw
        // their labels on top of each other — matplotlib pies have the same issue.
        var labelFont = Context.Theme.DataTextFont();
        var outerCandidates = new List<LabelCandidate>();
        var outerAnchors = new List<Point>();
        var outerAlignments = new List<TextAlignment>();

        for (int i = 0; i < series.Sizes.Length; i++)
        {
            if (!series.Sizes[i].IsDrawable()) continue;
            double sweep = series.SweepOf(i, total, Context.Theme);
            double midAngle = startAngle + sweep / 2;
            double explode = series.Explode.ElementOr(i, 0.0);
            var slice = new Point(cx, cy).AtAngleCounterClockwise(explode * radius, midAngle);
            double sliceCx = slice.X, sliceCy = slice.Y;
            double endAngle = startAngle + sweep;

            // Shadow: draw offset gray slice before the main slice
            if (series.Shadow)
            {
                double shadowOffset = radius * 0.03;
                double scx = sliceCx + shadowOffset;
                double scy = sliceCy + shadowOffset;
                Ctx.DrawPath(
                    Slice(new Point(scx, scy), radius, startAngle, endAngle),
                    ShapeStyle.Filled(Colors.DropShadow));
            }

            var sliceColor = PartColor(series.Colors, i);
            var sliceHatch = series.Hatches.ElementOr(i, HatchPattern.None);
            Ctx.DrawPath(
                Slice(slice, radius, startAngle, endAngle),
                edges.StyleOf(sliceColor, edge, new HatchFill(sliceHatch, null)));

            // AutoPct: draw percentage text at the centroid of the slice (inside the wedge).
            // Not collision-handled — interior labels are constrained to their own wedges and
            // rarely collide; if a wedge is too small for its AutoPct, the label just clips.
            if (percentFormat is not null)
            {
                double pct = series.Sizes[i] / total * 100;
                string label = percentFormat.FormatInvariant(pct);
                double textAngle = startAngle + sweep / 2;
                double textR = radius * 0.6;
                Ctx.Draw(TextLabel.Of(label), Context.Theme.DataTextFontOn(sliceColor, labelFont.Size, Context.Theme.Looks.Wedges.PercentInk), new TextPlacement(slice.AtAngleCounterClockwise(textR, textAngle), TextAlignment.Center) { Vertical = TextVerticalAlignment.Center });
            }

            // Collect the outer label for the collision pass — do not draw yet.
            if (series.Labels is not null && i < series.Labels.Length && series.Labels[i] is { } sliceLabel)
            {
                double textAngle = startAngle + sweep / 2;
                double textR = radius * 1.1;
                var at = slice.AtAngleCounterClockwise(textR, textAngle);
                // Right of the centre a name starts at its anchor, left of it ends there; within the theme's band about straight up
                // or down it is centred on it (WedgeLook.NameCentreBand: none under the matplotlib themes, as pie_label aligns it).
                double cosA = Math.Cos(textAngle);
                var align = cosA > nameBand ? TextAlignment.Left : cosA < -nameBand ? TextAlignment.Right : TextAlignment.Center;
                outerCandidates.Add(new LabelCandidate(at, sliceLabel, labelFont, align) { VerticalAlignment = TextVerticalAlignment.Center });
                outerAnchors.Add(at);
                outerAlignments.Add(align);
            }

            startAngle = endAngle;
        }

        if (outerCandidates.Count > 0)
            DrawPlacedLabels(PlaceLabels(outerCandidates, Area.PlotBounds));
    }

    /// <summary>The outline of a pie slice as the segments of a closed path of line segments (a <see cref="RingWedge"/> drawn as a
    /// polyline, so it works correctly in both Skia (PNG) and SVG renderers regardless of how each backend handles arc commands).</summary>
    private static IReadOnlyList<PathSegment> Slice(Point centre, double radius, double startAngle, double endAngle) =>
        RingWedge.CounterClockwise(centre, 0, radius, startAngle, endAngle).PolygonPath();

    /// <summary>The .NET format to print a wedge's share with. Text that holds a <c>{</c>, or no printf conversion, is a .NET
    /// format (or plain text) and comes back as it is. Otherwise <c>%f</c> and <c>%F</c> become <c>{0:F</c><i>n</i><c>}</c>
    /// (precision as written, 6 when none), <c>%d</c> and <c>%i</c> become <c>{0:F0}</c> (the rounded share), and <c>%%</c>
    /// becomes <c>%</c>; flags and width are dropped.</summary>
    /// <returns>The .NET format string.</returns>
    /// <param name="autoPct">The <see cref="string"/> it works on.</param>
    internal static string ToNetPercentFormat(string autoPct)
    {
        if (autoPct.Contains('{') || !autoPct.Contains('%')) return autoPct;
        var net = new StringBuilder(autoPct.Length + 8);
        bool converted = false;
        for (int i = 0; i < autoPct.Length; i++)
        {
            char c = autoPct[i];
            if (c == '}') { net.Append("}}"); continue; }
            if (c != '%') { net.Append(c); continue; }
            if (i + 1 < autoPct.Length && autoPct[i + 1] == '%') { net.Append('%'); i++; continue; }
            int end = i + 1;
            while (end < autoPct.Length && autoPct[end] is '-' or '+' or ' ' or '0' or '#') end++;
            while (end < autoPct.Length && char.IsAsciiDigit(autoPct[end])) end++;
            int precisionAt = -1;
            if (end < autoPct.Length && autoPct[end] == '.')
            {
                precisionAt = ++end;
                while (end < autoPct.Length && char.IsAsciiDigit(autoPct[end])) end++;
            }
            if (end >= autoPct.Length || autoPct[end] is not ('f' or 'F' or 'd' or 'i')) { net.Append('%'); continue; }
            int precision = autoPct[end] is 'd' or 'i' ? 0
                : precisionAt < 0 ? 6
                : precisionAt == end ? 0
                : autoPct.AsSpan(precisionAt, end - precisionAt).ReadInvariant<int>(System.Globalization.NumberStyles.Integer);
            net.Append("{0:F").AppendInvariant(precision).Append('}');
            converted = true;
            i = end;
        }
        return converted ? net.ToString() : autoPct;
    }
}
