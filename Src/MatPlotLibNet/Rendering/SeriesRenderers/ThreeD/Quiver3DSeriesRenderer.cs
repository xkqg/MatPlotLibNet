// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Quiver3DSeries"/> as arrows projected from 3D to 2D, drawn back to front with the scene.</summary>
internal sealed class Quiver3DSeriesRenderer : SceneSeriesRenderer<Quiver3DSeries>
{
    /// <inheritdoc />
    public Quiver3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Quiver3DSeries series)
    {
        if (series.X.Length == 0) return;

        var color = ColorOf(series);
        double len = series.ArrowLength;

        // Fitted to the arrows that can be drawn, tips included: the extent the series itself reports.
        var extent = series.DrawableExtent();
        var proj = ProjectionFor(extent?.X, extent?.Y, extent?.Z);

        // Each arrow at the depth of its base.
        var arrows = new List<Arrow>(series.X.Length);
        for (int i = 0; i < series.X.Length; i++)
        {
            double bx = series.X[i], by = series.Y[i], bz = series.Z[i];
            double tx = bx + series.U[i] * len;
            double ty = by + series.V[i] * len;
            double tz = bz + series.W[i] * len;

            var basePt = proj.Project(bx, by, bz);
            var tipPt = proj.Project(tx, ty, tz);
            // An arrow with an end that cannot be placed is skipped, head and all.
            if (!basePt.CanDrawLineTo(tipPt)) continue;

            arrows.Add(new(proj.Depth(bx, by, bz), basePt, tipPt));
        }

        // The arrows are lines of the library's own look: a line's resting width under every theme.
        var stroke = Slot.StrokeOf(color, LineWidthRule.Own);
        DrawBackToFront(arrows, arrow => DrawArrow(arrow, stroke));
    }

    /// <summary>Draws one arrow: its shaft, then its head as two barbs at the tip.</summary>
    private void DrawArrow(Arrow arrow, StrokeStyle stroke)
    {
        Ctx.DrawLine(arrow.Base, arrow.Tip, stroke);

        if (arrow.Base.HeadingTo(arrow.Tip).Length < 1e-6) return;

        ArrowHead.OpenHeadMirrored.Draw(Ctx, arrow.Base, arrow.Tip, ArrowHead.OpenHeadLength, stroke);
    }

    /// <summary>An arrow: its base and tip on the canvas, at the depth of its base.</summary>
    private readonly record struct Arrow(double Depth, Point Base, Point Tip) : IDepthMark;
}
