// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders user annotations (text boxes, arrows, callouts) on a Cartesian axes.
/// Extracted verbatim from <c>CartesianAxesRenderer.Render</c> lines 281-339
/// (pre-B.4). Responsibility: draw each Annotation's text + optional
/// background box + optional connection path + arrowhead.
/// </summary>
internal sealed class CartesianAnnotationsPart : CartesianAxesPart
{
    /// <inheritdoc />
    public CartesianAnnotationsPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, DataTransform transform)
        : base(axes, plotArea, ctx, theme, transform) { }

    /// <inheritdoc />
    public override void Render()
    {
        foreach (var annotation in Axes.Annotations)
        {
            var annotFont = annotation.Font ?? new Font
            {
                Family = Theme.DefaultFont.Family,
                Size = 10,
                Color = annotation.Color ?? Theme.ForegroundText
            };
            var textPos = Transform.DataToPixel(annotation.X, annotation.Y);

            // Background box or legacy background fill behind text
            var textSize = Ctx.MeasureText(annotation.Text, annotFont);
            var textBounds = new Rect(textPos.X - 2, textPos.Y - textSize.Height, textSize.Width + 4, textSize.Height + 2);

            if (annotation.BoxStyle != BoxStyle.None)
            {
                CalloutBoxRenderer.Draw(Ctx, textBounds, annotation.BoxStyle,
                    annotation.BoxPadding, annotation.BoxCornerRadius,
                    annotation.BoxFaceColor, annotation.BoxEdgeColor, annotation.BoxLineWidth);
            }
            else if (annotation.BackgroundColor.HasValue)
            {
                Ctx.DrawRectangle(textBounds, annotation.BackgroundColor.Value, null, 0);
            }

            // Draw text with alignment and optional rotation (rotation=0 is a no-op in the SVG renderer)
            Ctx.DrawText(annotation.Text, textPos, annotFont, annotation.Alignment, annotation.Rotation);

            // Connection path + arrowhead (respects ConnectionStyle + ArrowStyle)
            if (annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue
                && annotation.ArrowStyle != ArrowStyle.None)
            {
                var arrowTarget = Transform.DataToPixel(annotation.ArrowTargetX.Value, annotation.ArrowTargetY.Value);
                var arrowColor = annotation.ArrowColor ?? annotation.Color ?? Theme.ForegroundText;

                // Connection path
                var connPath = ConnectionPathBuilder.BuildPath(textPos, arrowTarget,
                    annotation.ConnectionStyle, annotation.ConnectionRad);
                Ctx.DrawPath(connPath, null, arrowColor, 1);

                // Arrowhead at target
                double dx = arrowTarget.X - textPos.X;
                double dy = arrowTarget.Y - textPos.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len > 0)
                {
                    double ux = dx / len, uy = dy / len;
                    var headPolygon = ArrowHeadBuilder.BuildPolygon(arrowTarget, ux, uy,
                        annotation.ArrowStyle, annotation.ArrowHeadSize);
                    if (headPolygon.Count > 0)
                        Ctx.DrawPolygon([.. headPolygon], arrowColor, null, 0);

                    var headPath = ArrowHeadBuilder.BuildPath(arrowTarget, ux, uy,
                        annotation.ArrowStyle, annotation.ArrowHeadSize);
                    if (headPath is { Count: > 0 })
                        Ctx.DrawPath(headPath, null, arrowColor, 1);
                }
            }
        }
    }
}
