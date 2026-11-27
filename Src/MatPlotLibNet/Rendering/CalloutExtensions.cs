// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>What an annotation's text callout is made of, as members of the thing that draws it or holds it: the leader line a
/// text that was moved is joined to its point by, and the box behind the text.</summary>
internal static class CalloutExtensions
{
    extension(IRenderContext ctx)
    {
        /// <summary>Draws a thin leader line from <paramref name="from"/> to <paramref name="to"/> — used by
        /// <see cref="Layout.LabelLayoutEngine"/> consumers (pie, sankey, sunburst) to connect a
        /// label's original anchor to its collision-adjusted final position.</summary>
        /// <param name="from">Where the label was put.</param>
        /// <param name="to">Where it stands.</param>
        /// <param name="color">The colour of the line.</param>
        /// <param name="width">The width of the line.</param>
        public void DrawLeaderLine(Point from, Point to, Color color, double width = 0.5)
            => ctx.DrawLine(from, to, StrokeStyle.Solid(color, width));
    }

    extension(Annotation annotation)
    {
        /// <summary>The box this annotation asks for around its text.</summary>
        /// <param name="textBounds">The rectangle of the text.</param>
        public CalloutBox BoxAround(Rect textBounds) => new(
            textBounds, annotation.BoxStyle, annotation.BoxPadding, annotation.BoxCornerRadius,
            annotation.BoxFaceColor, annotation.BoxEdgeColor, annotation.BoxLineWidth);
    }
}
