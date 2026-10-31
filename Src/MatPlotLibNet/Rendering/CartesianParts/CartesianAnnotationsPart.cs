// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders user annotations (text boxes, arrows, callouts) on a Cartesian axes.
/// Draws each Annotation's text + optional
/// background box + optional connection path + arrowhead.
/// </summary>
internal sealed class CartesianAnnotationsPart : CartesianAxesPart
{
    /// <inheritdoc />
    public CartesianAnnotationsPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, DataTransform transform)
        : base(axes, plotArea, ctx, theme, transform) { }

    /// <summary>The marks an annotation that asked to move keeps clear of: the series' and the legend's box. Nothing by default,
    /// so an annotation stands where it is put.</summary>
    internal MarkObstacles Obstacles { get; init; }

    /// <summary>One annotation as it stands on the plot: the font its text is drawn in, the text's measured size, the point
    /// its baseline stands at (its anchor, moved by its pixel offset and its vertical alignment), and where it was put before
    /// the layout moved it.</summary>
    private readonly record struct PlacedAnnotation(Annotation Annotation, Font Font, Size TextSize, Point TextPosition, Point PutAt);

    /// <summary>The annotations that can be drawn, in order: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddTextBoxes"/> hands to a legend.</summary>
    private IEnumerable<PlacedAnnotation> Placed()
    {
        foreach (var annotation in Axes.Annotations)
        {
            // The font's own colour, else the annotation's, else the theme's: the one rule every placed text follows.
            var annotFont = ThemedFontProvider.DataTextFont(Theme, annotation.Font, annotation.Color);
            // An axes-fraction annotation is placed on the PANEL, not the data: (0,0) bottom-left, (1,1) top-right,
            // so a panel label sits in its corner whatever the limits are (matplotlib's 'axes fraction').
            var anchor = annotation.Coordinates == AnnotationCoordinates.AxesFraction
                ? new Point(PlotArea.X + annotation.X * PlotArea.Width, PlotArea.Y + (1 - annotation.Y) * PlotArea.Height)
                : Transform.DataToPixel(annotation.X, annotation.Y);
            // The pixel offset moves the text off its point, the point itself and the arrow's target stay where they are.
            anchor = new Point(anchor.X + annotation.OffsetX, anchor.Y + annotation.OffsetY);

            // An annotation stands at its text: where that cannot be placed it is skipped whole, its box and its arrow
            // (which starts at the text) with it.
            if (!anchor.IsDrawable()) continue;

            // The vertical alignment decides where the baseline stands from the anchor; the box and the arrow follow the text.
            var textSize = Ctx.MeasureText(annotation.Text, annotFont);
            var textPos = new TextBox(anchor, textSize, annotFont.Size, annotation.Alignment,
                annotation.VerticalAlignment, annotation.Rotation).Baseline;
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

        var layout = new AnnotationLayout(PlotArea, Obstacles);
        TextBox BoxOf(PlacedAnnotation p) => new(p.TextPosition, p.TextSize, p.Font.Size, p.Annotation.Alignment,
            TextVerticalAlignment.Baseline, p.Annotation.Rotation);
        foreach (var p in put)
            if (!p.Annotation.AvoidOverlap) layout.Place(BoxOf(p), moves: false);

        var laid = new List<PlacedAnnotation>(put.Count);
        foreach (var p in put)
            laid.Add(p.Annotation.AvoidOverlap ? p with { TextPosition = layout.Place(BoxOf(p), moves: true) } : p);
        return laid;
    }

    /// <summary>Adds the box of every annotation's text, where it stands, to <paramref name="marks"/>: matplotlib counts a text
    /// among the marks a legend keeps clear of, and so does this. The text is turned with its rotation.</summary>
    /// <param name="marks">The collection the boxes go into.</param>
    internal void AddTextBoxes(MarkObstacles.Builder marks)
    {
        foreach (var placed in Placed())
        {
            marks.AddBox(new TextBox(placed.TextPosition, placed.TextSize, placed.Font.Size, placed.Annotation.Alignment,
                TextVerticalAlignment.Baseline, placed.Annotation.Rotation).Bounds());
        }
    }

    /// <inheritdoc />
    public override void Render()
    {
        foreach (var (annotation, annotFont, textSize, textPos, putAt) in Laid())
        {
            // A text that moved is joined to the point it was put at, unless an arrow leaves it: the arrow is the connector then.
            bool hasArrow = annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue && annotation.ArrowStyle != ArrowStyle.None;
            double moved = Math.Sqrt((textPos.X - putAt.X) * (textPos.X - putAt.X) + (textPos.Y - putAt.Y) * (textPos.Y - putAt.Y));
            if (moved > LeaderThreshold && !hasArrow)
                CalloutBoxRenderer.DrawLeaderLine(Ctx, putAt, textPos, annotation.Color ?? Theme.ForegroundText);

            // Background box or legacy background fill behind text: it stands where the text stands, so a text that is
            // centred or ends at its point has its box begin where the text begins, not at the point.
            var textBounds = new Rect(textPos.X + annotation.Alignment.StartOffset(textSize.Width) - 2,
                textPos.Y - textSize.Height, textSize.Width + 4, textSize.Height + 2);

            if (annotation.BoxStyle != BoxStyle.None)
            {
                CalloutBoxRenderer.Draw(Ctx, textBounds, annotation.BoxStyle,
                    annotation.BoxPadding, annotation.BoxCornerRadius,
                    annotation.BoxFaceColor, annotation.BoxEdgeColor, annotation.BoxLineWidth);
            }
            else if (annotation.BackgroundColor.HasValue)
            {
                Ctx.DrawRectangle(textBounds, new ShapeStyle(annotation.BackgroundColor.Value, null, 0));
            }

            // Draw text with alignment and optional rotation (rotation=0 is a no-op in the SVG renderer)
            Ctx.DrawText(annotation.Text, textPos, annotFont, annotation.Alignment, annotation.Rotation);

            // Connection path + arrowhead (respects ConnectionStyle + ArrowStyle)
            if (annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue
                && annotation.ArrowStyle != ArrowStyle.None)
            {
                var arrowTarget = Transform.DataToPixel(annotation.ArrowTargetX.Value, annotation.ArrowTargetY.Value);
                // An arrow to a point the axes cannot place is skipped; the text stays.
                if (!arrowTarget.IsDrawable()) continue;
                var arrowColor = annotation.ArrowColor ?? annotation.Color ?? Theme.ForegroundText;

                // Connection path
                var connPath = ConnectionPathBuilder.BuildPath(textPos, arrowTarget,
                    annotation.ConnectionStyle, annotation.ConnectionRad);
                Ctx.DrawPath(connPath, new ShapeStyle(null, arrowColor, 1));

                // Arrowhead at target: asked for by the two ends of the arrow; an arrow of no length has none.
                var headPolygon = ArrowHeadBuilder.BuildPolygon(textPos, arrowTarget,
                    annotation.ArrowStyle, annotation.ArrowHeadSize);
                if (headPolygon.Count > 0)
                    Ctx.DrawPolygon([.. headPolygon], new ShapeStyle(arrowColor, null, 0));

                var headPath = ArrowHeadBuilder.BuildPath(textPos, arrowTarget,
                    annotation.ArrowStyle, annotation.ArrowHeadSize);
                if (headPath is { Count: > 0 })
                    Ctx.DrawPath(headPath, new ShapeStyle(null, arrowColor, 1));
            }
        }
    }
}
