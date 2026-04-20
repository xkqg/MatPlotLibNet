// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders horizontal/vertical reference lines (full-width markers).
/// Extracted verbatim from <c>CartesianAxesRenderer.Render</c> lines 139-174
/// (pre-B.6). Responsibility: draw each ReferenceLine's full-axis line +
/// optional right/left-anchored label.
/// </summary>
internal sealed class CartesianReferenceLinesPart : CartesianAxesPart
{
    private readonly DataRange _range;

    /// <summary>Constructs a ReferenceLines part.</summary>
    public CartesianReferenceLinesPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme,
                                        DataTransform transform, DataRange range)
        : base(axes, plotArea, ctx, theme, transform)
    {
        _range = range;
    }

    /// <inheritdoc />
    public override void Render()
    {
        var refLabelFont = TickFont();
        foreach (var refLine in Axes.ReferenceLines)
        {
            var lineColor = refLine.Color ?? Colors.Gray;
            if (refLine.Orientation == Orientation.Horizontal)
            {
                var pt = Transform.DataToPixel(_range.XMin, refLine.Value);
                Ctx.DrawLine(
                    new Point(PlotArea.X, pt.Y),
                    new Point(PlotArea.X + PlotArea.Width, pt.Y),
                    lineColor, refLine.LineWidth, refLine.LineStyle);
                if (refLine.Label is not null)
                {
                    var labelFont = refLabelFont with { Color = lineColor };
                    Ctx.DrawText(refLine.Label,
                        new Point(PlotArea.X + PlotArea.Width, pt.Y - 2),
                        labelFont, TextAlignment.Right);
                }
            }
            else
            {
                var pt = Transform.DataToPixel(refLine.Value, _range.YMin);
                Ctx.DrawLine(
                    new Point(pt.X, PlotArea.Y),
                    new Point(pt.X, PlotArea.Y + PlotArea.Height),
                    lineColor, refLine.LineWidth, refLine.LineStyle);
                if (refLine.Label is not null)
                {
                    var labelFont = refLabelFont with { Color = lineColor };
                    Ctx.DrawText(refLine.Label,
                        new Point(pt.X + 2, PlotArea.Y + refLabelFont.Size),
                        labelFont, TextAlignment.Left);
                }
            }
        }
    }
}
