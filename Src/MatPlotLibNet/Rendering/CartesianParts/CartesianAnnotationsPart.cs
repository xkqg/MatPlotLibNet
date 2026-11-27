// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders user annotations (text boxes, arrows, callouts) on a Cartesian axes.
/// Draws each Annotation's text + optional
/// background box + optional connection path + arrowhead.
/// </summary>
internal sealed class CartesianAnnotationsPart : CartesianAxesPart
{
    /// <summary>Constructs the part for one render.</summary>
    /// <param name="context">What the render draws with: the axes, the plot area, the surface, the theme and the primary transform,
    /// which places the annotations.</param>
    internal CartesianAnnotationsPart(in CartesianPartContext context)
        : base(context) { }

    /// <summary>The marks an annotation that asked to move keeps clear of: the series' and the legend's box. Nothing by default,
    /// so an annotation stands where it is put.</summary>
    internal MarkObstacles Obstacles { get; init; }

    /// <summary>One annotation as it stands on the plot: the font its text is drawn in, the text's measured extent, the point
    /// its baseline stands at (its anchor, moved by its pixel offset and its vertical alignment), and where it was put before
    /// the layout moved it.</summary>
    private readonly record struct PlacedAnnotation(Annotation Annotation, Font Font, TextExtent TextSize, Point TextPosition, Point PutAt)
    {
        /// <summary>Where the text is drawn: on its baseline, in its alignment, turned by its rotation.</summary>
        internal TextPlacement TextPlacement => new(TextPosition, Annotation.Alignment) { Rotation = Annotation.Rotation };
    }

    /// <summary>The annotations that can be drawn, in order: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddTextBoxes"/> hands to a legend.</summary>
    private IEnumerable<PlacedAnnotation> Placed()
    {
        foreach (var annotation in Axes.Annotations)
        {
            // The font's own colour, else the annotation's, else the theme's: the one rule every placed text follows.
            var annotFont = Theme.DataTextFont(annotation.Font, annotation.Color);
            // An axes-fraction annotation is placed on the PANEL, not the data: (0,0) bottom-left, (1,1) top-right,
            // so a panel label sits in its corner whatever the limits are (matplotlib's 'axes fraction').
            bool inData = annotation.Coordinates != AnnotationCoordinates.AxesFraction;
            var anchor = inData
                ? Transform.DataToPixel(annotation.X, annotation.Y)
                : new Point(PlotArea.X + annotation.X * PlotArea.Width, PlotArea.Y + (1 - annotation.Y) * PlotArea.Height);

            // matplotlib draws an annotation in data coordinates only where the axes show the point it annotates
            // (Annotation._check_xy): its arrow's target when it has one (matplotlib's xy, the text standing at xytext), else its own.
            if (inData && !PlotArea.Shows(annotation.ArrowTargetX is { } toX && annotation.ArrowTargetY is { } toY
                    ? Transform.DataToPixel(toX, toY) : anchor)) continue;
            // The pixel offset moves the text off its point, the point itself and the arrow's target stay where they are.
            anchor = new Point(anchor.X + annotation.OffsetX, anchor.Y + annotation.OffsetY);

            // An annotation stands at its text: where that cannot be placed it is skipped whole, its box and its arrow
            // (which starts at the text) with it.
            if (!anchor.IsDrawable()) continue;

            // The vertical alignment decides where the baseline stands from the anchor; the box and the arrow follow the text.
            var textSize = Ctx.ExtentOf(TextLabel.Of(annotation.Text), annotFont);
            var textPos = new TextBox(new TextPlacement(anchor, annotation.Alignment)
                { Vertical = annotation.VerticalAlignment, Rotation = annotation.Rotation }, textSize).Baseline;
            yield return new PlacedAnnotation(annotation, annotFont, textSize, textPos, textPos);
        }
    }

    /// <summary>How far a text has to move before it is joined to its anchor by a line, in pixels: the distance the label
    /// layout of bars and slices uses.</summary>
    private const double LeaderThreshold = 6.0;

    /// <summary>The annotations as they stand once those that asked to move have found a place: the texts that stay are placed
    /// first (every text that moves keeps clear of them), then the others in list order, each clear of the ones before it.</summary>
    private List<PlacedAnnotation> Laid()
    {
        var put = Placed().ToList();
        if (!put.Any(p => p.Annotation.AvoidOverlap)) return put;

        var obstacles = Obstacles;
        TextBox BoxOf(PlacedAnnotation p) => new(p.TextPlacement, p.TextSize);
        foreach (var p in put)
            if (!p.Annotation.AvoidOverlap) PlaceText(BoxOf(p), PlotArea, ref obstacles, moves: false);

        var laid = new List<PlacedAnnotation>(put.Count);
        foreach (var p in put)
            laid.Add(p.Annotation.AvoidOverlap ? p with { TextPosition = PlaceText(BoxOf(p), PlotArea, ref obstacles, moves: true) } : p);
        return laid;
    }

    /// <summary>Adds the box of every annotation's text, where it stands, to <paramref name="marks"/>: matplotlib counts a text
    /// among the marks a legend keeps clear of, and so does this. The text is turned with its rotation.</summary>
    /// <param name="marks">The collection the boxes go into.</param>
    internal void AddTextBoxes(MarkObstacles.Builder marks)
    {
        foreach (var placed in Placed())
        {
            marks.AddBox(new TextBox(placed.TextPlacement, placed.TextSize).Bounds());
        }
    }

