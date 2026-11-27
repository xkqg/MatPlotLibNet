// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="QuiverKeySeries"/> as a reference arrow with a label in axes-fraction coordinates, the way
/// matplotlib's <c>QuiverKey</c> stands it: the key's point is the middle of the arrow for a label above or below it, its head for
/// a label to the right and its tail for a label to the left, and the label stands a tenth of an inch from that point.</summary>
internal sealed class QuiverKeySeriesRenderer : SeriesRenderer<QuiverKeySeries>
{
    /// <summary>The gap between the arrow and its label: matplotlib's <c>QuiverKey(labelsep=0.1)</c>, a tenth of an inch, which is
    /// 7.2 points.</summary>
    private static readonly double LabelSep = 7.2.PointsInPixels;

    /// <summary>Where the label stands for <paramref name="position"/>, by matplotlib's <c>QuiverKey</c> tables: <c>halign</c> centre,
    /// centre, left, right and <c>valign</c> bottom, top, centre, centre for N, S, E and W, the anchor the key's point shifted by the
    /// label sep towards the side the label is on.</summary>
    private static TextPlacement LabelAt(QuiverKeyLabelPosition position, Point point) => position switch
    {
        QuiverKeyLabelPosition.Below => new(new Point(point.X, point.Y + LabelSep), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top },
        QuiverKeyLabelPosition.Right => new(new Point(point.X + LabelSep, point.Y), TextAlignment.Left) { Vertical = TextVerticalAlignment.Center },
        QuiverKeyLabelPosition.Left => new(new Point(point.X - LabelSep, point.Y), TextAlignment.Right) { Vertical = TextVerticalAlignment.Center },
        _ => new(new Point(point.X, point.Y - LabelSep), TextAlignment.Center) { Vertical = TextVerticalAlignment.Bottom },
    };

    /// <inheritdoc />
    public QuiverKeySeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The last quiver drawn before <paramref name="key"/> on its axes, the quiver its length is in (matplotlib's
    /// <c>QuiverKey(Q, ...)</c>); <see langword="null"/> when there is none or the key is drawn on its own.</summary>
    private QuiverSeries? QuiverBefore(QuiverKeySeries key)
    {
        QuiverSeries? quiver = null;
        if (Context.AxesSeries is not { } drawn) return quiver;
        for (int i = 0; i < drawn.Count && !ReferenceEquals(drawn[i], key); i++)
        {
            if (drawn[i] is QuiverSeries before) quiver = before;
        }
        return quiver;
    }

    /// <inheritdoc />
    public override void Render(QuiverKeySeries series)
    {
        var color = ColorOf(series);
        var bounds = Area.PlotBounds;

        // Convert axes-fraction to pixel coordinates
        double px = bounds.X + series.X * bounds.Width;
        double py = bounds.Y + (1.0 - series.Y) * bounds.Height;

        // The arrow is as long as an arrow of the vector (U, 0) is in the quiver the key stands for (matplotlib draws it through
        // that quiver's own arrow): U times what a unit is in that quiver, the share of the axes' width its autoscale gives a unit or
        // its Scale in data units along X, and U data units when no quiver is drawn before it.
        double pixelsPerDataUnit = bounds.Width / (Transform.DataXMax - Transform.DataXMin);
        var quiver = QuiverBefore(series);
        bool autoscaled = quiver is not null && quiver.AutoScaledUnder(Context.Theme);
        double perUnit = quiver is null ? pixelsPerDataUnit
            : autoscaled ? quiver.AutoScaleShare() * bounds.Width
            : quiver.Scale * pixelsPerDataUnit;
        double arrowPx = series.U * perUnit;

        // The key's point lies on the arrow where matplotlib's pivot puts it: the head for a label to the right, the tail for a
        // label to the left, the middle otherwise.
        double tailX = series.LabelPosition switch
        {
            QuiverKeyLabelPosition.Right => px - arrowPx,
            QuiverKeyLabelPosition.Left => px,
            _ => Range1D.Around(px, arrowPx).Lo,
        };
        var tail = new Point(tailX, py);
        var head = new Point(tailX + arrowPx, py);
        // A key whose tail or head cannot be placed (a missing or infinite position or length) draws no arrow and no label.
        if (!tail.CanDrawLineTo(head)) return;
        if (autoscaled)
        {
            // The quiver's own polygon, along X (to the left for a negative U), as wide across as the quiver's shafts are. A quiver
            // with no scale (every vector zero long) has no arrow to draw one like.
            if (perUnit > 0)
            {
                double width = quiver!.AutoScaleWidth() * bounds.Width;
                Ctx.DrawPolygon(QuiverArrow.Place(tail, Math.Abs(arrowPx) / width, width, arrowPx < 0 ? -1 : 1, 0), ShapeStyle.Filled(color));
            }
        }
        else
        {
            // The quiver's arrow given a scale: lines of the library's own look.
            var stroke = Slot.StrokeOf(color, LineWidthRule.Own);
            Ctx.DrawLine(tail, head, stroke);

            // Arrow head (simple triangle)
            double hs = 6; // arrowhead size
            var back = Range1D.Around(head.Y, hs);
            Ctx.DrawLine(head, new Point(head.X - hs, back.Lo), stroke);
            Ctx.DrawLine(head, new Point(head.X - hs, back.Hi), stroke);
        }

        // Label: the size of the theme's text when the key was given none (matplotlib's label takes font.size), else the points it names.
        var font = Context.Theme.DataTextFont(series.LabelSizeUnder(Context.Theme));
        Ctx.Draw(TextLabel.Of(series.Label), font, LabelAt(series.LabelPosition, new Point(px, py)));
    }
}
