// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders horizontal/vertical reference lines (full-width markers).
/// Draws each ReferenceLine's full-axis line +
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
            var lineColor = refLine.Color ?? Theme.Neutral;
            // A line at a value its axis cannot place is skipped with its label. It spans the plot area, so only the
            // one coordinate it is placed at decides. The automatic range holds every line; a line beyond a limit the
            // caller set lies outside the plot and is not drawn either, nor its label.
            if (refLine.Orientation == Orientation.Horizontal)
            {
                var pt = Transform.DataToPixel(_range.XMin, refLine.Value);
                if (!pt.Y.IsDrawable() || PlotArea.HorizontalAcross(pt.Y) is not { } across) continue;
                Ctx.DrawLine(across.From, across.To, new StrokeStyle(lineColor, refLine.LineWidth, refLine.LineStyle));
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
                if (!pt.X.IsDrawable() || PlotArea.VerticalAcross(pt.X) is not { } across) continue;
                Ctx.DrawLine(across.From, across.To, new StrokeStyle(lineColor, refLine.LineWidth, refLine.LineStyle));
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