    /// <inheritdoc />
    public override void Render()
    {
        foreach (var placed in Laid())
        {
            var (annotation, annotFont, textSize, textPos, putAt) = placed;
            // A text that moved is joined to the point it was put at, unless an arrow leaves it: the arrow is the connector then.
            bool hasArrow = annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue && annotation.ArrowStyle != ArrowStyle.None;
            double moved = putAt.DistanceTo(textPos);
            if (moved > LeaderThreshold && !hasArrow)
                Ctx.DrawLeaderLine(putAt, textPos, annotation.Color ?? Theme.ForegroundText);

            // Background box or legacy background fill behind text: it stands where the text stands, so a text that is
            // centred or ends at its point has its box begin where the text begins, not at the point.
            var textBounds = new Rect(textPos.X + annotation.Alignment.StartOffset(textSize.Width) - 2,
                textPos.Y - textSize.Height, textSize.Width + 4, textSize.Height + 2);

            if (annotation.BoxStyle != BoxStyle.None)
            {
                annotation.BoxAround(textBounds).Draw(Ctx);
            }
            else if (annotation.BackgroundColor.HasValue)
            {
                Ctx.DrawRectangle(textBounds, ShapeStyle.Filled(annotation.BackgroundColor.Value));
            }

            // Draw text with alignment and optional rotation (rotation=0 is a no-op in the SVG renderer)
            Ctx.Draw(TextLabel.Of(annotation.Text), annotFont, placed.TextPlacement);

            // Connection path + arrowhead (respects ConnectionStyle + ArrowStyle)
            if (annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue
                && annotation.ArrowStyle != ArrowStyle.None)
            {
                var arrowTarget = Transform.DataToPixel(annotation.ArrowTargetX.Value, annotation.ArrowTargetY.Value);
                // An arrow to a point the axes cannot place is skipped; the text stays.
                if (!arrowTarget.IsDrawable()) continue;
                var arrowColor = annotation.ArrowColor ?? annotation.Color ?? Theme.ForegroundText;

                // Connection path
                var connPath = ConnectionPath.Of(annotation.ConnectionStyle).Between(textPos, arrowTarget, annotation.ConnectionRad);
                Ctx.DrawPath(connPath, ShapeStyle.Outlined(arrowColor, LineWidthRule.OwnPixel.Resting));

                // Arrowhead at target: drawn for the two ends of the arrow by the head of its style; an arrow of no length has none.
                ArrowHead.Of(annotation.ArrowStyle).Draw(Ctx, textPos, arrowTarget, annotation.ArrowHeadSize, arrowColor);
            }
        }
    }

    /// <summary>How many steps out from the anchor the layout of a text that moves tries, in text heights.</summary>
    private const int Steps = 4;

    /// <summary>A direction the layout steps in, as a multiple of the text's height: +X is right, +Y is down.</summary>
    private readonly record struct Direction(int X, int Y);

    private static readonly Direction[] Directions =
        [new(0, -1), new(0, 1), new(1, 0), new(-1, 0), new(1, -1), new(-1, -1), new(1, 1), new(-1, 1)];

    /// <summary>Places one text, and records the box it stands in among the marks the texts after it keep clear of.</summary>
    /// <remarks>Where the annotations of one axes stand once the ones that asked to move have found a place: the place they were put
    /// at when nothing is there, otherwise the first of a fixed list that covers the fewest marks (<see cref="MarkObstacles"/>), the
    /// same selector a Best legend uses. The list is the anchor, then four steps (one to four text heights) out in eight directions,
    /// in the order up, down, right, left, upper right, upper left, lower right, lower left; every place but the anchor lies inside
    /// the plot area. Nothing random takes part, so a chart is laid out the same way every time on every platform.
    /// <para>The marks a text must keep clear of are the ones it is given (the series' marks and the legend's box) and the
    /// annotations placed so far, each text's box once it is placed: a text that does not move is placed first by its caller, so
    /// every text that does move keeps clear of it, and a text that moved is an obstacle for the ones after it, in list order.</para></remarks>
    /// <param name="text">The text where it was put: its anchor, its measured size, its alignment and its turn.</param>
    /// <param name="plotArea">The plot area: a text that moves stays inside it.</param>
    /// <param name="obstacles">The marks the text keeps clear of (the series' and the legend's, and the texts placed so far); the
    /// text's own box is added to them.</param>
    /// <param name="moves">Whether the text asked to move out of the way; a text that did not stands where it was put.</param>
    /// <returns>The point the text is drawn at: where it was put, or the place the layout found for it.</returns>
    internal static Point PlaceText(TextBox text, Rect plotArea, ref MarkObstacles obstacles, bool moves)
    {
        var at = moves ? FreePlace(text, plotArea, obstacles) : text.Placement.Anchor;
        obstacles = obstacles.Including(text.At(at).Bounds());
        return at;
    }

    /// <summary>The first place, in the fixed order, that covers the fewest marks.</summary>
    private static Point FreePlace(TextBox text, Rect plotArea, MarkObstacles obstacles)
    {
        var places = new List<Point> { text.Placement.Anchor };
        var boxes = new List<Rect> { text.Bounds() };
        double step = text.Extent.Height;
        for (int ring = 1; ring <= Steps; ring++)
        {
            foreach (var direction in Directions)
            {
                var place = new Point(text.Placement.Anchor.X + direction.X * ring * step, text.Placement.Anchor.Y + direction.Y * ring * step);
                var box = text.At(place).Bounds();
                if (!plotArea.Contains(box)) continue;
                places.Add(place);
                boxes.Add(box);
            }
        }
        return places[obstacles.FirstLeastCovered(boxes)];
    }
}
