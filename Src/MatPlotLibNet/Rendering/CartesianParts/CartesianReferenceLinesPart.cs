// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.SeriesRenderers;
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
    /// <param name="context">What the render draws with: the axes, the plot area, the surface, the theme and the primary transform.</param>
    /// <param name="range">The range the axes shows.</param>
    internal CartesianReferenceLinesPart(in CartesianPartContext context, DataRange range)
        : base(context)
    {
        _range = range;
    }

    /// <inheritdoc />
    public override void Render()
    {
        foreach (var refLine in Axes.ReferenceLines)
        {
            // The line's colour, width and dash under the theme (IStrokedTool): the colour it names, else its verb's, else the neutral.
            var stroke = refLine.StrokeUnder(Theme);
            // A line at a value its axis cannot place is skipped with its label. It spans the plot area, so only the
            // one coordinate it is placed at decides. The automatic range holds every line; a line beyond a limit the
            // caller set lies outside the plot and is not drawn either, nor its label.
            if (refLine.Orientation.RunsAlongX)
            {
                var pt = Transform.DataToPixel(_range.XMin, refLine.Value);
                if (!pt.Y.IsDrawable() || PlotArea.HorizontalAcross(pt.Y) is not { } across) continue;
                DrawLine(across, stroke);
                if (refLine.Label is not null)
                    Ctx.DrawLineLabel(refLine.Label, LineLabelPlacement.AboveEnd(across), Theme, stroke.Color);
            }
            else
            {
                var pt = Transform.DataToPixel(refLine.Value, _range.YMin);
                if (!pt.X.IsDrawable() || PlotArea.VerticalAcross(pt.X) is not { } across) continue;
                DrawLine(across, stroke);
                if (refLine.Label is not null)
                    Ctx.DrawLineLabel(refLine.Label, LineLabelPlacement.BesideTop(pt.X, PlotArea.Y), Theme, stroke.Color);
            }
        }
    }

    /// <summary>Draws one line across the plot. A raster export under a matplotlib theme puts it on the pixel grid, as matplotlib's
    /// Agg backend puts an <c>axhline</c> or an <c>axvline</c> (a line of one horizontal or vertical segment, butt ends): a row
    /// or a column of its own instead of half of each of two.</summary>
    private void DrawLine(LineSegment across, StrokeStyle stroke)
    {
        var line = Ctx.OnPixelGrid(new PixelSegment(across.From, across.To), stroke.Thickness, projectingEnds: false);
        Ctx.DrawLine(line.From, line.To, stroke);
    }
}
