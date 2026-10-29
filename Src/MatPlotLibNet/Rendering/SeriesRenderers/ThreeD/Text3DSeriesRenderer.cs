// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Text3DSeries"/> as depth-sorted text labels projected from 3D to 2D.</summary>
internal sealed class Text3DSeriesRenderer : SeriesRenderer<Text3DSeries>
{
    /// <inheritdoc />
    public Text3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(Text3DSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(Text3DSeries series)
    {
        if (series.Annotations.Count == 0) return;

        var color = ResolveColor(series.Color);

        // Fitted to the coordinates that can be placed: a missing one (NaN) and an infinity are left out.
        var proj = ProjectionFor(
            series.Annotations.Select(a => a.X).ToArray().DrawableRange(),
            series.Annotations.Select(a => a.Y).ToArray().DrawableRange(),
            series.Annotations.Select(a => a.Z).ToArray().DrawableRange());

        // Build indexed list for depth sorting; a label at a point that cannot be placed is skipped.
        var items = new List<DepthText>(series.Annotations.Count);
        foreach (var a in series.Annotations)
        {
            var pt = proj.Project(a.X, a.Y, a.Z);
            if (!pt.IsDrawable()) continue;
            double depth = proj.Depth(a.X, a.Y, a.Z);
            items.Add(new(depth, pt, a.Text));
        }

        // Sort back-to-front (painter's algorithm)
        items.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        var font = new Font { Size = series.FontSize, Color = color };

        foreach (var item in items)
            Ctx.DrawText(item.Text, item.Pt, font, TextAlignment.Center);
    }

    private readonly record struct DepthText(double Depth, Point Pt, string Text);
}
